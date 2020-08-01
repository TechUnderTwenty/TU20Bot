using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using EmbedIO;
using EmbedIO.Routing;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration {
    public class WelcomeController : ServerController {
        [Route(HttpVerbs.Get, "/welcome/channel")]
        public string getWelcomeChannel() {
            return server.config.welcomeChannelId.ToString();
        }

        [Route(HttpVerbs.Put, "/welcome/channel")]
        public async Task setWelcomeChannel() {
            var input = await HttpContext.GetRequestDataAsync<IdJsonPayload>();

            server.config.welcomeChannelId = ulong.Parse(input.id);
        }
        
        [Route(HttpVerbs.Get, "/welcome/messages")]
        public IEnumerable<string> getWelcomeMessages() {
            return server.config.welcomeMessages;
        }
        
        [Route(HttpVerbs.Put, "/welcome/messages")]
        public async Task setWelcomeMessages() {
            var messages = await HttpContext.GetRequestDataAsync<List<string>>();

            server.config.welcomeMessages = messages;
        }

        [Route(HttpVerbs.Get, "/welcome/logs")]
        public IEnumerable<DiscordUserJoinPayload> getWelcomeLogs() {
            var guild = server.client.GetGuild(server.config.guildId);

            return server.config.usersJoined
                .OrderBy(x => x.time)
                .Select(x => {
                    // rider says i should use pattern matching but i hate it what the heck is going on
                    if (!(guild.GetUser(x.id) is IGuildUser user)) {
                        return new DiscordUserJoinPayload {
                            id = x.id.ToString(),
                            joinDate = x.time.UtcDateTime,
                            leftServer = true
                        };
                    }
                    
                    return new DiscordUserJoinPayload {
                        id = x.id.ToString(),
                        joinDate = x.time.UtcDateTime,
                        username = user.Username,
                        discriminator = user.DiscriminatorValue,
                        nickname = user.Nickname
                    };
                });
        }
    }
}