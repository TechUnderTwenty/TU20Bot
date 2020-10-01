using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using TU20Bot.Database;
using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration {
    public class EmailChecker {
        public class EmailMatchResult {
            public ulong id;
            public UserMatchPayload match;
            public UserDetailsPayload detail;
        }
        
        private Config config;
        private Client client;
        private DbCommUnverifiedUser dbComm;

        public EmailChecker(Config config, Client client, DbCommUnverifiedUser dbComm) {
            this.config = config;
            this.client = client;
            this.dbComm = dbComm;
        }
        
        // Method running on a separate thread and matching any unverified email to the email list in csv
        public async Task emailCheck(List<UserMatchPayload> matches) {
            var result = checkEmailInCsvList(matches);

            if (result == null)
                return;

            var guild = client.GetGuild(config.guildId);
            var role = guild.GetRole(result.match.role);
            var user = guild.GetUser(result.id);

            if (role != null && user != null)
                await user.AddRoleAsync(role);
            
            Console.WriteLine($"User email with id {result.id} verified."); // REVIEW!! Is for debugging really.
        }

        // Method comparing and returning the CSVData and ulong of the user who's email have been verified
        public EmailMatchResult checkEmailInCsvList(List<UserMatchPayload> matches) {
            var unverifiedUserList = dbComm.getUserList();

            foreach (var unverifiedUser in unverifiedUserList) {
                // Comparing all emails in the newly obtained csv list with unverified emails
                foreach (var match in matches) {
                    foreach (var detail in match.details) {
                        // If some unverified email matches the email from the csv list,
                        if (!detail.email.Equals(unverifiedUser.email))
                            continue;

                        // Get the user id of user associated with that email
                        var id = unverifiedUser.userId; // REVIEW!! I don't feel comfortable about this!!

                        // Remove that specific index from the dictionary since the user has been verified
                        dbComm.removeUserInfo(unverifiedUser);

                        return new EmailMatchResult {
                            id = id,
                            match = match,
                            detail = detail
                        };
                    }
                }
            }

            return null;
        }
    }
}
