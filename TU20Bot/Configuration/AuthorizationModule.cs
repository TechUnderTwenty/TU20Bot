using System;
using System.Linq;
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

        [JsonProperty]
        public List<string> permissions { get; set; }
    }

    public class AuthorizationModule : WebModuleBase {
        private readonly Server server;
        private readonly IWebModule module;
        private const string bearerPrefix = "Bearer ";

        protected override async Task OnRequestAsync(IHttpContext context) {
            try {
                if (server.config.jwtSecret == null) {
                    await module.HandleRequestAsync(context);

                    return;
                }

                var authorizationHeader = context.Request.Headers["Authorization"];

                if (!tokenIsValid(authorizationHeader))
                    throw HttpException.Unauthorized("Token is invalid.");

                // Extract the incoming JWT Token from the header
                var token = authorizationHeader[bearerPrefix.Length..];

                Exception moduleError = null;

                try {
                    var secret = Encoding.UTF8.GetBytes(server.config.jwtSecret);
                    var result = JWT.Decode(token, secret);

                    var payload = attemptValidateToken(result);

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
                    throw HttpException.Forbidden("Invalid JWT signature.");
                } catch (Exception e) {
                    throw HttpException.InternalServerError(e.Message);
                }

                if (moduleError != null) {
                    throw moduleError;
                }
            }  catch (HttpException e) {
                if (e.StatusCode == 404)
                    throw RequestHandler.PassThrough();

                throw;
            }
        }

        /// <summary>
        /// Check whether the incoming Authorisation header is not null and contains the expected bearer token
        /// </summary>
        private static bool tokenIsValid(string token)
            => !string.IsNullOrEmpty(token) && token.StartsWith(bearerPrefix);

        /// <summary>
        /// Parses a string as JWT and, if valid, validates it's properties
        /// </summary>
        /// <returns>AuthorizationPayload instance from the JWT token</returns>
        /// <throws>Exception</throws>
        private static AuthorizationPayload attemptValidateToken(string jwtToken) {
            if (string.IsNullOrEmpty(jwtToken)) {
                throw new Exception("ERROR: Empty payload.");
            }

            // Deserialize the JWT Token into an AuthorizationPayload object
            var payload = JsonConvert.DeserializeObject<AuthorizationPayload>(jwtToken);

            if (payload == null) {
                throw new Exception("ERROR: Invalid payload.");
            }

            // If the token is expired
            if (DateTime.Now.CompareTo(payload.validUntil) > 0) {
                throw new Exception("ERROR: Expired token.");
            }

            return payload;
        }

        /// <summary>
        /// Check if the user of the current session is signed in and if they contain the appropriate permissions
        /// </summary>
        public static bool validatePermissions(IEnumerable<string> permissions, string token, string jwtSecret) {
            if (!tokenIsValid(token))
                return false;

            // Extract the incoming JWT Token from the header
            var jwt = token[bearerPrefix.Length..];

            try {
                var result = JWT.Decode(jwt, Encoding.UTF8.GetBytes(jwtSecret));
                var payload = JsonConvert.DeserializeObject<AuthorizationPayload>(result);

                // Return if the payload contains the desired permission information
                return payload.permissions.Any(permissions.Contains);
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
