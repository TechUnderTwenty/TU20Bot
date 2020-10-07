using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;

using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class NameMatch : ModuleBase<SocketCommandContext> {
        public enum MatchLevel {
            NoMatch,
            CloseMatch,
            CompleteMatch
        }

        public class MatchResult {
            public MatchLevel level;
            public List<string> noSpacesMatch;
            public List<string> lastNameMatch;
            public SocketGuildUser user;
            public string fullName;
        }

        public static List<MatchResult> matchNames(
            IEnumerable<UserDetails> details, IEnumerable<SocketGuildUser> users) {
            return users.Select(x => matchName(x, details)).ToList();
        }

        /// <summary>
        /// Given an unknown name and a set of valid names, identify any matches.
        /// </summary>
        public static MatchResult matchName(
            SocketGuildUser user, IEnumerable<UserDetails> details, string name = null) {
            var fullName = name ?? user.Nickname ?? user.Username;

            var matchResult = new MatchResult {
                user = user,
                fullName = fullName,
                level = MatchLevel.CloseMatch,

                noSpacesMatch = new List<string>(),
                lastNameMatch = new List<string>()
            };

            var spaceIndex = fullName.LastIndexOf(' ');

            foreach (var detail in details) {
                // If a user doesn't have a first or last name in the server
                if (spaceIndex <= 0) {
                    if (compare(detail.fullNameNoSpace, fullName, false)) {
                        matchResult.noSpacesMatch.Add(detail.fullNameNoSpace);
                    }
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
                    var firstName = fullName.Substring(0, spaceIndex);
                    var lastName = fullName.Substring(spaceIndex + 1);

                    // Checking if the last names match
                    if (!compare(detail.lastName, lastName, true))
                        continue;
                    
                    // Checking if the first names match
                    if (compare(detail.firstName, firstName, false)) {
                        matchResult.level = MatchLevel.CompleteMatch;
                        return matchResult;
                    }

                    matchResult.lastNameMatch.Add(detail.firstName + " " + detail.lastName);
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

            // For first name since first name can be different from the one provided
            return name.Replace(" ", "").Contains(matchName.Replace(" ", ""))
                   || matchName.Replace(" ", "").Contains(name.Replace(" ", ""));
        }
    }
}
