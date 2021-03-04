using Discord.WebSocket;

using MongoDB.Driver;

using TU20Bot.Configuration;

namespace TU20Bot {
    public class Client : DiscordSocketClient {
        public readonly Config config;
        public readonly Handler handler;
        public readonly IMongoDatabase database;

        public Client(Config config, Handler handler, IMongoDatabase database) {
            this.config = config;
            this.handler = handler;
            this.database = database;
        }
    }
}