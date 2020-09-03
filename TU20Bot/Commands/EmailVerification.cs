using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class EmailVerification : ModuleBase<SocketCommandContext> {

        private Config config = new Config();

        [Command("verify")]
        public async Task EmailVerify(string email) {

            string result = emailCompare(email, Context.User.Id);

            if (result != null)
                await ReplyAsync(result);
            else
                await ReplyAsync("Could not verify email. Your email has been saved and will be verified automatically.");
        }

        public string emailCompare(string email, ulong userId) {
            for (int i = 0; i < config.emails.Count; i++) {
                if (email.Equals(config.emails[i]))
                    return "Email verified";
            }

            // Since the email didn't match, adding user to the dictionary
            Config.userEmailId.Add(userId, email);
            return null;
        }
    }
}
