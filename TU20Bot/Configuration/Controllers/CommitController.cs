using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;

namespace TU20Bot.Configuration.Controllers {
    [ControllerInfo(true)]
    public class CommitController : ServerController {
        [Route(HttpVerbs.Put, "/commit")]
        public void commitConfig() {
            Config.save(server.config);
        }
    }
}