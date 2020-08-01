using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;
using EmbedIO.Routing;

using Swan.Formatters;

namespace TU20Bot.Configuration {
    class WelcomeChannelInput {
        [JsonProperty("id")]
        public string id { get; set; }
    }
    
    public class WelcomeController : ServerController {
        [Route(HttpVerbs.Get, "/welcome/channel")]
        public string getWelcomeChannel() {
            return server.config.welcomeChannelId.ToString();
        }

        [Route(HttpVerbs.Put, "/welcome/channel")]
        public async Task setWelcomeChannel() {
            var input = await HttpContext.GetRequestDataAsync<WelcomeChannelInput>();

            server.config.welcomeChannelId = ulong.Parse(input.id);
        }
        
        [Route(HttpVerbs.Get, "/welcome/messages")]
        public List<string> getWelcomeMessages() {
            return server.config.welcomeMessages;
        }
        
        [Route(HttpVerbs.Put, "/welcome/messages")]
        public async Task setWelcomeMessages() {
            var messages = await HttpContext.GetRequestDataAsync<List<string>>();

            server.config.welcomeMessages = messages;
        }
    }
}