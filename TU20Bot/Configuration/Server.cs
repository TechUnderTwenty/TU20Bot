using System;
using EmbedIO;
using EmbedIO.WebApi;

namespace TU20Bot.Configuration {
    public class Server : WebServer {
        private const string hostIp = "http://127.0.0.1";
        private const string hostLocal = "http://localhost";
        private const int port = 3000;

        public readonly Client client;
        public readonly Config config;

        private Func<T> createFactory<T>() where T : ServerController, new() {
            return () => new T() { server = this };
        }

        public Server(Client client) : base(e => e
            .WithUrlPrefix($"{hostIp}:{port}") // helps with vue dev server integration
            .WithUrlPrefix($"{hostLocal}:{port}")
            .WithMode(HttpListenerMode.EmbedIO)) {
            this.client = client;
            config = client.config;
            
            this
                .WithLocalSessionManager()
                .WithWebApi("/", e => e
                    .WithController(createFactory<PingController>())
                    .WithController(createFactory<WelcomeController>())
                    .WithController(createFactory<DiscordController>())
                    .WithController(createFactory<LogController>())
                    .WithController(createFactory<CommitController>()));
        }
    }
}
