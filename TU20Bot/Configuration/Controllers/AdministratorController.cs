using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

using MongoDB.Driver;

using TU20Bot.Models;

namespace TU20Bot.Configuration.Controllers {
    public class AdministratorController : ServerController {
        private readonly string[] administrator = { "Admin" };
        
        [Route(HttpVerbs.Post, "/create")]
        public async Task create([QueryField] string username, [QueryField] string password) {
            if (username == null || password == null)
                throw HttpException.BadRequest();
            
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
            if (username == null)
                throw HttpException.BadRequest();
            
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