using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

namespace TU20Bot.Commands {
    public class EmailVerification : ModuleBase<SocketCommandContext> {
        [Command("verify")]
        public async Task verifyEmail(string email) {
            var config = ((Client)Context.Client).config;

            var guild = Context.Client.GetGuild(config.guildId);

            if (guild == null) {
                await Context.Channel.SendMessageAsync("Configuration problem :O dm admins please.");
                return;
            }

            // TODO: This linq query should be done with MongoDB, but I'm sleepy and I want to do it later.
            var roles = config.matches
                .Where(x => x.details.Any(y => y.email == email)) // find any matches that have matching emails
                .Select(x => guild.GetRole(x.role)) // grab the roles
                .ToList();

            if (roles.Any()) {
                // We want to get the user again to make sure guild information is attached (in case of DM).
                var guildUser = guild.GetUser(Context.User.Id);
                
                await guildUser.AddRolesAsync(roles);
            }

            // If the message was not sent in a private (direct message) channel, remove it
            if (!(Context.Message.Channel is IPrivateChannel))
                await Context.Message.DeleteAsync();
        }
    }
}
