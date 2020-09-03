using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TU20Bot.Configuration {
    public class EmailChecker {

        private Config config;
        private Client client;

        public EmailChecker() {
            config = new Config();
            client = new Client(config);
        }

        public void checkForEmail() {
            Console.WriteLine(config.userEmailId.Count);
            for (int i = 0; i < config.userEmailId.Count; i++) {
                if (config.emails.Contains(config.userEmailId.ElementAt(i).Value)) {
                    // grant that specific user a role
                    ulong userId = config.userEmailId.ElementAt(i).Key;
                    var user = client.GetUser(userId);
                    Console.WriteLine($"{user} email verified from list");
                    // remove that specific index from the dictionary
                    config.userEmailId.Remove(userId);
                }
            }            
        }
    }
}
