using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TU20Bot.Configuration;

namespace TU20Bot.Commands
{
    public class NameMatch : ModuleBase<SocketCommandContext>
    {

        // The max limit of chars set by discord
        private const int messageLimit = 2000;
        private Config _config;

        private const int firstNameIndex = 0;
        private const int lastNameIndex = 1;

        public delegate void IndexMatch(int index);
        public delegate void IndexMatchDetailed(int index, string firstName, string lastName);

        [Command("lsuser-assignrole")]
        [Summary("matches guild member names with a specified list and assigns roles to the matched names")]
        public async Task matchNames(ulong roleId)
        {

            var users = Context.Guild.Users;

            _config = new Config();

            await sendSplitMessage(await nameMatching(_config.origNames, users, roleId), "\n");
        }

        public async Task<string> nameMatching(string[,] listNames, IReadOnlyCollection<SocketGuildUser> users, ulong? roleId)
        {

            StringBuilder errorLog = new StringBuilder("");
            StringBuilder discordMessage = new StringBuilder("");

            SocketRole role = null;

            if (roleId != null)
                role = Context.Guild.GetRole((ulong)roleId);

            List<int> indexNotToRead = new List<int>();

            foreach (var user in users)
            {
                string fullName = user.Nickname ?? user.Username;

                nameMatchAlg(fullName, listNames,

                /** There was no space within the name, but either the first or last name matched **/
                index =>
                {
                    errorLog.Append($"`{listNames[index, firstNameIndex]} {listNames[index, lastNameIndex]}` has either " +
                                            $"first or last name that matches with `{fullName}`\n");

                    // Person exists with some name in the server
                    addIfNotContains(indexNotToRead, index);
                },

                /** A full match **/
                async (index, firstName, lastName) =>
                {
                    // Assigning user a role specified in the command
                    if (role != null)
                    {
                        // var role = Context.Guild.GetRole((ulong)roleId);
                        await (user as IGuildUser).AddRoleAsync(role);
                        discordMessage.Append($"`{firstName} {lastName}` has been granted role `{role.Name}`\n");

                        // Person exists in the server
                        addIfNotContains(indexNotToRead, index);

                        return;
                    }

                    // For testing purposes
                    discordMessage.Append($"`{firstName} {lastName}` full name matched.\n");

                    // Person exists in the server
                    addIfNotContains(indexNotToRead, index);
                },

                /** If the last name matches but the first name is different **/
                (index, firstName, lastName) =>
                {
                    // If first name doesn't match
                    errorLog.Append($"`{firstName} {lastName}`'s last name matches " +
                            $"with `{listNames[index, firstNameIndex]} {listNames[index, lastNameIndex]}`'s last name.\n");

                    // Person exists with some name in the server
                    addIfNotContains(indexNotToRead, index);
                });
            }

            // For adding all the messages to send to the user
            discordMessage.Append(errorLog);

            // For creating a list of all the indexes not present in the indexNotToRead List
            for (int i = 0; i < listNames.GetLength(0); i++)
            {

                // For creating a list that has indexes to read from for the not present in server message
                if (!indexNotToRead.Contains(i))
                {
                    // For reporting if the user doesn't exist in the server (no last name match)
                    discordMessage.Append($"{listNames[i, firstNameIndex]} {listNames[i, lastNameIndex]}" +
                                          $" did not have a last name match therefore is not in the server.\n");
                }

            }

            return discordMessage.ToString();
        }

        private static void addIfNotContains<T>(List<T> list, T value)
        {
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
        public static void nameMatchAlg(string fullName, string[,] listNames, IndexMatch firstOrLastMatch, IndexMatchDetailed succesfulMatch, IndexMatchDetailed lastNameMatch)
        {
            int spaceIndex = fullName.LastIndexOf(' ');

            for (int i = 0; i < listNames.GetLength(0); i++)
            {
                // If a user doesn't have a first or last name in the server
                if (spaceIndex <= 0)
                {
                    if (compare((listNames[i, firstNameIndex] + listNames[i, lastNameIndex]), fullName, false))
                        firstOrLastMatch(i);
                }

                /*
                 * If a user has both first and last names in the server
                 * If last name matches then proceed to see if the first name matches
                 * If both of them match then give the user the role passed in the command
                 * If first name doesn't match then notify in the console that the last name 
                 *    match was successful.
                 * If last name doesn't match then notify the user in the console. 
                 */
                else
                {
                    string firstName = fullName.Substring(0, spaceIndex);
                    string lastName = fullName.Substring(spaceIndex + 1);

                    // Checking if the last names match
                    if (compare(listNames[i, lastNameIndex], lastName, true))
                    {
                        // Checking if the first names match
                        if (compare(listNames[i, firstNameIndex], firstName, false))
                        {
                            succesfulMatch(i, firstName, lastName);
                            break;
                        }

                        lastNameMatch(i, firstName, lastName);
                    }
                }
            }
        }

        /// <summary>
        /// Compares two name strings
        /// </summary>
        /// <param name="name"></param>
        /// <param name="matchName"></param>
        /// <param name="fullMatch">If true, then match for equivalency, otherwise match if contains</param>
        /// <returns></returns>
        private static bool compare(string name, string matchName, bool fullMatch)
        {
            // For last name since last name should be exactly the same
            if (fullMatch)
            {
                return name.Replace(" ", "").Equals(matchName.Replace(" ", "")) || matchName.Replace(" ", "").Equals(name.Replace(" ", ""));
            }

            // For first name since first name can be differnt from the one provided
            return name.Replace(" ", "").Contains(matchName.Replace(" ", "")) || matchName.Replace(" ", "").Contains(name.Replace(" ", ""));
        }

        private async Task sendSplitMessage(string mainMessage, string separator)
        {

            int startIndex = 0;

            if (mainMessage == null)
                return;

            if (mainMessage.Length <= messageLimit)
            {
                await ReplyAsync(mainMessage);
                return;
            }

            // If the separator doesn't exist in the string then split it into simple parts
            if (!mainMessage.Contains(separator))
            {
                do
                {
                    await ReplyAsync(mainMessage.Substring(startIndex, messageLimit));
                    startIndex = messageLimit;
                } while (startIndex + messageLimit <= mainMessage.Length);
                await ReplyAsync(mainMessage.Substring(startIndex));
                return;
            }

            do
            {
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