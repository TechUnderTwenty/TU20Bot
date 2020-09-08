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

        public void checkForEmail(List<CSVData> csvEmail) {
            for (int i = 0; i < config.userEmailId.Count; i++) {
                if (csvEmail.Any(x => x.Email.Equals(config.userEmailId.ElementAt(i).Value))) {
                    ulong userId = config.userEmailId.ElementAt(i).Key;
                    var user = client.GetUser(userId);
                    Console.WriteLine($"{user} email verified from list");
                    // Remove that specific index from the dictionary since the user has been verified
                    config.userEmailId.Remove(userId);
                }
            }            
        }
    }
}
