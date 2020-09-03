using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TU20Bot.Configuration;

namespace TU20Bot.Commands {
    public class EmailVerification : ModuleBase<SocketCommandContext> {

        private Config _config = new Config();

        // For storing email of people who aren't in the list but ran the command
        // TODO: change it to public thing and run it on a new thread and keep on updating it all the time
        // TODO: how do we grant them a role for the event
        

        [Command("verify")]
        public async Task EmailVerify(string email) {

            string result = emailCompare(email);

            if (result != null)
                await ReplyAsync(result);
            else
                await ReplyAsync("Could not verify email. Your email has been saved and will be verified automatically.");
        }

        public string emailCompare(string email) {
            for (int i = 0; i < _config.emails.Count; i++) {
                if (email.Equals(_config.emails[i]))
                    return "email verified";
            }

            // Since the email didn't match, adding user to the dictionary
            _config.userEmailId.Add(Context.User.Id, email);
            return null;
        }

    }
}
