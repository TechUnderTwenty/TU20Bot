using System;
using System.Security.Cryptography;
using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Jose;

namespace TU20Bot.Configuration.Controllers {
    public class AuthenticationController: ServerController {
        public const int HASH_SIZE = 24;
        private const int ITERATIONS = 1000000;
        private static readonly string tempSalt = Environment.GetEnvironmentVariable("TEMPSALT");
        private static readonly string tempSalt_default = "TEMPSALT";

        [Route(HttpVerbs.Post, "/login")]
        public string Login([QueryField] string password) {

            var stringHash = hashPassword(password);
            if(stringHash.Equals(this.server.config.consolePassword))
                return JWT.Encode(new AuthorizationPayload{
                    fullName = "Admin",
                    validUntil = DateTime.Now.AddHours(1) // Expire in 1 hour
                }, Encoding.UTF8.GetBytes(this.server.config.jwtSecret), JwsAlgorithm.HS256);

            return null;
        }

        /// <summary>
        /// Given a password string, create a PBKDF2 Hash
        /// </summary>
        public static string hashPassword(string password) {
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(tempSalt != null ? tempSalt : tempSalt_default), ITERATIONS);
            return Convert.ToBase64String(pbkdf2.GetBytes(HASH_SIZE));
        }
    }
}
