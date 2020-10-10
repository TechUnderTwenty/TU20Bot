using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

using EmbedIO;
using EmbedIO.Routing;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration.Controllers {
    public class MatchController : ServerController {
        private struct CellReference {
            public int columnNumber;
            public int rowNumber;
        }
        
        [Route(HttpVerbs.Get, "/match")]
        public IEnumerable<object> getMatches() {
            var guild = server.client.GetGuild(server.client.config.guildId);
            
            return server.config.matches.Select(x => new {
                role = new {
                    id = x.role.ToString(),
                    name = guild.GetRole(x.role)?.Name
                },
                details = x.details.Select(y => new {
                    y.email,
                    y.firstName,
                    y.lastName
                })
            });
        }

        [Route(HttpVerbs.Post, "/match")]
        public async Task<int> createMatch() {
            var match = await HttpContext.GetRequestDataAsync<MatchJsonPayload>();

            server.config.matches.Add(new UserMatch {
                role = ulong.Parse(match.role),
                details = match.details.Select(x => new UserDetails {
                    email = x.email,
                    firstName = x.firstName,
                    lastName = x.lastName
                }).ToList()
            });

            return server.config.matches.Count - 1;
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
        public object editData(int index) {
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
                            error = $"Row conflict detected, headers were split across multiple rows ({errorMessage})."
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
            var match = server.config.matches[index];
            match.details = details;

            // :cry: I made it!!!
            return new {
                details = match.details.Select(x => new {
                    x.email,
                    x.firstName,
                    x.lastName
                })
            };
        }

        [Route(HttpVerbs.Put, "/match/{index}")]
        public async Task editMatch(int index) {
            var match = await HttpContext.GetRequestDataAsync<MatchJsonPayload>();
            
            server.config.matches[index] = new UserMatch {
                role = ulong.Parse(match.role),
                details = match.details.Select(x => new UserDetails {
                    email = x.email,
                    firstName = x.firstName,
                    lastName = x.lastName
                }).ToList()
            };
        }

        [Route(HttpVerbs.Delete, "/match/{index}")]
        public void deleteMatch(int index) {
            server.config.matches.RemoveAt(index);
        }
    }
}
