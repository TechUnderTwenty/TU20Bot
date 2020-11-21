using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;
using EmbedIO.Routing;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration.Controllers {
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
    }
}