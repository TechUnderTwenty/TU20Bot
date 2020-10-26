using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Driver;

using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Configuration;

namespace BotTest {
    [TestClass]
    public class EmailVerificationTest {
        // TODO: Tests are stubbed for the time being, nothing to test until MongoDB records are in.
        
        private static EmailVerification emailVerification;
        // private static EmailChecker emailChecker;
        private static Config config;
        private static Client client;

        private static MongoClient mongo;
        private static IMongoDatabase database;
        // private static DbCommUnverifiedUser dbComm;
        
        private static readonly List<UserMatch> records = new List<UserMatch> {
            new UserMatch {
                role = 0,
                details = new List<UserDetails> {
                    new UserDetails {
                        firstName = "john1",
                        lastName = "Doe1",
                        email = "johndoe@tu20.com"
                    }
                }
            }
        };

        [ClassInitialize]
        public static void setup(TestContext testContext) {
            config = Config.load() ?? Config.configure();
            // Needs database connection...
            Assert.IsNotNull(config.mongoUrl);
            
            mongo = new MongoClient(config.mongoUrl);
            database = mongo.GetDatabase(config.databaseName);

            client = new Client(config, null);

            // TODO: Implement w/ MongoDB
            // dbComm = new DbCommUnverifiedUser(new BotDbContext());
            emailVerification = new EmailVerification();
            // emailChecker = new EmailChecker(config, client, dbComm);
        }


        [TestMethod]
        public void checkCompareMethod() {
            // Assert.IsNotNull(emailVerification.compareEmail("johndoe@tu20.com", records));
            // Assert.IsNull(emailVerification.compareEmail("johndoe@nothing.com", records));
        }

        [TestMethod]
        public void checkNotInList() {
            // Running the method with an email not in the list 
            // Assert.IsNull(emailVerification.compareEmail("johndoe@examplemail.com", records));

            // TODO: Implement w/ MongoDB
            // Adding the same unavailable email to the csv file and dictionary
            // await EmailVerification.saveUnverifiedEmail(dbComm, 1, "johndoe@examplemail.com");
            
            records.Add(new UserMatch {
                details = new List<UserDetails> {
                    new UserDetails {
                        firstName = "john",
                        lastName = "Doe",
                        email = "johndoe@examplemail.com"
                    }
                }
            });

            // TODO: Implement w/ MongoDB
            // Checking to see if the email is present in the list
            // This will be run on separate thread when program is running
            // var result = emailChecker.checkEmailInCsvList(records);

            // If the function is working properly, it will return the persons info
            // Assert.IsNotNull(result.detail);
        }
    }
}