using EmbedIO;
using EmbedIO.Routing;

namespace TU20Bot.Configuration.Controllers {
    public class PingController : ServerController {
        [Route(HttpVerbs.Get, "/ping")]
        public string ping() {
            return "Pong";
        }
    }
}
