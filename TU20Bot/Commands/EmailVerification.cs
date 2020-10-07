using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore.Internal;
using TU20Bot.Configuration;
using TU20Bot.Configuration.Payloads;

using TU20Bot.Database;

namespace TU20Bot.Commands {
    public class EmailCompareResult {
        public UserMatch match;
        public UserDetails detail;
    }
    
    public class EmailVerification : ModuleBase<SocketCommandContext> {
        [Command("verify")]
        public async Task emailVerify(string email) {
            // Change dictionary such that it doesn't give an error with the same key
            var config = ((Client)Context.Client).config;
            var unverifiedUser = new DbCommUnverifiedUser(new BotDbContext());

            var result = compareEmail(email, config.matches);

            if (result.detail != null) {
                await ReplyAsync("Email verified");
                
                var guild = Context.Guild ?? Context.Client.GetGuild(config.guildId);
                var user = guild.GetUser(Context.User.Id);
                var role = guild.GetRole(result.match.role);

                await user.AddRoleAsync(role);
            } else {
                await saveUnverifiedEmail(unverifiedUser, Context.User.Id, email);
                await ReplyAsync("Could not verify email. Your email has been saved and will be verified automatically.");
            }

            // If the message was not sent in a private (direct message) channel, remove it
            if (!(Context.Message.Channel is IPrivateChannel))
                await Context.Message.DeleteAsync();
        }

        public static async Task saveUnverifiedEmail(DbCommUnverifiedUser commUnverifiedUser, ulong id, string email) {
            await commUnverifiedUser.addUserInfo(id, email);
        }

        public EmailCompareResult compareEmail(string email, IEnumerable<UserMatch> matches) {
            foreach (var match in matches) {
                var detail = match.details.FirstOrDefault(x => x.email == email);

                if (detail == null)
                    continue;

                return new EmailCompareResult {
                    match = match,
                    detail = detail
                };
            }

            return null;
        }
    }
}
