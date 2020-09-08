using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Configuration;

namespace BotTest {
    [TestClass]
    public class NameMatchTest {

        private static NameMatch _nameMatch;
        private static Config _config;
        private static Client _client;
        private static Handler _handler;

        private const bool OUTPUT_TO_DISCORD = false;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext testContext) {

            string token = "";

            // Start bot with token from "token.txt" in working folder.
            try {
                var _token = File.ReadAllText("token.txt").Trim();
                token = _token.ToString();
            } catch (IOException) {
                // current directory: BotTest/bin/Debug/netcoreapp3.1
                Console.WriteLine("Could not read from token.txt." +
                    " Did you put token.txt file in the current directory?");
                Assert.Fail();
            }

            // Initializing all the requored classes
            _config = new Config();
            _client = new Client(_config);
            _handler = new Handler(_client);

            await _handler.init();

            // Logging in the bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _nameMatch = new NameMatch();
        }

        [TestMethod]
        public async Task CheckMatchName() {

            // Waiting for the bot to log in and appear online
            bool ready = false;
            _client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready) ;

            var guild = _client.GetGuild(_config.guildId);
            var users = guild.Users;

            //Sending a message to a specified channel in a specified server using the algorithm function
            var stringResponse = await _nameMatch.nameMatching(_config.origNames, users, null);

            // TODO: Add thorough checks to verify the algorithm is working as intended

            if (OUTPUT_TO_DISCORD)
                await guild.GetTextChannel(_config.welcomeChannelId).SendMessageAsync(stringResponse);
        }

        [TestMethod]
        public void TestMatchNameAlgorithm_FullMatch() {
            string[,] nameSet = { { "John", "Doe" } };
            var response = NameMatch.nameMatchAlg("John Doe", nameSet);
            Assert.AreEqual(response.level, NameMatch.MatchLevel.CompleteMatch);
        }

        [TestMethod]
        public void TestMatchNameAlgorithm_LastNameMatch() {
            string[,] nameSet = { { "Bill", "Doe" } };
            var response = NameMatch.nameMatchAlg("John Doe", nameSet);
            Assert.AreEqual(response.level, NameMatch.MatchLevel.CloseMatch);
            Assert.IsTrue(response.lastNameMatch.Count == 1);
        }

        [TestMethod]
        public void TestMatchNameAlgorithm_NoSpaceMatch() {
            string[,] nameSet = { { "John", "Doe" } };
            var response = NameMatch.nameMatchAlg("JohnDoe", nameSet);
            Assert.AreEqual(response.level, NameMatch.MatchLevel.CloseMatch);
            Assert.IsTrue(response.noSpacesMatch.Count == 1);
        }

        [TestMethod]
        public void TestMatchNameAlgorithm_NoMatch() {
            string[,] nameSet = { { "Bill", "Johnson" } };
            var response = NameMatch.nameMatchAlg("John Doe", nameSet);
            Assert.AreEqual(response.level, NameMatch.MatchLevel.NoMatch);
        }

    }


}
