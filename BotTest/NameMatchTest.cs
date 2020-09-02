using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
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

            // Waiting for the bot to log in and appear online
            bool ready = false;
            _client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready);

            _nameMatch = new NameMatch();
        }

        [TestMethod]
        public async Task CheckMatchName() {

            var guild = _client.GetGuild(_config.guildId);
            var users = guild.Users;

            // Sending a message to a specified channel in a specified server using the algorithm function
            await guild.GetTextChannel(_config.welcomeChannelId)
                .SendMessageAsync(await _nameMatch.nameMatching(_config.origNames, users, null));
        }
    }
}