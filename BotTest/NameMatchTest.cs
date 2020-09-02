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
                // Directory: BotTest/bin/Debug/netcoreapp3.1
                Console.WriteLine("Could not read from token.txt." +
                    " Did you put token.txt file in the current working?");
                Assert.Fail();
            }
            _config = new Config();
            _client = new Client(_config);
            _handler = new Handler(_client);

            await _handler.init();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            bool ready = false;
            _client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready);

            _nameMatch = new NameMatch();
        }

        [TestMethod]
        public async Task TestMethod1() {

            var guild = _client.GetGuild(_config.guildId);
            var users = guild.Users;
            await guild.GetTextChannel(_config.welcomeChannelId)
                .SendMessageAsync(await _nameMatch.nameMatching(_config.origNames, users, null));
        }
    }
}