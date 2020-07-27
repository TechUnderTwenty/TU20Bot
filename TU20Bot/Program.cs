using System;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace TU20Bot {
    class Program {
        private readonly string token;

        private DiscordSocketClient client;
        private Handler handler;

        // Initializes Discord.Net
        private async Task start() {
            client = new DiscordSocketClient();
            handler = new Handler(client);

            await handler.init();
            
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

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
                var token = File.ReadAllText("token.txt");
                new Program(token).start().GetAwaiter().GetResult();
            } catch (IOException) {
                Console.WriteLine("Could not read from token.txt. Did you run `init <token>`?");
            }
        }
    }
}
