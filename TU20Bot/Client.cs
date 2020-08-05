using Discord.WebSocket;

using TU20Bot.Configuration;

namespace TU20Bot {
    public class Client : DiscordSocketClient {
        public readonly Config config;

        public Client(Config config) {
            this.config = config;
        }
    }
}