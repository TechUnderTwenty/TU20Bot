using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.Routing;

using Swan.Formatters;

using Discord;

namespace TU20Bot.Configuration {
    public class DiscordController : ServerController {
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
    }
}