using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

using EmbedIO;
using EmbedIO.Routing;

using MongoDB.Driver;

using TU20Bot.Models;
using TU20Bot.Support;
using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration.Controllers {
    public class MatchController : ServerController {
        private struct CellReference {
            public int columnNumber;
            public int rowNumber;
        }

        private async Task<bool> evaluateMatch(UserMatch match) {
            var guild = server.client.GetGuild(server.config.guildId);
            
            // Being cautious, even though EmbedIO will do this for me.
            if (guild == null)
                return false;

            var role = guild.GetRole(match.role);
            var users = guild.Users;
            
            // Attempt to match all the users with an approximate name-match algorithm
            var nameMatches = NameMatcher
                .matchNames(match.userDetailInformation, users)
                .Where(result => result.level != NameMatcher.MatchLevel.NoMatch)
                .Select(x => x.id);
            
            IEnumerable<ulong> emailMatches = null;

            // If possible, query the database for users with mathcing emails that are already in our database 
            if (server.client.database != null) {
                var collection = server.client.database
                    .GetCollection<UserModel>(UserModel.collectionName);
                
                // Get all non-null emails from the match
                var allEmails = match.userDetailInformation
                    .Select(x => x.email)
                    .Where(x => x != null);

                // Find all emails in the database that are provided in the match 
                var emailList = await collection
                    .Find(Builders<UserModel>.Filter.In(x => x.email, allEmails))
                    .ToListAsync();

                emailMatches = emailList.Select(x => ulong.Parse(x.discordId));
            }

            var results = nameMatches;
            
            if (emailMatches != null) {
                results = results.Concat(emailMatches).Distinct();
            }
            
            if (role == null || users == null)
                return false;

            // Assign roles to people who matched. This is blocking right now.
            foreach (var result in results) {
                var user = guild.GetUser(result);

                // Just in case...
                if (user == null)
                    continue;

                await user.AddRoleAsync(role);
            }

            return true;
        }
        
        [Route(HttpVerbs.Get, "/match")]
        public IEnumerable<object> getMatches() {
            var guild = server.client.GetGuild(server.client.config.guildId);
            
            return server.config.userRoleMatches.Select(x => new {
                role = new {
                    id = x.role.ToString(),
                    name = guild.GetRole(x.role)?.Name
                },
                details = x.userDetailInformation.Select(y => new {
                    y.email,
                    y.firstName,
                    y.lastName
                })
            });
        }

        [Route(HttpVerbs.Post, "/match")]
        public async Task<int> createMatch() {
            var match = (await HttpContext.GetRequestDataAsync<MatchJsonPayload>()).toUserMatch();

            await evaluateMatch(match).ConfigureAwait(false);

            // Add it to the config for future matches.
            var index = server.config.userRoleMatches.Count;
            server.config.userRoleMatches.Add(match);

            return index;
        }

        // Convenience for MatchController.editData(). Fetches data from a NPOI row.
        private static void fetchCell(IRow row, CellReference? reference, ICollection<string> data) {
            if (!reference.HasValue)
                return;

            var cell = row.GetCell(reference.Value.columnNumber);

            // Not going to bother to calculate non string cells.
            // ... But I should ident it anyway.
            data.Add(cell.CellType == CellType.String ? cell.StringCellValue : "");
        }
        
        // Convenience for MatchController.editData(). Loads cell into cell reference.
        private static void pickCell(ref CellReference? reference, IRow row, ICell cell, string name) {
            if (reference.HasValue) {
                throw new Exception(
                    $"Detected two or more {name} headers " +
                    $"(columns {reference.Value.columnNumber} and {cell.ColumnIndex}).");
            }

            reference = new CellReference {
                columnNumber = cell.ColumnIndex,
                rowNumber = row.RowNum + 1
            };
        }

        // Very long method, seems like that's the case with spreadsheets.
        // I apologize for the next person who has to edit this method.
        [Route(HttpVerbs.Put, "/match/{index}/data")]
        public async Task<object> editData(int index) {
            if (server.config.userRoleMatches.Count <= index)
                throw HttpException.BadRequest($"Match {index} does not exist.");
            
            var workbook = new XSSFWorkbook(HttpContext.Request.InputStream);

            // Spreadsheet parsing can go wrong quickly, I'm trying to make good error messages to catch that.
            if (workbook.NumberOfSheets != 1) {
                return new {
                    error = $"Expected one sheet but got {workbook.NumberOfSheets}."
                };
            }

            var sheet = workbook.GetSheetAt(0);

            // This is a complicated, crappy system.
            // I'm trying to use to be compatible with as many spreadsheets as possible.
            CellReference? firstName = null;
            CellReference? lastName = null;
            CellReference? email = null;
            CellReference? fullName = null;
            var columnsAccounted = new List<int>();

            // Here for convenience. It may have been a mistake to use nullable CellReference. For review.
            bool foundAllCells() {
                return (firstName.HasValue && lastName.HasValue || fullName.HasValue) && email.HasValue;
            }

            bool hasRowConflict() {
                var list = new[] {
                    firstName?.rowNumber,
                    lastName?.rowNumber,
                    fullName?.rowNumber,
                    email?.rowNumber,
                }.Where(x => x.HasValue).ToArray();

                return list.Any(x => x != list.First());
            }

            // Justifying my use of exceptions: case is exceptional. See MatchController.pickCell().
            try {
                foreach (IRow row in sheet) {
                    foreach (var cell in row) {
                        // We only want cells that have text and are in a column we haven't seen before.
                        if (cell.CellType != CellType.String || columnsAccounted.Contains(cell.ColumnIndex))
                            continue;

                        var text = cell.StringCellValue.ToLower();

                        if (text.Length == 0)
                            continue;

                        // Rough approximation of what a header might have.
                        if (text.Contains("name")) {
                            if (text.Contains("first")) {
                                pickCell(ref firstName, row, cell, "first name");
                            } else if (text.Contains("last")) {
                                pickCell(ref lastName, row, cell, "last name");
                            } else {
                                pickCell(ref fullName, row, cell, "full name");
                            }
                        } else if (text.Contains("email") || text.Contains("address")) {
                            pickCell(ref email, row, cell, "address");
                        }

                        columnsAccounted.Add(cell.ColumnIndex);
                    }

                    // I want to make sure the starting row is consistent for all entries.
                    if (hasRowConflict()) {
                        var errorMessage = string.Join(", ", new List<string> {
                            firstName.HasValue ? $"first name: {firstName.Value.rowNumber}" : null,
                            lastName.HasValue ? $"last name: {lastName.Value.rowNumber}" : null,
                            fullName.HasValue ? $"full name: {fullName.Value.rowNumber}" : null,
                            email.HasValue ? $"email: {email.Value.rowNumber}" : null,
                        }.Where(x => x != null));

                        return new {
                            error = $"Headers were split across multiple rows ({errorMessage})."
                        };
                    }

                    // Quit early if we have found all columns.
                    if (foundAllCells())
                        break;
                }
            } catch (Exception e) {
                // Catch anything thrown from MatchController.catchCell().
                return new {
                    error = e.Message
                };
            }

            if (!foundAllCells()) {
                // :eyes:
                var errorMessage = string.Join(", ", new List<string> {
                    firstName.HasValue ? null : "missing first name",
                    lastName.HasValue ? null : "missing last name",
                    fullName.HasValue ? null : "missing full name",
                    email.HasValue ? null : "missing email name",
                }.Where(x => x != null));

                return new {
                    error = $"Could not find required headers ({errorMessage})."
                };
            }

            if (!email.HasValue) {
                return new {
                    error = "Internal error, was not able to collect data."
                };
            }

            // Email always has a value. See foundAllCells().
            var firstNameData = new List<string>();
            var lastNameData = new List<string>();
            var fullNameData = new List<string>();
            var emailData = new List<string>();
            var startingRow = email.Value.rowNumber;

            // Iterate through them all. Efficiency takes a hit, excel allows a lot of empty rows.
            for (var a = startingRow; a < sheet.LastRowNum; a++) {
                var row = sheet.GetRow(a);

                // Use convenience function to reduce duplication.
                fetchCell(row, firstName, firstNameData);
                fetchCell(row, lastName, lastNameData);
                fetchCell(row, fullName, fullNameData);
                fetchCell(row, email, emailData);
            }

            // Going to assume all lists have the same size. I need to combine them manually now.
            // Email is still guaranteed to have a value I suppose.
            var details = new List<UserDetails>();

            for (var a = 0; a < emailData.Count; a++) {
                var result = new UserDetails();

                if (emailData[a].Length == 0)
                    continue;

                if (fullName.HasValue) {
                    var name = fullNameData[a];
                    var splitIndex = name.LastIndexOf(' ');

                    result.firstName = name.Substring(0, splitIndex);
                    result.lastName = name.Substring(splitIndex);
                } else {
                    result.firstName = firstNameData[a];
                    result.lastName = lastNameData[a];
                }

                result.email = emailData[a];

                details.Add(result);
            }

            // Replace the previous details object.
            var match = server.config.userRoleMatches[index];
            match.userDetailInformation = details;

            // Evaluate match takes a long time with rate limits, run later and hope it works.
            // Better than praying that the super short axios timeout is enough time.
            await evaluateMatch(match).ConfigureAwait(false);
            
            return new {
                details = match.userDetailInformation.Select(x => new {
                    x.email,
                    x.firstName,
                    x.lastName
                })
            };
        }

        [Route(HttpVerbs.Put, "/match/{index}")]
        public async Task editMatch(int index) {
            var match = (await HttpContext.GetRequestDataAsync<MatchJsonPayload>()).toUserMatch();

            await evaluateMatch(match).ConfigureAwait(false);

            server.config.userRoleMatches[index] = match;
        }

        // To lazy to make this remove roles... just delete the role yourself.
        [Route(HttpVerbs.Delete, "/match/{index}")]
        public void deleteMatch(int index) {
            server.config.userRoleMatches.RemoveAt(index);
        }
        
        [Route(HttpVerbs.Post, "/match/run")]
        public async Task<bool> runMatch() {
            // No AnyAsync :thinking:
            foreach (var match in server.config.userRoleMatches) {
                if (!await evaluateMatch(match)) {
                    return false;
                }
            }

            return true;
        }
    }
}
