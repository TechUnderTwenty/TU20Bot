using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;

namespace TU20Bot.Configuration {
    public class CommitController : ServerController {
        [Route(HttpVerbs.Put, "/commit")]
        public string commitConfig() {
            Config.save(Config.defaultPath, server.config);

            return "Done.";
        }
    }
}