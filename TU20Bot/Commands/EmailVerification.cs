using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class EmailVerification : ModuleBase<SocketCommandContext> {

        [Command("verify")]
        public async Task EmailVerify(string email) {

            // Change dictionary such that it doesn't give an error with the same key
            Config config = ((Client)Context.Client).config;

            bool result = await emailCompare(email, config.userDataCsv, config);

            if (result) {
                await ReplyAsync("Email verified");
            } else {
                saveUnverifiedEmail(config.userEmailId, Context.User.Id, email);
                await ReplyAsync("Could not verify email. Your email has been saved and will be verified automatically.");
            }
        }

        public async Task<bool> emailCompare(string email, List<CSVData> csvEmailList, Config config) {

            foreach (var botUser in csvEmailList) {

                // If both of the emails match
                if (botUser.Email.Equals(email)) {

                    // If the email in the list if of a speaker asign the speaker role
                    if (botUser.isSpeaker) {
                        var roleSpeaker = Context.Guild.GetRole(config.speakerRoleID);
                        await (Context.User as IGuildUser).AddRoleAsync(roleSpeaker);
                        return true;
                    }

                    // If it is not of the speaker assign the attendee role
                    var roleAttendee = Context.Guild.GetRole(config.attendeeRoleID);
                    await (Context.User as IGuildUser).AddRoleAsync(roleAttendee);
                    return true;
                }
            }

            // If there's nothing in the list or the list doesn't have user email
            return false;
        }

        public void saveUnverifiedEmail(Dictionary<ulong, string> emailIdStore, ulong id, string email) {
            if (!emailIdStore.ContainsKey(id))
                emailIdStore.Add(id, email);
        }


    }
}
