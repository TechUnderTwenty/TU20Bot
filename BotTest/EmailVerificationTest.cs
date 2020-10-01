using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Database;
using TU20Bot.Configuration;
using TU20Bot.Configuration.Payloads;

namespace BotTest {
    [TestClass]
    public class EmailVerificationTest {
        private static EmailVerification emailVerification;
        private static EmailChecker emailChecker;
        private static Config config;
        private static Client client;
        private static DbCommUnverifiedUser dbComm;
        
        private static readonly List<UserMatchPayload> records = new List<UserMatchPayload> {
            new UserMatchPayload {
                role = 0,
                details = new List<UserDetailsPayload> {
                    new UserDetailsPayload {
                        firstName = "john1",
                        lastName = "Doe1",
                        email = "johndoe@tu20.com"
                    }
                }
            }
        };

        [ClassInitialize]
        public static void setup(TestContext testContext) {
            // Initializing all the required classes
            config = new Config();
            client = new Client(config);

            dbComm = new DbCommUnverifiedUser(new BotDbContext());
            emailVerification = new EmailVerification();
            emailChecker = new EmailChecker(config, client, dbComm);
        }


        [TestMethod]
        public void checkCompareMethod() {
            Assert.IsNotNull(emailVerification.compareEmail("johndoe@tu20.com", records));
            Assert.IsNull(emailVerification.compareEmail("johndoe@nothing.com", records));
        }

        [TestMethod]
        public async Task checkNotInList() {
            // Running the method with an email not in the list 
            Assert.IsNull(emailVerification.compareEmail("johndoe@examplemail.com", records));

            // Adding the same unavailable email to the csv file and dictionary
            await EmailVerification.saveUnverifiedEmail(dbComm, 1, "johndoe@examplemail.com");
            
            records.Add(new UserMatchPayload {
                details = new List<UserDetailsPayload> {
                    new UserDetailsPayload {
                        firstName = "john",
                        lastName = "Doe",
                        email = "johndoe@examplemail.com"
                    }
                }
            });

            // Checking to see if the email is present in the list
            // This will be run on separate thread when program is running
            var result = emailChecker.checkEmailInCsvList(records);

            // If the function is working properly, it will return the persons info
            Assert.IsNotNull(result.detail);
        }
    }
}