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

            bool result = emailCompare(email, config.userDataCsv);

            if (result)
                await ReplyAsync("Email verified");
            else {
                saveUnverifiedEmail(config.userEmailId, Context.User.Id, email);
                await ReplyAsync("Could not verify email. Your email has been saved and will be verified automatically.");
            }
        }

        public bool emailCompare(string email, List<CSVData> csvEmailList) {

            foreach(var userEmail in csvEmailList) {
                if (email.Equals(userEmail.Email))
                    return true;
            }
            return false;
        }

        public void saveUnverifiedEmail(Dictionary<ulong, string> emailIdStore, ulong id, string email) {
            if(!emailIdStore.ContainsKey(id))
                emailIdStore.Add(id, email);
        }


    }
}
