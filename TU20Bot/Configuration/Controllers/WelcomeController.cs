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

        // Too lazy to move this to another endpoint/create another controller.
        [Route(HttpVerbs.Get, "/welcome/error/channel")]
        public string getErrorChannel() {
            return server.config.errorChannelId.ToString();
        }

        [Route(HttpVerbs.Put, "/welcome/error/channel")]
        public async Task setErrorChannel() {
            var input = await HttpContext.GetRequestDataAsync<IdJsonPayload>();

            server.config.errorChannelId = ulong.Parse(input.id);
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