using System.Linq;
using System.Collections.Generic;

using Discord.WebSocket;

using TU20Bot.Configuration;

namespace TU20Bot.Support {
    public static class NameMatcher {
        public enum MatchLevel {
            NoMatch,
            PartialMatch,
            CompleteMatch
        }

        public class MatchResult {
            public MatchLevel level;
            
            public ulong id;
            public string fullName;
            
            public List<string> noSpacesMatch;
            public List<string> lastNameMatch;

            public MatchResult withLevel(MatchLevel value) {
                level = value;

                return this;
            }
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
            
            var noSpacesMatch = new List<string>();
            var lastNameMatch = new List<string>();
            
            var result = new MatchResult {
                level = MatchLevel.CompleteMatch,
                noSpacesMatch = noSpacesMatch,
                lastNameMatch = lastNameMatch,
                id = user.Id,
                fullName = fullName
            };

            var spaceIndex = fullName.LastIndexOf(' ');
            spaceIndex = spaceIndex == -1 ? fullName.Length - 1 : spaceIndex;

            foreach (var detail in details) {
                // If a user doesn't have a first or last name in the server
                if (spaceIndex <= 0 && compareNames(detail.fullName, fullName) != MatchLevel.NoMatch)
                    noSpacesMatch.Add(detail.fullName);

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
                    if (compareNames(detail.lastName, lastName) != MatchLevel.CompleteMatch)
                        continue;
                    
                    // Checking if the first names match
                    if (compareNames(detail.firstName, firstName) != MatchLevel.NoMatch)
                        return result.withLevel(MatchLevel.CompleteMatch);

                    lastNameMatch.Add(detail.firstName + " " + detail.lastName);
                }
            }

            return result.withLevel(
                lastNameMatch.Any() || noSpacesMatch.Any() 
                    ? MatchLevel.PartialMatch
                    : MatchLevel.NoMatch);
        }

        /// <summary>
        /// Compares two name strings
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static MatchLevel compareNames(string first, string second) {
            // Drop any spaces in inputs.
            var a = first.Replace(" ", "");
            var b = second.Replace(" ", "");

            if (a == b)
                return MatchLevel.CompleteMatch;

            if (a.Contains(b) || b.Contains(a))
                return MatchLevel.PartialMatch;

            return MatchLevel.NoMatch;
        }
    }
}
