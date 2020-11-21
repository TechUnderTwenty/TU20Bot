using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.Routing;

using Discord;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration.Controllers {
    public class DiscordController : ServerController {
        [Route(HttpVerbs.Put, "/discord/server")]
        public async Task setServer() {
            var id = await HttpContext.GetRequestDataAsync<IdJsonPayload>();

            server.config.guildId = ulong.Parse(id.id);
        }
        
        [Route(HttpVerbs.Get, "/discord/server")]
        public object getServer() {
            var guild = server.client.GetGuild(server.config.guildId);

            return new {
                id = guild.Id.ToString(),
                name = guild.Name,
                icon = guild.IconUrl
            };
        }
        
        [Route(HttpVerbs.Get, "/discord/channels")]
        public IEnumerable<object> getChannels([QueryField] string type) {
            // Not going to risk enum recognition.
            var textOnly = type == "text";
            var audioOnly = type == "audio";
            
            var guild = server.client.GetGuild(server.config.guildId);

            var y = guild.Channels
                .Where(x => (!textOnly || x is ITextChannel) && (!audioOnly || x is IAudioChannel))
                .Select(x => new { id = x.Id.ToString(), name = x.Name });

            return y;
        }

        [Route(HttpVerbs.Get, "/discord/roles")]
        public IEnumerable<object> getRoles() {
            var guild = server.client.GetGuild(server.config.guildId);

            return guild.Roles.Select(x => new {
                id = x.Id.ToString(),
                name = x.Name
            });
        }
    }
}