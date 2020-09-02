using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class NameMatch : ModuleBase<SocketCommandContext> {

        /*
         * replacing substring and index of with split
         * changing .getLength(0) to sth else in the for loop
         * inverting the for loop
         */

        // The max limit of chars set by discord
        private const int messageLimit = 2000;
        private Config _config;

        [Command("lsuser-assignrole")]
        [Summary("matches guild member names with a specified list and assigns roles to the matched names")]
        public async Task matchNames(ulong roleId) {

            var users = Context.Guild.Users;

            _config = new Config();

            await sendSplitMessage(await nameMatching(_config.origNames, users, roleId), "\n");
        }

        public async Task<string> nameMatching(string[,] listNames,
            IReadOnlyCollection<SocketGuildUser> users, ulong? roleId) {

            StringBuilder errorLog = new StringBuilder("");
            StringBuilder discordMessage = new StringBuilder("");
            const int firstNameIndex = 0;
            const int lastNameIndex = 1;
            SocketRole role = null;
            if (roleId != null)
                role = Context.Guild.GetRole((ulong)roleId);

            foreach (var user in users) {
                string fullName = user.Nickname ?? user.Username;
                int spaceIndex = fullName.LastIndexOf(' ');

                for (int i = 0; i < listNames.GetLength(0); i++) {

                    // If a user doesn't have a first or last name in the server
                    if (spaceIndex <= 0) {
                        if (compare((listNames[i, firstNameIndex] + listNames[i, lastNameIndex]), fullName, false)) {
                            errorLog.Append(
                                $"`{listNames[i, firstNameIndex]} {listNames[i, lastNameIndex]}` has either " +
                                $"first or last name that matches with `{fullName}`\n");
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
                        if (compare(listNames[i, lastNameIndex], lastName, true)) {

                            // Checking if the first names match
                            if (compare(listNames[i, firstNameIndex], firstName, false)) {

                                // Assigning user a role specified in the command
                                if (role != null) {
                                    // var role = Context.Guild.GetRole((ulong)roleId);
                                    await (user as IGuildUser).AddRoleAsync(role);
                                    discordMessage.Append(
                                        $"`{firstName} {lastName}` has been granted role `{role.Name}`\n");
                                    break;
                                }

                                // For testing purpose
                                discordMessage.Append(
                                    $"`{firstName} {lastName}` full name matched.\n");
                                break;
                            }

                            // If first name doesn't match
                            errorLog.Append($"`{firstName} {lastName}`'s last name matches " +
                                    $"with `{listNames[i, firstNameIndex]} {listNames[i, lastNameIndex]}`'s last name.\n");
                        }
                    }
                }
            }


            // For adding all the messages to send to the user
            discordMessage.Append(errorLog);

            // Changing this anyways
            // For reporting if the user doesn't exist in the server (no last name match)
            for (int i = 0; i < listNames.GetLength(0); i++) {
                if (!discordMessage.ToString().Contains(listNames[i, lastNameIndex])) {
                    discordMessage.Append(
                        $"{listNames[i, firstNameIndex]} {listNames[i, lastNameIndex]}" +
                        $" did not have a last name match therefore is not in the server.\n");
                }
            }

            return discordMessage.ToString();
        }


        private bool compare(string name, string matchName, bool fullMatch) {
            // For last name since last name should be exactly the same
            if (fullMatch) {
                if (name.Replace(" ", "").Equals(matchName.Replace(" ", "")) ||
                matchName.Replace(" ", "").Equals(name.Replace(" ", ""))) {
                    return true;
                }
                return false;
            }

            // For first name since first name can be differnt from the one provided
            if (name.Replace(" ", "").Contains(matchName.Replace(" ", "")) ||
                matchName.Replace(" ", "").Contains(name.Replace(" ", ""))) {
                return true;
            }
            return false;
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