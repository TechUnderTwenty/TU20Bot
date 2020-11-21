using Discord.WebSocket;

using MongoDB.Driver;

using TU20Bot.Configuration;

namespace TU20Bot {
    public class Client : DiscordSocketClient {
        public readonly Config config;
        public readonly IMongoDatabase database;

        public Client(Config config, IMongoDatabase database) {
            this.config = config;
            this.database = database;
        }
    }
}