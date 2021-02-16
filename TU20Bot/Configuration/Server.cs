using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using EmbedIO;
using EmbedIO.WebApi;

namespace TU20Bot.Configuration {
    public class Server : WebServer {
        private const string allSources = "http://+";
        private const int port = 3000;

        public readonly Client client;
        public readonly Config config;

        private Func<T> makeFactory<T>() where T : ServerController, new()
            => () => new T { server = this };
        
        public Server(Client client) : base(e => e
            .WithUrlPrefix($"{allSources}:{port}")
            .WithMode(HttpListenerMode.EmbedIO)) {
            this.client = client;
            config = client.config;

            // Grab types at runtime.
            var assembly = GetType().Assembly;
            if (assembly == null)
                throw new Exception("Assembly missing, server cannot start.");

            var types = assembly.GetTypes();
            
            // Will be used to create controller generic factories for EmbedIO.
            var factoryMethod = typeof(Server).GetMethod(nameof(makeFactory),
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (factoryMethod == null)
                throw new Exception("Missing factory method, server cannot start.");
            
            // authApi -> Routes that require authentication (AuthenticationModule).
            // openApi -> Any open routes (/login).
            var authApi = new WebApiModule("/");
            var openApi = new WebApiModule("/");

            foreach (var type in types) {
                // Look through to find a type that extends ServerController and has the Controller attribute.
                if (type == typeof(ServerController))
                    continue;
                
                if (!typeof(ServerController).IsAssignableFrom(type))
                    continue;

                var attributes = type.GetCustomAttributes(typeof(ControllerInfo), true);
                if (!attributes.Any())
                    continue;

                // authorization fields will be used to decide if endpoints will be protected by AuthorizationModule.
                var attribute = (ControllerInfo) attributes.First();
                var api = attribute.authorization ? authApi : openApi;

                // Make the factory.
                var genericFactory = (Func<WebApiController>) factoryMethod
                    .MakeGenericMethod(type)
                    .Invoke(this, new object[] { });

                if (genericFactory == null)
                    throw new Exception($"Cannot create factory for {type.Name}.");

                // Add the controller.
                api.WithController(type, genericFactory);
            }

            // This breaks exception messages for the login endpoint but I can't care less.
            openApi.OnHttpException = (context, exception) => {
                // We want to ignore Not Found exceptions so routes will get passed down to AuthorizationModule.
                if (exception.StatusCode == 404)
                    return Task.CompletedTask;
                
                // Otherwise, do our best to make a friendly error message.
                context.SetHandled();
                exception.PrepareResponse(context);

                if (exception.Message != null) {
                    context.Response.OutputStream.Write(
                        Encoding.UTF8.GetBytes(exception.Message));
                }

                return Task.CompletedTask;
            };
            
            // Register our modules.
            this
                .WithModule(new ModuleGroup("/", false).WithModule(openApi))
                .WithModule(new AuthorizationModule("/", this, authApi))
                .WithLocalSessionManager();
        }
    }
}
