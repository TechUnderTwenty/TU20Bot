using EmbedIO;
using EmbedIO.Routing;

namespace TU20Bot.Configuration.Controllers {
    public class MatchController : ServerController {
        [Route(HttpVerbs.Get, "/match")]
        public string getMatches() {
            return "Hello World!";
        }
    }
}
