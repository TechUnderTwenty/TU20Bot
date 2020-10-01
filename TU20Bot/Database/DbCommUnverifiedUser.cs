using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using TU20Bot.Models;

namespace TU20Bot.Database {
    public class DbCommUnverifiedUser {
        private readonly BotDbContext db;

        public DbCommUnverifiedUser(BotDbContext context) {
            db = context;
        }

        // Adds the info given in the parameters to the db
        public async Task addUserInfo(ulong userId, string email) {
            await db.unverifiedUsers.AddAsync(new UnverifiedUser {
                userId = userId,
                email = email,
            });
           await db.SaveChangesAsync();
        }

        // Returns a list of all the users stored in the db
        public List<UnverifiedUser> getUserList() {
            return db.unverifiedUsers.ToList();
        }

        // Removes a specific row from the db
        public void removeUserInfo(UnverifiedUser user) {
            db.unverifiedUsers.Remove(user);

            try {
                db.SaveChanges();
            } catch (Exception exception) {
                Console.WriteLine(exception.Message);
            }
        }

    }
}
