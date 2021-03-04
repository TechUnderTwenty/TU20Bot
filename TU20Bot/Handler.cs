using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using MongoDB.Driver;

using TU20Bot.Models;
using TU20Bot.Support;
using TU20Bot.Configuration;
using Tag = TU20Bot.Models.Tag;

namespace TU20Bot {
    public class Handler {
        // Config
        private const char prefix = '-';
        
        private Client client;
        
        private CommandService commands;
        private ServiceProvider services;

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

        private async Task sendErrorMessage(string problem, IMessage userMessage = null) {
            if (userMessage != null) {
                try {
                    await userMessage.AddReactionAsync(new Emoji("❌"));
                } catch (Exception) { /* Oh well, what can you do. */ }
            }
            
            if (client.GetChannel(client.config.errorChannelId) is IMessageChannel errorChannel) {
                try {
                    await errorChannel.SendMessageAsync(problem);
                    return;
                } catch (Exception) { /* Go back! */ }
            }
            
            await Console.Error.WriteLineAsync(problem);
        }
        
        // Called by Discord.Net when the bot receives a message.
        private async Task messageReceived(SocketMessage message) {
            if (!(message is SocketUserMessage userMessage)) return;

            var prefixStart = 0;

            if (userMessage.HasCharPrefix(prefix, ref prefixStart)) {
                // Create Context and Execute Commands
                var context = new SocketCommandContext(client, userMessage);
                var result = await commands.ExecuteAsync(context, prefixStart, services);
                
                // Handle any errors. Returns are there to skip event builder entries.
                if (!result.IsSuccess) {
                    if (result.Error != CommandError.UnknownCommand) {
                        if (result.Error == CommandError.Exception
                            && result is ExecuteResult execution) {
                            await sendErrorMessage(
                                $"```\n{execution.Exception.Message}\n\n{execution.Exception.StackTrace}\n```",
                                userMessage);
                        } else {
                            await sendErrorMessage(
                                $"Halt We've hit an error.\n```\n{result.ErrorReason}\n```",
                                userMessage);
                        }

                        return;
                    }
                } else {
                    return;
                }
            }

            // If execution reaches here, the text should not have matched any command.
            if (!userMessage.Author.IsBot && message.Channel.Id == client.config.eventsChannelId) {
                var eventCollection = client.database.GetCollection<EventModel>(EventModel.collectionName);
                
                // First, check if the user already has an event. Find Limit = 1 is more efficient but :/
                var count = await eventCollection.CountDocumentsAsync(
                    Builders<EventModel>.Filter.Ne(x => x.promptId, null));

                // If he hasn't dismissed his previous event, we won't let him create another.
                if (count != 0)
                    return;

                // Send a prompt for them to tag their event.
                // Maybe should be in DMs? Would be kinda intrusive.
                var prompt = await message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("Add Tags")
                    .WithDescription(
                        $"Hey <@{message.Author.Id}>.\n\n" +
                        "To finish submitting your event, " +
                        "please react to your original message with one of the following tags. " +
                        "React with ✅ to this prompt to dismiss it.\n\n" +
                        string.Join("\n\n", Tag.allTags.Select(x => $"{x.emoji}   {x.commonName}")))
                    .Build());

                // Generates a link to a discord message. There's a case for DM messages, but its unnecessary.
                static string link(IMessage m) =>
                    m.Channel is IGuildChannel c
                        ? $"https://discord.com/channels/{c.Guild.Id}/{c.Id}/{m.Id}"
                        : $"https://discord.com/channels/@me/{m.Channel}/{m.Id}";

                // Add the event to the database with relevant details.
                await eventCollection.InsertOneAsync(new EventModel {
                    authorId = message.Author.Id,
                    
                    messageId = message.Id,
                    messageLink = link(message),
                    messageContent = message.Content,
                    
                    promptId = prompt.Id
                });
                
                // We also want to create an index for the collection so we can do text searching later.
                await eventCollection.Indexes.CreateOneAsync(new CreateIndexModel<EventModel>(
                    Builders<EventModel>.IndexKeys.Text(x => x.messageContent)
                ));

                // Add the example reactions, including tag emojis and ✅.
                await ((SocketUserMessage)message).AddReactionsAsync(
                    Tag.allTags.Select(x => new Emoji(x.emoji) as IEmote).ToArray());

                await prompt.AddReactionAsync(new Emoji("✅"));
                
                // When a user reacts, work is picked up in Handler's reactionAdded and reactionRemoved.
            }
        }

        private async Task messageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel) {
            // Check if the deleted message is relevant to the events submission channel.
            if (channel.Id == client.config.eventsChannelId) {
                var eventCollection = client.database.GetCollection<EventModel>(EventModel.collectionName);
                
                // Let's delete any record of the event if the original message was deleted.
                var model = await eventCollection.FindOneAndDeleteAsync(
                    Builders<EventModel>.Filter.Eq(x => x.messageId, message.Id));

                // Delete the prompt too, if it exists.
                if (model?.promptId != null) {
                    await channel.DeleteMessageAsync(model.promptId.Value);
                } else {
                    // Otherwise, try to see if a prompt for a message was deleted.
                    var promptModel = await eventCollection.FindOneAndUpdateAsync(
                        Builders<EventModel>.Filter.Eq(x => x.promptId, message.Id),
                        Builders<EventModel>.Update.Set(x => x.promptId, null)
                    );

                    // Then lets remove the bot reactions to the original message if possible.
                    if (promptModel != null
                        && await channel.GetMessageAsync(promptModel.messageId) is IUserMessage original) {
                        // Some trickery to convert tag ids to emojis and vice-versa.
                        var allTags = Tag.allTags.Select(x => new Emoji(x.emoji) as IEmote).ToArray();

                        await original.RemoveReactionsAsync(client.CurrentUser, allTags);
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
            // Log
            client.config.logs.Add(new LogEntry {
                logEvent = LogEvent.UserJoin,
                id = user.Id,
                name = user.Username,
                discriminator = user.DiscriminatorValue,
                time = DateTime.UtcNow
            });
            
            // Greetings
            var channel = (IMessageChannel)client.GetChannel(client.config.welcomeChannelId);

            var greetings = client.config.welcomeMessages;

            // Send welcome message.
            await channel.SendMessageAsync(
                greetings[random.Next(0, greetings.Count)] + $" <@{user.Id}>");

            string email = null;

            // Check database for users that have registered their email previously.
            if (client.database != null) {
                var collection = client.database.GetCollection<UserModel>(UserModel.collectionName);

                var emailModel = await collection.Find(
                    Builders<UserModel>.Filter.Eq(x => x.discordId, user.Id.ToString())
                ).Limit(1).FirstOrDefaultAsync();

                if (emailModel != null)
                    email = emailModel.email;
            }

            // Try to match the new user by their name to any existing roleMatch lists
            var nameMatches = client.config.userRoleMatches
                .Where(x => NameMatcher.matchName(user, x.userDetailInformation).level != NameMatcher.MatchLevel.NoMatch) // drop
                .Select(x => x.role); // grab roles

            // If the email has been found, find all role matches to which the email belongs to
            var emailMatches = email == null ? new List<ulong>() : client.config.userRoleMatches
                .Where(x => x.userDetailInformation.Any(u => u.email == email)) // check for email
                .Select(x => x.role); // grab roles

            var guild = user.Guild;

            var roles = nameMatches
                .Concat(emailMatches)
                .Distinct()
                .Select(x => guild.GetRole(x))
                .ToList();

            // Add the roles.
            if (roles.Any())
                await user.AddRolesAsync(roles);
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
                    x => x.Channel = new Discord.Optional<IVoiceChannel>(moveTo));
            }
        }

        private enum ModifyRoleOp {
            Add,
            Remove
        }

        private async Task modifyRole(ulong messageId,
            ISocketMessageChannel channel, SocketReaction reaction, ModifyRoleOp op) {
            var reactor = client.config.reactors.FirstOrDefault(x => x.id == messageId);
            
            // Must be in a guild to assign roles, must has a user attributed to reaction, must have matched a reactor
            if (!reaction.User.IsSpecified
                || !(reaction.User.Value is IGuildUser)
                || !(channel is IGuildChannel)
                || reactor == null)
                return;

            // Search to see if the reactor has a role defined for this emoticon
            var name = reactor.pairs.FirstOrDefault(x => x.emote == reaction.Emote.Name);
            if (name == null)
                return;

            var role = ((IGuildChannel)channel).Guild.GetRole(name.roleId);

            switch (op) {
                case ModifyRoleOp.Add:
                    await ((IGuildUser)reaction.User.Value).AddRoleAsync(role);
                    break;
                case ModifyRoleOp.Remove:
                    await ((IGuildUser)reaction.User.Value).RemoveRoleAsync(role);
                    break;
                default:
                    return;
            }
        }

        private async Task reactionAdded(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel, SocketReaction reaction) {
            await modifyRole(message.Id, channel, reaction, ModifyRoleOp.Add);

            // Check if this may be a reaction to an event prompt.
            if (channel.Id == client.config.eventsChannelId
                && reaction.UserId != client.CurrentUser.Id) {
                // Find a tag that matches the emoji being reacted.
                var tag = Tag.allTags.FirstOrDefault(x => x.emoji == reaction.Emote.Name);

                var eventCollection = client.database.GetCollection<EventModel>(EventModel.collectionName);

                if (tag != null) {
                    // If a specific tag was reacted, we'd like to add it to the model in the DB.
                    await eventCollection.FindOneAndUpdateAsync(
                        // Matches all events where the poster is the reactor...
                        // ... and the reaction has been made to the **original message** message.
                        Builders<EventModel>.Filter.And(
                            Builders<EventModel>.Filter.Eq(x => x.messageId, message.Id),
                            Builders<EventModel>.Filter.Eq(x => x.authorId, reaction.UserId)
                        ),
                        Builders<EventModel>.Update.AddToSet(x => x.tagIds, tag.id)
                    );
                } else if (reaction.Emote.Name == "✅") {
                    // Otherwise, if the ✅ emoji was reacted, lets do some closing work.

                    // Remove the prompt from the DB.
                    var model = await (await eventCollection.FindAsync(
                        // Matches all events where the poster is the reactor...
                        // ... and the reaction has been made to the **prompt** message.
                        Builders<EventModel>.Filter.And(
                            Builders<EventModel>.Filter.Eq(x => x.promptId, message.Id),
                            Builders<EventModel>.Filter.Eq(x => x.authorId, reaction.UserId)
                        ),
                        new FindOptions<EventModel> { Limit = 1 }
                    )).FirstOrDefaultAsync();
                    
                    if (model != null) {
                        // Remove the prompt from real life.
                        await channel.DeleteMessageAsync(message.Id);
                        
                        // Actually, messageDeleted handler will take it from here.
                    }
                }
            }
        }
        
        private async Task reactionRemove(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel, SocketReaction reaction) {
            await modifyRole(message.Id, channel, reaction, ModifyRoleOp.Remove);
            
            // Check if this action might be related to an event prompt.
            if (channel.Id == client.config.eventsChannelId) {
                // Find a tag that matches the emoji being dropped.
                var tag = Tag.allTags.FirstOrDefault(x => x.emoji == reaction.Emote.Name);

                var eventCollection = client.database.GetCollection<EventModel>(EventModel.collectionName);
                
                if (tag != null) {
                    // If a tag was found, lets drop all instances of this tag from the DB model.
                    await eventCollection.FindOneAndUpdateAsync(
                        // Matches all events where the poster is the reactor...
                        // ... and the reaction has been made to the **original message** message.
                        Builders<EventModel>.Filter.And(
                            Builders<EventModel>.Filter.Eq(x => x.messageId, message.Id),
                            Builders<EventModel>.Filter.Eq(x => x.authorId, reaction.UserId)
                        ),
                        Builders<EventModel>.Update.Pull(x => x.tagIds, tag.id)
                    );
                }
            }
        }

        // Initializes the Message Handler, subscribe to events, etc.
        public async Task init(Client discordClient) {
            client = discordClient;
            
            client.Log += log;
            client.UserLeft += userLeft;
            client.UserJoined += userJoined;
            client.MessageReceived += messageReceived;
            client.MessageDeleted += messageDeleted;
            client.UserVoiceStateUpdated += voiceUpdated;
            client.ReactionAdded += reactionAdded;
            client.ReactionRemoved += reactionRemove;
            
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }
    }
}
