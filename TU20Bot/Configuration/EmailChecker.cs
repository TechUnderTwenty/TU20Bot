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
        private DbCommUnverifiedUser dbComm;

        public EmailChecker(Config config, Client client, DbCommUnverifiedUser dbComm) {
            this.config = config;
            this.client = client;
            this.dbComm = dbComm;
        }


        // Method running on a separate thread and mathcing any unverified email to the email list in csv
        public async Task emailCheck(List<CSVData> csvEmail) {

            var userInfo = checkEmailInCsvList(csvEmail);

            if (userInfo.userData != null) {
                await assignRole(userInfo);
                Console.WriteLine($"user email with id: {userInfo.userId} verified");
            }

        }

        // Method comparing and returing the CSVData and ulong of the user who's email have been verified
        public (ulong userId, CSVData userData) checkEmailInCsvList(List<CSVData> csvData) {

            var unverifiedUserList = dbComm.getUserList();

            foreach (var unverifiedUser in unverifiedUserList) {

                // Comparing all emails in the newly obtained csv list with unverified emails
                foreach (var botUser in csvData) {

                    // If some unverified email matches the email from the csv list,
                    if (botUser.Email.Equals(unverifiedUser.Email)) {

                        // Get the user id of user associated with that email
                        ulong Id = unverifiedUser.UserId;

                        // Remove that specific index from the dictionary since the user has been verified
                        dbComm.removeUserInfo(unverifiedUser);

                        return (userId: Id, userData: botUser);
                    }
                }
            }

            return (userId: ulong.MinValue, userData: null);
        }


        public async Task assignRole((ulong userId, CSVData userData) userInfo) {

            var user = client.GetGuild(config.guildId).GetUser(userInfo.userId);

            // If the email is of a speaker then asign the speaker role
            if (userInfo.userData.isSpeaker) {
                var roleSpeaker = client.GetGuild(config.guildId).GetRole(config.speakerRoleID);
                await (user as IGuildUser).AddRoleAsync(roleSpeaker);
            } else {
                // If the email is not of a speaker then assign an attendee role
                var roleAttendee = client.GetGuild(config.guildId).GetRole(config.attendeeRoleID);
                await (user as IGuildUser).AddRoleAsync(roleAttendee);
            }
        }


    }
}
