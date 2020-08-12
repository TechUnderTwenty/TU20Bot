using System.Linq;
using System.Threading.Tasks;

using Discord;

using EmbedIO;
using EmbedIO.Routing;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration {
    public class FactoryController : ServerController {
        [Route(HttpVerbs.Get, "/factory/{id}")]
        public object getFactory(ulong id) {
            var factory = server.config.factories.First(x => x.id == id);

            return new {
                id = factory.id.ToString(),
                name = factory.name,
                maxChannels = factory.maxChannels
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
    }
}