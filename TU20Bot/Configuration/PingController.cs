using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace TU20Bot.Configuration {
    public class PingController : ServerController {
        [Route(HttpVerbs.Get, "/ping")]
        public string ping() {
            return "Pong";
        }
    }
}
