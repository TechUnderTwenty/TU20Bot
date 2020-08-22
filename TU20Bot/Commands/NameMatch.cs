using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TU20Bot.Commands {
    public class NameMatch : ModuleBase<SocketCommandContext> {

        private readonly string[,] origNames = {
            {"Dev","Narula"},
            {"Timur","Khayrullin"},
            {"Zaynah","nolastname"},
            {"Yifan","Wang"},
            {"Tahmeed","Naser"},
            {"Shrena","Sribalan"},
            {"Rick","(O20)"},
            {"Pranav","Vyas"},
            {"Karen","Truong"},
            {"Muhammad Muizz","nolastname"},
            {"Vince","Li"},
            {"Alex","Li"},
            {"nofirstname","Nolan"},
            {"Borna","Sadeghi"},
            {"Hargun","nolastname"},
            {"Markos","Georghiades"},
            {"Muhammad Muizz","Zafar"},
            {"Taylor desgroup","Whatley"},
            {"Viktor fasest","Korolyuk"},
            {"Denys","Linkov"},
            {"Muhammad Ali","Syed"},
        };

        [Command("lsuser-assignrole")]
        [Summary("matches guild member names with a specified list and assigns roles to the matched names")]
        public async Task matchNames(ulong roleId) {
            var users = Context.Guild.Users;
            string discordMessage = "";
            string noLastNameMsg = "";
            string noFirstNameMsg = "";
            string notInGuildMsg = "";
            for (int i = 0; i < origNames.GetLength(0); i++) {
                foreach (var user in users) {
                    string fullName = user.Nickname;
                    if (fullName == null) {
                        fullName = user.ToString();
                        fullName = fullName.Substring(0, fullName.LastIndexOf('#'));
                    }

                    int spaceIndex = fullName.LastIndexOf(' ');
                    // If a user doesn't have a first or last name in the server
                    if (spaceIndex <= 3) {
                        if (compare((origNames[i, 0] + origNames[i, 1]), fullName)) {
                            noFirstNameMsg +=
                                $" User `{origNames[i, 0]} {origNames[i, 1]}` has either " +
                                $"first or last name which matches with `{fullName}`\n";
                            Console.WriteLine($" User `{origNames[i, 0]} {origNames[i, 1]}` has either " +
                                              $"first or last name which matches with `{fullName}`");
                        }
                    }
                    /*
                     * If a user has both first and last names in the server
                     * If last name matches then proceed to see if the first name matches
                     * If both of them match then give the user the role passed in the command
                     * If first name doesn't match then notify in the console that the last name 
                     * match was successful.
                     * If last name doesn't match then notify the user in the console. 
                     */
                    else {
                        string firstName = fullName.Substring(0, spaceIndex);
                        string lastName = fullName.Substring(spaceIndex + 1);

                        // Checking if the last names match
                        if (compare(origNames[i, 1], lastName) ||
                            compare(lastName, origNames[i, 1])) {
                            // Checking if the first names match
                            if (compare(origNames[i, 0], firstName) ||
                                compare(firstName, origNames[i, 0])) {
                                // Assigning user a role specified in the command
                                var role = Context.Guild.GetRole(roleId);
                                await (user as IGuildUser).AddRoleAsync(role);
                                discordMessage +=
                                    $"`{firstName} {lastName}` has been granted role `{role}`\n";
                                break;
                            }

                            // If first name doesn't match
                            noLastNameMsg += $"`{lastName}` of `{firstName} {lastName}` matches " +
                                             $"with `{origNames[i, 1]}` of `{origNames[i, 0]} {origNames[i, 1]}`\n";
                            Console.WriteLine($"`{lastName}` of `{firstName} {lastName}` matches " +
                                              $"with `{origNames[i, 1]}` of `{origNames[i, 0]} {origNames[i, 1]}`");
                        }
                    }
                }
            }

            // For adding all the messages to send to the user
            discordMessage += noLastNameMsg + noFirstNameMsg;
            // For reporting if the user doesn't exist in the server (no last name match)
            for (int i = 0; i < origNames.GetLength(0); i++) {
                if (!discordMessage.Contains(origNames[i, 1])) {
                    notInGuildMsg +=
                        $"{origNames[i, 0]} {origNames[i, 1]} did not have a last name match therefore is not in the server\n";
                }
            }

            discordMessage += notInGuildMsg;
            await sendSplitMessage(discordMessage, "\n");
        }

        private bool compare(string name, string matchName) {
            return name.Replace(" ", "").Contains(matchName.Replace(" ", ""));
        }

        private async Task sendSplitMessage(string mainMessage, string separator) {
            // The max limit set by discord
            int messageLimit = 2000;
            int startIndex = 0;

            if (mainMessage == null) {
                return;
            }

            if (mainMessage.Length <= messageLimit) {
                await ReplyAsync(mainMessage);
                return;
            }
            // If the separator doesn't exist in the string then split it into simple parts
            if (!mainMessage.Contains(separator)) {
                do {
                    await ReplyAsync(mainMessage.Substring(startIndex, messageLimit));
                    startIndex = messageLimit;
                } while (startIndex+messageLimit <= mainMessage.Length);
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