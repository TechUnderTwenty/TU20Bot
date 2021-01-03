using System;
using EmbedIO;
using EmbedIO.WebApi;

using TU20Bot.Configuration.Controllers;

namespace TU20Bot.Configuration {
    public class Server : WebServer {
        private const string allSources = "http://+";
        private const int port = 3000;

        public readonly Client client;
        public readonly Config config;

        private Func<T> createFactory<T>() where T : ServerController, new() {
            return () => new T { server = this };
        }

        public Server(Client client) : base(e => e
            .WithUrlPrefix($"{allSources}:{port}")
            .WithMode(HttpListenerMode.EmbedIO)) {
            this.client = client;
            config = client.config;

            var api = new WebApiModule("/")
                .WithController(createFactory<PingController>())
                .WithController(createFactory<WelcomeController>())
                .WithController(createFactory<DiscordController>())
                .WithController(createFactory<LogController>())
                .WithController(createFactory<CommitController>())
                .WithController(createFactory<FactoryController>())
                .WithController(createFactory<MatchController>());

            this
                .WithWebApi("/admin", m => m.WithController(createFactory<AuthenticationController>()))
                .WithLocalSessionManager()
                .WithModule(new AuthorizationModule("/", this, api));
        }
    }
}
