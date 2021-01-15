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

        public static void error(string text, IHttpResponse response) {
            response.StatusCode = 400;
            var writer = new StreamWriter(response.OutputStream);
            writer.WriteLine(text);
            writer.Flush();
        }

        protected override async Task OnRequestAsync(IHttpContext context) {
            var authorizationHeader = context.Request.Headers["Authorization"];

            var response = tokenExists(authorizationHeader);

            if (tokenExists(authorizationHeader) != null) {
                error(response, context.Response);
                throw HttpException.Unauthorized();
            }

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
                error("ERROR: Invalid JWT signature.", context.Response);
            } catch (Exception _error) {
                error(_error.Message, context.Response);
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
        public static string tokenExists(string authorisationHeader) {

            if (string.IsNullOrEmpty(authorisationHeader)) {
                return "ERROR: No authorization token provided.";
            }

            // I completely do not understand this pattern, and I want someone to tell me why. - Taylor
            if (!authorisationHeader.StartsWith(bearerPrefix)) {
                return "ERROR: Invalid authorization token provided.";
            }

            return null;
        }

        /// <summary>
        /// Parses a string as JWT and, if valid, validates it's properties
        /// </summary>
        /// <param name="jwtToken"></param>
        /// <returns>AuthorizationPayload instance from the JWT token</returns>
        /// <throws>Exception</throws>
        public static AuthorizationPayload attemptValidateToken(string jwtToken) {

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
        /// <param name="permissions"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="jwtSecret"></param>
        /// <returns></returns>
        public static bool validatePermissions(List<string> permissions, string httpHeader, string jwtSecret) {
            if (tokenExists(httpHeader) != null) return false;

            var authorizationHeader = httpHeader;

            // Extract the incoming JWT Token from the header
            var token = authorizationHeader[bearerPrefix.Length..];

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

        /// <summary>
        /// Same as `AuthorizationModule#validatePermissions(List<string> permissions, string httpHeader, string jwtSecret)`,
        ///     except it additionally writes and flushes errors to httpResponse.
        /// </summary>
        /// <param name="permissions"></param>
        /// <param name="header"></param>
        /// <param name="jwtSecret"></param>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public static bool validatePermissions(List<string> permissions, string header, string jwtSecret, IHttpResponse httpResponse) {
            var response = tokenExists(header);

            if (response != null) {
                // Write an error to the HTTP stream
                error(response, httpResponse);
                return false;
            }

            return validatePermissions(permissions, header, jwtSecret);
        }

        public AuthorizationModule(string baseRoute, Server server, IWebModule module) : base(baseRoute) {
            this.server = server;
            this.module = module;
        }

        public override bool IsFinalHandler => module.IsFinalHandler;
    }
}