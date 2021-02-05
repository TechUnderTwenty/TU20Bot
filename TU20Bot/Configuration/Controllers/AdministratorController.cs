using System.Threading.Tasks;
using System.Collections.Generic;

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

using MongoDB.Driver;

using TU20Bot.Models;

namespace TU20Bot.Configuration.Controllers {
    using AccountCollection = IMongoCollection<AccountModel>;

    public class AdministratorController : ServerController {
        private readonly string[] administratorPermissions = { "Admin" };
        
        private bool validated =>
            AuthorizationModule.validatePermissions(
                administratorPermissions, Request.Headers["Authorization"], server.config.jwtSecret
            );

        private AccountCollection accountCollection =>
            server.client.database.GetCollection<AccountModel>(AccountModel.collectionName);

        // async/await + pure does not play well together :|
        // this looks really ugly, but I don't want to make it non-pure
        private async Task<AccountModel> getUser(string username) =>
            await (
                await accountCollection.FindAsync(Builders<AccountModel>.Filter
                    .Eq(x => x.username, username))
            ).FirstOrDefaultAsync();

        [Route(HttpVerbs.Post, "/create")]
        public async Task create([QueryField] string username, [QueryField] string password) {
            if (username == null || password == null)
                throw HttpException.BadRequest();
            
            if (!validated)
                throw HttpException.Forbidden();

            // If a user already exists, it is not possible to create a new user
            if (await getUser(username) != null)
                throw HttpException.NotAcceptable();

            var model = new AccountModel {
                username = username.ToLower(), // :to_lower_thinking: - Taylor
                permissions = new List<string> { "Admin" }
            };
            model.setPassword(password);

            await accountCollection.InsertOneAsync(model);
        }

        [Route(HttpVerbs.Delete, "/delete")]
        public async Task delete([QueryField] string username) {
            if (username == null)
                throw HttpException.BadRequest();

            if (!validated)
                throw HttpException.Forbidden();

            var user = await getUser(username);

            if (user == null)
                throw HttpException.NotFound();

            var result = await accountCollection.DeleteOneAsync(
                Builders<AccountModel>.Filter.Eq(x => x.id, user.id));

            if (result.DeletedCount != 1)
                throw HttpException.InternalServerError();
        }
    }
}