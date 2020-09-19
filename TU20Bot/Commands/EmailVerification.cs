using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class EmailVerification : ModuleBase<SocketCommandContext> {

        [Command("verify")]
        public async Task EmailVerify(string email) {

            // Change dictionary such that it doesn't give an error with the same key
            Config config = ((Client)Context.Client).config;
            DbCommUnverifiedUser unverifiedUser = new DbCommUnverifiedUser(new BotDbContext());

            CSVData result = emailCompare(email, config.userDataCsv);

            if (result != null) {
                await ReplyAsync("Email verified");
                await assignRole(result, config);
            } else {
                await saveUnverifiedEmail(unverifiedUser, Context.User.Id, email);
                await ReplyAsync("Could not verify email. Your email has been saved and will be verified automatically.");
            }
        }

        public CSVData emailCompare(string email, List<CSVData> csvEmailList) {

            foreach (var botUser in csvEmailList) {

                // If both of the emails match
                if (botUser.Email.Equals(email)) {
                    return botUser;
                }
            }

            // If there's nothing in the list or the list doesn't have user email
            return null;
        }

        public async Task assignRole(CSVData userData, Config config) {
            var guild = Context.Client.GetGuild(config.guildId);
            var user = guild.GetUser(Context.User.Id);

            // If the email in the list if of a speaker asign the speaker role
            if (userData.isSpeaker) {
                var roleSpeaker = guild.GetRole(config.speakerRoleID);
                await user.AddRoleAsync(roleSpeaker);
            }

            // If it is not of the speaker assign the attendee role
            var roleAttendee = guild.GetRole(config.attendeeRoleID);
            await user.AddRoleAsync(roleAttendee);
        }

        public async Task saveUnverifiedEmail(DbCommUnverifiedUser commUnverifiedUser, ulong id, string email) {
            await commUnverifiedUser.addUserInfo(id, email);
        }
    }
}
