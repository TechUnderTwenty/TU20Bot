using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Configuration;
using TU20Bot.Configuration.Payloads;

namespace BotTest {
    [TestClass]
    public class NameMatchTest {
        private static NameMatch nameMatch;
        private static Config config;
        private static Client client;
        
        private readonly List<UserDetailsPayload> nameSet = new List<UserDetailsPayload>{
            new UserDetailsPayload {
                firstName = "John",
                lastName = "Doe",
                email = "johndoe@tu20.com"
            },
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
            client = new Client(config);

            config.matches.Add(new UserMatchPayload {
                role = 0,
                details = CSVReader.readFile()
            });

            // Logging in the bot
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            nameMatch = new NameMatch();
        }

        [TestMethod]
        public void checkMatchName() {
            // Waiting for the bot to log in and appear online
            var ready = false;
            client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready) { }

            var guild = client.GetGuild(config.guildId);
            var users = guild.Users;

            //Sending a message to a specified channel in a specified server using the algorithm function
            nameMatch.matchNames(config.matches.SelectMany(x => x.details), users);

            // TODO: Add thorough checks to verify the algorithm is working as intended
        }

        [TestMethod]
        public void checkFullName() {
            var response = NameMatch.matchName(null, nameSet, "John Doe");
            Assert.AreEqual(response.level, NameMatch.MatchLevel.CompleteMatch);
        }

        [TestMethod]
        public void checkLastName() {
            var response = NameMatch.matchName(null, nameSet, "John Doe");
            Assert.AreEqual(response.level, NameMatch.MatchLevel.CloseMatch);
            Assert.IsTrue(response.lastNameMatch.Count == 1);
        }

        [TestMethod]
        public void checkNoSpace() {
            var response = NameMatch.matchName(null, nameSet, "JohnDoe");
            Assert.AreEqual(response.level, NameMatch.MatchLevel.CloseMatch);
            Assert.IsTrue(response.noSpacesMatch.Count == 1);
        }

        [TestMethod]
        public void checkNameAlgorithm() {
            var response = NameMatch.matchName(null, nameSet, "Bill Pizza");
            Assert.AreEqual(response.level, NameMatch.MatchLevel.NoMatch);
        }
    }
}
