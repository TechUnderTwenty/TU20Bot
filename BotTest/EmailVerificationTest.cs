using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Configuration;
using Discord;
using System.Linq;

namespace BotTest {
    [TestClass]
    public class EmailVerificationTest {

        private static EmailVerification _emailVerification;
        private static EmailChecker _emailChecker;
        private static Config _config;
        private static Client _client;
        // private static Handler _handler;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext testContext) {

            string token = "";

            // Start bot with token from "token.txt" in working folder.
            try {
                token = File.ReadAllText("token.txt").Trim();
            } catch (IOException) {
                // current directory: BotTest/bin/Debug/netcoreapp3.1
                Console.WriteLine("Could not read from token.txt." +
                                  " Did you put token.txt file in the current directory?");
                Assert.Fail();
            }

            // Initializing all the requored classes
            _config = new Config();
            _client = new Client(_config);
            // _handler = new Handler(_client);
            _emailVerification = new EmailVerification();
            _emailChecker = new EmailChecker(_config, _client);

            // await _handler.init();

            // Logging in the bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Waiting for the bot to log in and appear online
            bool ready = false;
            _client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready)
                ;
        }


        [TestMethod]
        public void CheckCompareMethod() {
            bool emailExists = _emailVerification.emailCompare("johndoe@tu20.com", _config.emails);
            Assert.IsTrue(emailExists);
            bool emailNotInList = _emailVerification.emailCompare("johndoe@nothing.com", _config.emails);
            Assert.IsFalse(emailNotInList);
        }

        [TestMethod]
        public void CheckNotInListEmail() {
            // Running the method with an email not in the list 
            bool emailNotInList = _emailVerification.emailCompare("johndoe@examplemail.com", _config.emails);
            Assert.IsFalse(emailNotInList);

            // Adding the same unavailable email to the list and dictionary
            _emailVerification.saveUnverifiedEmail(_config.userEmailId, 1, "johndoe@examplemail.com");
            _config.emails.Add("johndoe@examplemail.com");

            // Checking to see if the email is present in the list
            // This will be run on separate thread when program is running
            _emailChecker.checkForEmail();

            // If the function is working properly, it will remove the element which can be verified
            Assert.AreEqual(0, _config.userEmailId.Count);
        }
    }
}