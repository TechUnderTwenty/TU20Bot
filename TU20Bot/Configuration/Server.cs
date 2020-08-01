using System;
using EmbedIO;
using EmbedIO.WebApi;

namespace TU20Bot.Configuration {
    public class Server : WebServer {
        private const string host = "http://localhost";
        private const int port = 3000;

        private Func<T> createFactory<T>() where T : ServerController, new() {
            return () => new T() { server = this };
        }

        public Server() : base(e => e
            .WithUrlPrefix($"{host}:{port}")
            .WithMode(HttpListenerMode.EmbedIO)) {
            this
                .WithLocalSessionManager()
                .WithWebApi("/", e => e
                    .WithController(createFactory<PingController>()));
        }
    }
}
