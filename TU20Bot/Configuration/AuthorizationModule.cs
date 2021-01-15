using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;

using Jose;
using Newtonsoft.Json;
using System.Linq;

namespace TU20Bot.Configuration {
    // I kinda hate Swan.Formatters, I'm going to use Newtonsoft.
    public class AuthorizationPayload {

        [JsonProperty]
        public string fullName { get; set; }

        [JsonProperty]
        public DateTime validUntil { get; set; }

        [JsonProperty]
        public List<string> permissions { get; set; }
    }

    public class AuthorizationModule : WebModuleBase {
        private readonly Server server;
        private readonly IWebModule module;
        private const string bearerPrefix = "Bearer ";

        private static void error(string text, IHttpResponse response) {
            response.StatusCode = 400;
            var writer = new StreamWriter(response.OutputStream);
            writer.WriteLine(text);
            writer.Flush();
        }

        protected override async Task OnRequestAsync(IHttpContext context) {
            if (!validateToken(context.Request, context.Response)) {
                throw HttpException.Unauthorized();
            }

            var authorization = context.Request.Headers["Authorization"];

            // Extract the incoming JWT Token from the header
            var token = authorization.Substring(bearerPrefix.Length);

            Exception moduleError = null;

            try {
                var secret = Encoding.UTF8.GetBytes(server.config.jwtSecret);
                var result = JWT.Decode(token, secret);

                if (string.IsNullOrEmpty(result)) {
                    error("ERROR: Empty payload.", context.Response);
                    return;
                }

                // Deserialize the JWT Token into an AuthorizationPayload object
                var payload = JsonConvert.DeserializeObject<AuthorizationPayload>(result);

                if (payload == null) {
                    error("ERROR: Invalid payload.", context.Response);
                    return;
                }

                // If the token is expired
                if (DateTime.Now.CompareTo(payload.validUntil) > 0) {
                    error("ERROR: Expired token.", context.Response);
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
                error("ERROR: Invalid JWT signature.", context.Response);
            } catch (Exception) {
                error("Error: Failure to parse JWT.", context.Response);
            }

            if (moduleError != null) {
                throw moduleError;
            }
        }

        /// <summary>
        /// Check whether the incoming Authorisation header is not null and contains the expected bearer token
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool validateToken(IHttpRequest request, IHttpResponse response) {
            var authorization = request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authorization)) {
                error("ERROR: No authorization token provided.", response);
                return false;
            }

            // I completely do not understand this pattern, and I want someone to tell me why. - Taylor
            if (!authorization.StartsWith(bearerPrefix)) {
                error("ERROR: Invalid authorization token provided.", response);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if the user of the current session is signed in and if they contain the appropriate permissions
        /// </summary>
        /// <param name="permissions"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="jwtSecret"></param>
        /// <returns></returns>
        public static bool validatePermissions(List<string> permissions, IHttpRequest request, IHttpResponse response, string jwtSecret) {
            if (!validateToken(request, response)) return false;

            var authorization = request.Headers["Authorization"];

            // Extract the incoming JWT Token from the header
            var token = authorization.Substring(bearerPrefix.Length);

            var secret = Encoding.UTF8.GetBytes(jwtSecret);

            try {
                var result = JWT.Decode(token, secret);
                var payload = JsonConvert.DeserializeObject<AuthorizationPayload>(result);

                // Return if the payload contains the desired permission information
                return payload.permissions.Any(_permission => permissions.Contains(_permission));
            } catch {
                return false;
            }
        }

        public AuthorizationModule(string baseRoute, Server server, IWebModule module) : base(baseRoute) {
            this.server = server;
            this.module = module;
        }

        public override bool IsFinalHandler => module.IsFinalHandler;
    }
}