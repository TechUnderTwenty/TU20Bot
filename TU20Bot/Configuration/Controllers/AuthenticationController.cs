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
    public class AuthenticationController : ServerController {
        [Route(HttpVerbs.Post, "/login")]
        public string login([QueryField] string username, [QueryField] string password) {

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

        private readonly string[] administrator = { "Admin" };
        
        [Route(HttpVerbs.Post, "/create")]
        public async Task create([QueryField] string username, [QueryField] string password) {
            var validated = AuthorizationModule.validatePermissions(
                administrator, Request.Headers["Authorization"], server.config.jwtSecret);
            
            if (!validated)
                throw HttpException.Forbidden();

            var collection = server.client.database
                .GetCollection<AccountModel>(AccountModel.collectionName);
            var user = collection.Find(x => x.username.Equals(username.ToLower()));

            // If a user already exists, it is not possible to create a new user
            if (await user.AnyAsync())
                throw HttpException.NotAcceptable();

            var model = new AccountModel {
                username = username.ToLower(), // :to_lower_thinking: - Taylor
                permissions = new List<string> { "Admin" }
            };
            model.setPassword(password);

            await collection.InsertOneAsync(model);
        }

        [Route(HttpVerbs.Delete, "/delete")]
        public async Task delete([QueryField] string username) {
            var validated = AuthorizationModule.validatePermissions(
                administrator, Request.Headers["Authorization"], server.config.jwtSecret);
            
            if (!validated)
                throw HttpException.Forbidden();

            var collection = server.client.database
                .GetCollection<AccountModel>(AccountModel.collectionName);
            var user = collection.Find(x => x.username.Equals(username.ToLower()));

            if (!await user.AnyAsync())
                throw HttpException.NotFound();

            var model = user.First();
            var result = await collection.DeleteOneAsync(
                Builders<AccountModel>.Filter.Eq("id", model.id));

            if (result.DeletedCount != 1)
                throw HttpException.InternalServerError();
        }
    }
}
