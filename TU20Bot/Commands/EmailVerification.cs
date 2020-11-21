using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using MongoDB.Driver;
 
using TU20Bot.Models;

namespace TU20Bot.Commands {
    public class EmailVerification : ModuleBase<SocketCommandContext> {
        [Command("verify")]
        public async Task verifyEmail([Remainder] string email) {
            var config = ((Client)Context.Client).config;

            var guild = Context.Client.GetGuild(config.guildId);

            if (guild == null) {
                await Context.Channel.SendMessageAsync("Configuration problem :O dm admins please.");
                return;
            }

            if (!email.Contains("@") || !email.Contains(".")) {
                await Context.Channel.SendMessageAsync("Please enter a valid email.");
                return;
            }
            
            var database = ((Client) Context.Client).database;
            if (database != null) {
                var collection = database.GetCollection<UserModel>(UserModel.collectionName);

                // I know there are lambda ways to express this, which are probably better...
                await collection.UpdateOneAsync(
                    Builders<UserModel>.Filter
                        .Eq(x => x.discordId, Context.Message.Author.Id.ToString()),
                    Builders<UserModel>.Update
                        .Set(x => x.email, email)
                        .Set(x => x.discordId, Context.Message.Author.Id.ToString()),
                    new UpdateOptions { IsUpsert = true }
                );
            }

            // TODO: This linq query should be done with MongoDB, but I'm sleepy and I want to do it later.
            var roles = config.userRoleMatches
                .Where(x => x.userDetailInformation.Any(y => y.email == email)) // find any matches that have matching emails
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
            
            // Provide the user feedback.
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("You have been verified. Thanks.");
        }
    }
}
