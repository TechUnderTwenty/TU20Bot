using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;
using EmbedIO;
using EmbedIO.Routing;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration {
    public class FactoryController : ServerController {
        [Route(HttpVerbs.Get, "/factory")]
        public IEnumerable<object> getFactories() {
            foreach (var factory in server.config.factories) {
                yield return new {
                    id = factory.id.ToString(),
                    name = factory.name,
                    maxChannels = factory.maxChannels,
                    channels = factory.channels
                };
            }
        }
        
        [Route(HttpVerbs.Get, "/factory/{id}")]
        public object getFactory(ulong id) {
            var factory = server.config.factories.First(x => x.id == id);

            return new {
                id = factory.id.ToString(),
                name = factory.name,
                maxChannels = factory.maxChannels,
                channels = factory.channels
            };
        }
        
        [Route(HttpVerbs.Post, "/factory/{id}")]
        public void createFactory(ulong id) {
            if (server.config.factories.Any(x => x.id == id)) {
                HttpContext.Response.StatusCode = 403;
                return;
            }
            
            var channel = (IChannel)server.client.GetChannel(id);
                
            var factory = new FactoryDescription {
                id = id,
                name = channel.Name,
                maxChannels = 5
            };
            
            server.config.factories.Add(factory);
        }
        
        [Route(HttpVerbs.Put, "/factory/{id}")]
        public async Task editFactory(ulong id) {
            var factory = server.config.factories.First(x => x.id == id);

            var description = await HttpContext.GetRequestDataAsync<FactoryJsonPayload>();

            factory.name = description.name;
            factory.maxChannels = description.maxChannels;
        }

        [Route(HttpVerbs.Delete, "/factory/{id}")]
        public void deleteFactory(ulong id) {
            server.config.factories.RemoveAll(x => x.id == id);
        }

        [Route(HttpVerbs.Put, "/factory/{id}/clean")]
        public async Task cleanFactory(ulong id) {
            var factory = server.config.factories.First(x => x.id == id);
            
            foreach (var channel in factory.channels)
                await ((SocketVoiceChannel)server.client.GetChannel(channel)).DeleteAsync();
            
            factory.channels.Clear();
        }
    }
}