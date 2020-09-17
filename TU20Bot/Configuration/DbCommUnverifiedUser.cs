using Microsoft.EntityFrameworkCore.Internal;
using Org.BouncyCastle.Math.EC.Rfc7748;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TU20Bot.Configuration {
    public class DbCommUnverifiedUser {
        private BotDbContext db;

        public DbCommUnverifiedUser(BotDbContext context) {
            db = context;
        }

        // Adds the info given in the parameters to the db
        public void addUserInfo(ulong userId, string email) {
            db.unverifiedUsers.AddAsync(new UnverifiedUser {
                UserId = userId,
                Email = email,
            });
            db.SaveChangesAsync();
        }

        // Returns a list of all the users stored in the db
        public List<UnverifiedUser> getUserList() {
            return db.unverifiedUsers.ToList();
        }

        // Removes a sepcific row from the db
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
