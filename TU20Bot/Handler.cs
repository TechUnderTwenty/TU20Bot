using System;
using System.Linq;
using System.Timers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using TU20Bot.Configuration;

namespace TU20Bot {
    public class Handler {
        // Config
        private const char prefix = '-';
        private const bool showStackTrace = true;

        private readonly Client client;
        
        private readonly CommandService commands;
        private readonly ServiceProvider services;

        private readonly Random random = new Random();

        // Checks the state of a factory; if it's currently unused, all the voice channels are removed
        // Triggered by a factory timer
        private async Task checkFactory(FactoryDescription factory) {
            var inUse = false;

            foreach (var channelId in factory.channels) {
                var channel = (IVoiceChannel)client.GetChannel(channelId);
                if (channel == null)
                    continue;
                
                var users = channel.GetUsersAsync();
                if (!await users.AnyAsync(user => user.Count != 0))
                    continue;
                
                inUse = true;
                break;
            }

            // If all channels are currently not in use, remove all of them
            if (!inUse) {
                factory.timer.Stop();
                factory.timer = null;
                
                foreach (var voiceChannel in factory.channels
                    .Select(channel => client.GetChannel(channel))
                    .OfType<SocketVoiceChannel>()) {
                    await voiceChannel.DeleteAsync();
                }

                factory.channels.Clear();
            }
        }

        // Called by Discord.Net when it wants to log something.
        private static Task log(LogMessage message) {
            Console.WriteLine(message.Message);
            return Task.CompletedTask;
        }
        
        // Called by Discord.Net when the bot receives a message.
        private async Task messageReceived(SocketMessage message) {
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

        private Task userLeft(SocketGuildUser user) {
            client.config.logs.Add(new LogEntry {
                logEvent = LogEvent.UserLeave,
                id = user.Id,
                name = user.Username,
                discriminator = user.DiscriminatorValue,
                time = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }
        
        // Called when a user joins the server.
        private async Task userJoined(SocketGuildUser user) {
            client.config.logs.Add(new LogEntry {
                logEvent = LogEvent.UserJoin,
                id = user.Id,
                name = user.Username,
                discriminator = user.DiscriminatorValue,
                time = DateTime.UtcNow
            });
            
            var channel = (IMessageChannel)client.GetChannel(client.config.welcomeChannelId);

            var greetings = client.config.welcomeMessages;

            // Send welcome message.
            await channel.SendMessageAsync(
                greetings[random.Next(0, greetings.Count)] + $" <@{user.Id}>");
        }

        private async Task voiceUpdated(
            SocketUser user, SocketVoiceState before, SocketVoiceState after) {
            var factory = client.config.factories.FirstOrDefault(x => x.id == after.VoiceChannel?.Id);

            if (factory == null)
                return;
            
            var guild = client.GetGuild(after.VoiceChannel.Guild.Id);
            
            IVoiceChannel moveTo = null;
            
            if (factory.channels.Count < factory.maxChannels) {
                const double timeoutTime = 1000 * 60;
                
                // Create a voice channel in the format of: "name count"
                var channel = await guild.CreateVoiceChannelAsync(
                    $"{factory.name} {factory.channels.Count + 1}");
                factory.channels.Add(channel.Id);

                // If no timer exists, create one.
                // For an existing factory the timer will be set to null when all voice channels are no longer in use
                if (factory.timer == null) {
                    factory.timer = new System.Timers.Timer(timeoutTime);
                    factory.timer.Elapsed += (sender, args) => {
                        checkFactory(factory).RunSynchronously();
                    };
                    factory.timer.AutoReset = true;
                    factory.timer.Start();
                }

                await channel.ModifyAsync(x => x.CategoryId = after.VoiceChannel.CategoryId);

                moveTo = channel;
            } else if (factory.channels.Count != 0) {
                moveTo = guild.GetChannel(
                    factory.channels[random.Next(factory.channels.Count)]) as IVoiceChannel;
            }

            if (moveTo != null) {
                await ((SocketGuildUser)user).ModifyAsync(
                    x => x.Channel = new Optional<IVoiceChannel>(moveTo));
            }
        }

        // Initializes the Message Handler, subscribe to events, etc.
        public async Task init() {
            client.Log += log;
            client.UserLeft += userLeft;
            client.UserJoined += userJoined;
            client.MessageReceived += messageReceived;
            client.UserVoiceStateUpdated += voiceUpdated;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        public Handler(Client client) {
            this.client = client;
            
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
        }
    }
}
