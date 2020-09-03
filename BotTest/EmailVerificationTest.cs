using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TU20Bot;
using TU20Bot.Commands;
using TU20Bot.Configuration;

namespace BotTest {
    [TestClass]
    class EmailVerificationTest {

        private static EmailVerification _emailVerification;
        private static EmailChecker _emailChecker;
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
            _emailVerification = new EmailVerification();
            _emailChecker = new EmailChecker();

            await _handler.init();

            // Logging in the bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Run email checker on another thread
            new Thread(() => {
                _emailChecker.checkForEmail();
                while (true)
                    ;
            }).Start();

            // Waiting for the bot to log in and appear online
            bool ready = false;
            _client.Ready += () => {
                ready = true;
                return Task.CompletedTask;
            };
            while (!ready);     
        }

        [TestMethod]
        public void EmailVerifyTest() {

            _emailVerification.emailCompare("hfoihjeoijf");
        }
    }
}
