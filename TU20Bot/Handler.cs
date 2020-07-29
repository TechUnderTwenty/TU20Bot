using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace TU20Bot {
    public class Handler {
        // Config
        private const char prefix = '-';
        private const bool showStackTrace = true;

        private readonly DiscordSocketClient client;
        
        private readonly CommandService commands;
        private readonly ServiceProvider services;

        private string[] greetings = {
            "Hello there!",
            "Whats poppin",
            "Wagwan",
            ...
            "Wagwan", "Hi", "AHOY", "Welcome", "Greetings", "Howdy"};

        private Random rndInt = new Random();

        // Called by Discord.Net when it wants to log something.
        private static Task log(LogMessage message) {
            Console.WriteLine(message.Message);
            return Task.CompletedTask;
        }
        
        // Called by Discord.Net when the bot receives a message.
        private async Task checkMessage(SocketMessage message) {
            if (!(message is SocketUserMessage userMessage)) return;

            var prefixStart = 0;

            if (userMessage.HasCharPrefix(prefix, ref prefixStart)) {
                // Create Context and Execute Commands
                var context = new SocketCommandContext(client, userMessage);
                var result = await commands.ExecuteAsync(context, prefixStart, services);
                
                // Handle any errors.
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand) {
                    if (showStackTrace && result.Error == CommandError.Exception 
                            && result is ExecuteResult execution) {
                        await userMessage.Channel.SendMessageAsync(
                            $"```\n{execution.Exception.Message}\n\n{execution.Exception.StackTrace}\n```");
                    } else {
                        await userMessage.Channel.SendMessageAsync(
                            "Halt We've hit an error.\n```\n{result.ErrorReason}\n```");
                    }
                }
            }
        }

        // Initializes the Message Handler, subscribe to events, etc.
        public async Task init() {
            client.Log += log;
            client.UserJoined += userJoined;
            client.MessageReceived += checkMessage;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        public async Task userJoined(SocketGuildUser user) {
            var channel = (IMessageChannel) client.GetChannel(737081385583378447);

            await channel.SendMessageAsync(msgArr[rndInt.Next(0, msgArr.Length)] + $" <@{user.Id}>");
        }
        
        public Handler(DiscordSocketClient client) {
            this.client = client;
            
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
        }
    }
}
