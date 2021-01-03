using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;

using Jose;
using Newtonsoft.Json;

namespace TU20Bot.Configuration {
    // I kinda hate Swan.Formatters, I'm going to use Newtonsoft.
    public class AuthorizationPayload {
        
        [JsonProperty]
        public string fullName { get; set; }

        [JsonProperty]
        public DateTime validUntil { get; set; }
    }
    
    public class AuthorizationModule : WebModuleBase {
        private readonly Server server;
        private readonly IWebModule module;
        private const string bearerPrefix = "Bearer ";

        protected override async Task OnRequestAsync(IHttpContext context) {
            var authorization = context.Request.Headers["Authorization"];

            void error(string text) {
                context.Response.StatusCode = 400;
                var writer = new StreamWriter(context.Response.OutputStream);
                writer.WriteLine(text);
                writer.Flush();
            }

            if (string.IsNullOrEmpty(authorization)) {
                error("ERROR: No authorization token provided.");
                return;
            }
            
            // I completely do not understand this pattern, and I want someone to tell me why. - Taylor
            if (!authorization.StartsWith(bearerPrefix)) {
                error("ERROR: Invalid authorization token provided.");
                return;
            }

            // Extract the incoming JWT Token from the header
            var token = authorization.Substring(bearerPrefix.Length);

            Exception moduleError = null;

            try {
                var secret = Encoding.UTF8.GetBytes(server.config.jwtSecret);
                var result = JWT.Decode(token, secret);

                if (string.IsNullOrEmpty(result)) {
                    error("ERROR: Empty payload.");
                    return;
                }

                // Deserialize the JWT Token into an AuthorizationPayload object
                var payload = JsonConvert.DeserializeObject<AuthorizationPayload>(result);

                if (payload == null) {
                    error("ERROR: Invalid payload.");
                    return;
                }

                // If the token is expired
                if (DateTime.Now.CompareTo(payload.validUntil) > 0) {
                    error("ERROR: Expired token.");
                    return;
                }

                Console.WriteLine(
                    "[Authorization] Access for {0} to \"{1}\".",
                    payload.fullName, context.Request.Url);

                // Direct the module to handle the request
                try {
                    await module.HandleRequestAsync(context);
                } catch (Exception e) {
                    moduleError = e;
                }

            } catch (IntegrityException) {
                error("ERROR: Invalid JWT signature.");
            } catch (Exception) {
                error("Error: Failure to parse JWT.");
            }

            if (moduleError != null) {
                throw moduleError;
            }
        }

        public AuthorizationModule(string baseRoute, Server server, IWebModule module) : base(baseRoute) {
            this.server = server;
            this.module = module;
        }

        public override bool IsFinalHandler => module.IsFinalHandler;
    }
}