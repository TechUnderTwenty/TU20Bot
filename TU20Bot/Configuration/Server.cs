using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebApi;

using TU20Bot.Configuration.Controllers;

namespace TU20Bot.Configuration {
    public class Server : WebServer {
        private const string allSources = "http://+";
        private const int port = 3000;

        public readonly Client client;
        public readonly Config config;

        private Func<T> factory<T>() where T : ServerController, new()
            => () => new T { server = this };
        
        public Server(Client client) : base(e => e
            .WithUrlPrefix($"{allSources}:{port}")
            .WithMode(HttpListenerMode.EmbedIO)) {
            this.client = client;
            config = client.config;

            var serverType = GetType();
            
            var assembly = serverType.Assembly;
            if (assembly == null)
                throw new Exception("Assembly missing, server cannot start.");

            var types = assembly.GetTypes();
            
            var factoryMethod = typeof(Server).GetMethod(nameof(factory),
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (factoryMethod == null)
                throw new Exception("Missing factory method, server cannot start.");
            
            var authApi = new WebApiModule("/");
            var openApi = new WebApiModule("/");

            foreach (var type in types) {
                if (type == typeof(ServerController))
                    continue;
                
                if (!typeof(ServerController).IsAssignableFrom(type))
                    continue;

                var attributes = type.GetCustomAttributes(typeof(Controller), true);

                if (!attributes.Any())
                    continue;

                var attribute = (Controller) attributes.First();
                var api = attribute.authorization ? authApi : openApi;

                var genericFactory = (Func<WebApiController>) factoryMethod
                    .MakeGenericMethod(type)
                    .Invoke(this, new object[] { });

                if (genericFactory == null)
                    throw new Exception($"Cannot create factory for {type.Name}.");

                api.WithController(type, genericFactory);
            }

            // This breaks exception messages for the login endpoint but I can't care less.
            openApi.OnHttpException = (context, exception) => {
                if (exception.StatusCode == 404)
                    return Task.CompletedTask;
                
                context.SetHandled();
                exception.PrepareResponse(context);

                if (exception.Message != null) {
                    context.Response.OutputStream.Write(
                        Encoding.UTF8.GetBytes(exception.Message));
                }

                return Task.CompletedTask;
            };
            
            this
                .WithModule(new ModuleGroup("/", false).WithModule(openApi))
                .WithModule(new AuthorizationModule("/", this, authApi))
                .WithLocalSessionManager();
        }
    }
}
