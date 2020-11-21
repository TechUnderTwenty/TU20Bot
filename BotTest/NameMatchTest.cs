using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using TU20Bot;
using TU20Bot.Support;
using TU20Bot.Configuration;

namespace BotTest {
    [TestClass]
    public class NameMatchTest {
        private static Config config;
        private static Client client;
        
        private readonly List<UserDetails> nameSet = new List<UserDetails>{
            new UserDetails {
                firstName = "John",
                lastName = "Doe",
                email = "johndoe@tu20.com"
            }
        };

        [ClassInitialize]
        public static async Task setup(TestContext testContext) {
            string token;

            // Start bot with token from "token.txt" in working folder.
            try {
                token = await File.ReadAllTextAsync("token.txt");
            } catch (IOException) {
                // current directory: BotTest/bin/Debug/netcoreapp3.1
                Console.WriteLine("Could not read from token.txt." +
                    " Did you put token.txt file in the current directory?");
                return;
            }

            // Initializing all the required classes
            config = new Config();
            client = new Client(config, null);

            config.userRoleMatches.Add(new UserMatch {
                role = 0,
                userDetailInformation = CSVReader.readFile()
            });

            // Logging in the bot
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            
            var ready = false;
            client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready) { }
        }

        [TestMethod]
        public void checkMatchName() {
            var guild = client.GetGuild(config.guildId);
            var users = guild.Users;

            //Sending a message to a specified channel in a specified server using the algorithm function
            NameMatcher.matchNames(config.userRoleMatches.SelectMany(x => x.userDetailInformation), users);

            // TODO: Add thorough checks to verify the algorithm is working as intended
        }

        [TestMethod]
        public void checkFullName() {
            var response = NameMatcher.matchName(null, nameSet, "John Doe");
            Assert.AreEqual(response.level, NameMatcher.MatchLevel.CompleteMatch);
        }

        [TestMethod]
        public void checkLastName() {
            var response = NameMatcher.matchName(null, nameSet, "John Doe");
            Assert.AreEqual(response.level, NameMatcher.MatchLevel.PartialMatch);
            Assert.IsTrue(response.lastNameMatch.Count == 1);
        }

        [TestMethod]
        public void checkNoSpace() {
            var response = NameMatcher.matchName(null, nameSet, "JohnDoe");
            Assert.AreEqual(response.level, NameMatcher.MatchLevel.PartialMatch);
            Assert.IsTrue(response.noSpacesMatch.Count == 1);
        }

        [TestMethod]
        public void checkNameAlgorithm() {
            var response = NameMatcher.matchName(null, nameSet, "Bill Pizza");
            Assert.AreEqual(response.level, NameMatcher.MatchLevel.NoMatch);
        }
    }
}
