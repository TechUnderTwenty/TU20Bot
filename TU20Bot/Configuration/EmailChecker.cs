using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TU20Bot.Configuration {
    public class EmailChecker {

        private Config config;
        private Client client;

        public EmailChecker(Config config, Client client) {
            this.config = config;
            this.client = client;
        }

        // Method running a separate thread and mathcing any unverified email to the email list in csv
        public async Task checkForEmail(List<CSVData> csvEmail) {

            for (int i = 0; i < config.userEmailId.Count; i++) {

                // Comparing all emails in the newly obtained csv list with unverified emails
                foreach (var botUser in csvEmail) {

                    // If some unverified email matches the email from the csv list,
                    if (botUser.Email.Equals(config.userEmailId.ElementAt(i).Value)) {

                        // Get the user id of user associated with that email
                        ulong userId = config.userEmailId.ElementAt(i).Key;
                        var user = client.GetUser(userId);

                        // If the email is of a speaker then asign the speaker role
                        if (botUser.isSpeaker) {
                            var roleSpeaker = client.GetGuild(config.guildId).GetRole(config.speakerRoleID);
                            await(user as IGuildUser).AddRoleAsync(roleSpeaker);
                        }
                        
                        // If the email is not of a speaker then assign an attendee role
                        var roleAttendee = client.GetGuild(config.guildId).GetRole(config.attendeeRoleID);
                        await(user as IGuildUser).AddRoleAsync(roleAttendee);

                        // Inform the user by printing to console
                        Console.WriteLine($"{user} email verified from list");

                        // Remove that specific index from the dictionary since the user has been verified
                        config.userEmailId.Remove(userId);
                    }

                }
            }
        }
    }
}
