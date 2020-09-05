using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class NameMatch : ModuleBase<SocketCommandContext> {

        // The max limit of chars set by discord
        private const int messageLimit = 2000;
        private Config _config;

        private const int firstNameIndex = 0;
        private const int lastNameIndex = 1;

        public enum MatchLevel {
            NoMatch,
            CloseMatch,
            CompleteMatch
        }

        public class MatchResult {
            public MatchLevel level { get; set; }
            public List<string> noSpacesMatch { get; set; }
            public List<string> lastNameMatch { get; set; }
            public SocketGuildUser user { get; set; }
            public string fullName { get; set; }
        }

        [Command("lsuser-assignrole")]
        [Summary("matches guild member names with a specified list and assigns roles to the matched names")]
        public async Task matchNames(ulong roleId) {

            var users = Context.Guild.Users;

            _config = new Config();

            await sendSplitMessage(await nameMatching(_config.origNames, users, roleId), "\n");
        }

        public async Task<string> nameMatching(string[,] listNames, IReadOnlyCollection<SocketGuildUser> users, ulong? roleId) {

            var errorLog = new StringBuilder("");
            var discordMessage = new StringBuilder("");

            SocketRole role = null;

            if (roleId != null)
                role = Context.Guild.GetRole((ulong)roleId);

            var results = new List<MatchResult>();

            foreach (var user in users) {
                var fullName = user.Nickname ?? user.Username;
                var match = nameMatchAlg(fullName, listNames);

                match.user = user;
                results.Add(match);
            }

            var noMatchString = new List<string>();

            foreach (var result in results) {
                switch (result.level) {
                    case MatchLevel.CompleteMatch:
                        // Assigning user a role specified in the command
                        if (role != null) {
                            // var role = Context.Guild.GetRole((ulong)roleId);
                            await (result.user as IGuildUser).AddRoleAsync(role);
                            discordMessage.Append($"`{result.fullName}` has been granted role `{role.Name}`\n");
                            continue;
                        }

                        // For testing purposes
                        discordMessage.Append($"`{result.fullName}` full name matched.\n");
                        break;

                    case MatchLevel.NoMatch:
                        noMatchString.Append($"{result.fullName} did not have a last name match therefore is not in the server.\n");
                        break;

                    default:
                        // If first name doesn't match
                        foreach (var name in result.lastNameMatch)
                            errorLog.Append($"`{result.fullName}`'s last name matches with `{name}`'s last name.\n");

                        // If there is a match when there is no spaces in the name
                        foreach (var name in result.noSpacesMatch)
                            errorLog.Append($"`{name}` has either first or last name that matches with `{result.fullName}`\n");
                        break;
                }
            }

            // For adding all the messages to send to the user
            discordMessage.Append(errorLog);
            discordMessage.Append(noMatchString);

            return discordMessage.ToString();
        }

        private static void addIfNotContains<T>(List<T> list, T value) {
            if (!list.Contains(value)) list.Add(value);
        }

        /// <summary>
        /// Given an unknown name and a set of valid names, identify any matches.
        /// </summary>
        /// <param name="fullName">An unknown name</param>
        /// <param name="listNames">A set peoples names dived into first and last name</param>
        /// <param name="firstOrLastMatch">Executed when there is no space in fullname, however either the first or last name match</param>
        /// <param name="succesfulMatch">Executed when there is a complete match, that is both first name and last name match</param>
        /// <param name="lastNameMatch">Executed when only the last name matches </param>
        public static MatchResult nameMatchAlg(string fullName, string[,] listNames) {
            MatchResult matchResult = new MatchResult() {
                fullName = fullName,
                level = MatchLevel.CloseMatch,
                noSpacesMatch = new List<string>(),
                lastNameMatch = new List<string>()
            };

            int spaceIndex = fullName.LastIndexOf(' ');

            for (int i = 0; i < listNames.GetLength(0); i++) {
                // If a user doesn't have a first or last name in the server
                if (spaceIndex <= 0) {
                    if (compare((listNames[i, firstNameIndex] + listNames[i, lastNameIndex]), fullName, false))
                        matchResult.noSpacesMatch.Add(listNames[i, firstNameIndex] + listNames[i, lastNameIndex]);
                }

                /*
                 * If a user has both first and last names in the server
                 * If last name matches then proceed to see if the first name matches
                 * If both of them match then give the user the role passed in the command
                 * If first name doesn't match then notify in the console that the last name 
                 *    match was successful.
                 * If last name doesn't match then notify the user in the console. 
                 */
                else {
                    string firstName = fullName.Substring(0, spaceIndex);
                    string lastName = fullName.Substring(spaceIndex + 1);

                    // Checking if the last names match
                    if (compare(listNames[i, lastNameIndex], lastName, true)) {
                        // Checking if the first names match
                        if (compare(listNames[i, firstNameIndex], firstName, false)) {
                            matchResult.level = MatchLevel.CompleteMatch;
                            return matchResult;
                        }

                        matchResult.lastNameMatch.Add(listNames[i, firstNameIndex] + " " + listNames[i, lastNameIndex]);
                    }
                }
            }
            if (matchResult.lastNameMatch.Count == 0 && matchResult.noSpacesMatch.Count == 0)
                matchResult.level = MatchLevel.NoMatch;
            return matchResult;
        }

        /// <summary>
        /// Compares two name strings
        /// </summary>
        /// <param name="name"></param>
        /// <param name="matchName"></param>
        /// <param name="fullMatch">If true, then match for equivalency, otherwise match if contains</param>
        /// <returns></returns>
        private static bool compare(string name, string matchName, bool fullMatch) {
            // For last name since last name should be exactly the same
            if (fullMatch)
                return name.Replace(" ", "") == matchName.Replace(" ", "");

            // For first name since first name can be differnt from the one provided
            return name.Replace(" ", "").Contains(matchName.Replace(" ", "")) || matchName.Replace(" ", "").Contains(name.Replace(" ", ""));
        }

        private async Task sendSplitMessage(string mainMessage, string separator) {

            int startIndex = 0;

            if (mainMessage == null)
                return;

            if (mainMessage.Length <= messageLimit) {
                await ReplyAsync(mainMessage);
                return;
            }

            // If the separator doesn't exist in the string then split it into simple parts
            if (!mainMessage.Contains(separator)) {
                do {
                    await ReplyAsync(mainMessage.Substring(startIndex, messageLimit));
                    startIndex = messageLimit;
                } while (startIndex + messageLimit <= mainMessage.Length);
                await ReplyAsync(mainMessage.Substring(startIndex));
                return;
            }

            do {
                // Get the substring of the specified length
                string subString = mainMessage.Substring(startIndex, messageLimit + 1);

                // Get the last index of new line in the substring of specified length
                int endIndex = subString.LastIndexOf(separator);

                // Send the message from the start index to last index of new line character
                await ReplyAsync(mainMessage.Substring(startIndex, endIndex));

                // set the start index to end index, +1 to skip over \n character
                startIndex += endIndex + 1;

            } while (startIndex + messageLimit <= mainMessage.Length);

            // Send whatever is left from the string to the chat
            await ReplyAsync(mainMessage.Substring(startIndex));
        }

    }
}
