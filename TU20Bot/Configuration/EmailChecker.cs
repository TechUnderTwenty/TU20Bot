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

        public void checkForEmail() {
            Console.WriteLine(Config.userEmailId.Count);
            for (int i = 0; i < Config.userEmailId.Count; i++) {
                if (config.emails.Contains(Config.userEmailId.ElementAt(i).Value)) {
                    // grant that specific user a role
                    ulong userId = Config.userEmailId.ElementAt(i).Key;
                    var user = client.GetUser(userId);
                    Console.WriteLine($"{user} email verified from list");
                    // remove that specific index from the dictionary
                    Config.userEmailId.Remove(userId);
                }
            }            
        }
    }
}
