using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.Routing;

using Jose;

using MongoDB.Driver;

using TU20Bot.Models;

namespace TU20Bot.Configuration.Controllers {
    [Controller(false)]
    public class AuthenticationController : ServerController {
        [Route(HttpVerbs.Post, "/login")]
        public string login([QueryField] string username, [QueryField] string password) {
            if (username == null || password == null)
                throw HttpException.BadRequest();

            var user = server.client.database
                .GetCollection<AccountModel>(AccountModel.collectionName)
                .Find(x => x.username == username.ToLower());

            if (!user.Any()) {
                throw HttpException.Unauthorized();
            }

            if (!user.First().checkPassword(password))
                throw HttpException.Unauthorized();

            return JWT.Encode(new AuthorizationPayload {
                fullName = username,
                validUntil = DateTime.Now.AddHours(1), // Expire in 1 hour
                permissions = user.First().permissions
            }, Encoding.UTF8.GetBytes(server.config.jwtSecret), JwsAlgorithm.HS256);
        }
    }
}
