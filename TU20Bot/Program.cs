using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using TU20Bot.Configuration;

namespace TU20Bot {
    internal class Program {
        private readonly string token;

        private Client client;
        private Config config;
        private Server server;
        private Handler handler;

        // Initializes Discord.Net
        private async Task start() {
            config = Config.load(Config.defaultPath) ?? new Config();

            client = new Client(config);
            server = new Server(client);
            handler = new Handler(client);

            await handler.init();

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            // Run server on another thread.
            new Thread(() => server.RunAsync().GetAwaiter().GetResult()).Start();

            await Task.Delay(-1);
        }

        private Program(string token) {
            this.token = token;
        }

        // Entry
        public static void Main(string[] args) {
            // Init command with token.
            if (args.Length >= 2 && args[0] == "init") {
                File.WriteAllText("token.txt", args[1]);
            }

            // Start bot with token from "token.txt" in working folder.
            try {
                var token = File.ReadAllText("token.txt").Trim();
                new Program(token).start().GetAwaiter().GetResult();
            } catch (IOException) {
                Console.WriteLine("Could not read from token.txt. Did you run `init <token>`?");
            }


        }
    }
}
