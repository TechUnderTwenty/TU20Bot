using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Jose;
using MongoDB.Driver;
using TU20Bot.Models;

namespace TU20Bot.Configuration.Controllers {
    public class AuthenticationController : ServerController {

        [Route(HttpVerbs.Post, "/login")]
        public async Task<string> Login([QueryField] string username, [QueryField] string password) {

            var user = await server.client.database.GetCollection<AccountModel>(AccountModel.collectionName).FindAsync(_user => _user.username.Equals(username.ToLower()));

            if (!user.Any()) {
                throw HttpException.Unauthorized();
            }

            if (user.First().checkPassword(password)) {
                return JWT.Encode(new AuthorizationPayload {
                    fullName = username,
                    validUntil = DateTime.Now.AddHours(1), // Expire in 1 hour
                    permissions = user.First().permissions
                }, Encoding.UTF8.GetBytes(this.server.config.jwtSecret), JwsAlgorithm.HS256);
            }

            throw HttpException.Unauthorized();
        }

        [Route(HttpVerbs.Post, "/create")]
        public async Task<string> Create([QueryField] string username, [QueryField] string password) {

            if (!AuthorizationModule.ValidatePermissions(new List<string> { "Admin" }, Request, Response, server.config.jwtSecret)) {
                throw HttpException.Forbidden();
            }

            var user = server.client.database.GetCollection<AccountModel>(AccountModel.collectionName).Find(_user => _user.username.Equals(username.ToLower()));

            // If a user already exists, it is not possible to create a new user
            if (user.Any()) throw HttpException.NotAcceptable();

            var _user = new AccountModel {
                username = username.ToLower(),
                permissions = new List<string> { "Admin" }
            };
            _user.setPassword(password);

            await server.client.database.GetCollection<AccountModel>(AccountModel.collectionName).InsertOneAsync(_user);

            return username.ToLower();
        }
    }
}