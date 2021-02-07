using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using MongoDB.Driver;

using TU20Bot.Models;
using Tag = TU20Bot.Models.Tag;

namespace TU20Bot.Support {
    public class EventBuilder {
        public readonly EventModel model = new EventModel();
        
        private readonly Client client;
        
        private readonly IUser user;
        private readonly IMessageChannel channel;

        private IUserMessage prompt;
        private IUserMessage product;

        // A bit of a state machine.
        private enum State {
            NotStarted,
            Description,
            Image,
            Resources,
            Tags,
            Confirm,
            
            Edit
        }

        private static readonly Dictionary<string, State> stateEmojis = new Dictionary<string, State> {
            { "📰", State.Description },
            { "🖼️", State.Image },
            { "📚", State.Resources },
            { "🛑", State.Tags },
        };

        private State? unfinishedState;
        private State state = State.NotStarted;

        private string productLink =>
            $"https://discord.com/channels/@me/{product.Channel.Id}/{product.Id}";
        
        private static State nextState(State state) =>
            state switch {
                State.NotStarted => State.Description,
                State.Description => State.Image,
                State.Image => State.Resources,
                State.Resources => State.Tags,
                State.Tags => State.Confirm,
                
                State.Edit => State.Confirm,
                
                _ => throw new Exception("Unhandled state.")
            };

        private Task updateProduct() => product.ModifyAsync(x => x.Embed = model.makeEmbed());

        private async Task moveState(State? next = null) {
            if (prompt != null)
                await prompt.DeleteAsync();

            await updateProduct();

            if (next.HasValue) {
                unfinishedState = state;
                state = next.Value;
            } else {
                state = unfinishedState ?? nextState(state);
                unfinishedState = null;
            }

            try {
                prompt = state switch {
                    State.Description => await channel.SendMessageAsync("", false,
                        new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithTitle("📰   Add Description")
                            .WithDescription(
                                "Please enter a brief description of your event in your next message.")
                            .Build()),
                    
                    State.Image => await channel.SendMessageAsync("", false,
                        new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithTitle("🖼️   Upload Image")
                            .WithDescription(
                                "Please send a link or upload an image to use for this event. " +
                                "React ➡ to skip this step.")
                            .Build()),
                    
                    State.Resources => await channel.SendMessageAsync("", false,
                        new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithTitle("📚   Add Resources")
                            .WithDescription(
                                "Please send any links to any related resources " +
                                "(websites, calendar, sign up sheets) separated by commas. " +
                                "React ➡ to skip this step.")
                            .Build()),
                    
                    State.Tags => await channel.SendMessageAsync("", false,
                        new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithTitle("🛑   Select Tags")
                            .WithDescription("Please react with any of the following tags. " +
                                 "When you're done, react with ✅.\n\n" +
                                 string.Join("\n\n", Tag.allTags.Select(x => $"{x.emoji}   {x.commonName}")))
                            .Build()),
                    
                    State.Confirm => await channel.SendMessageAsync("", false,
                        new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithTitle("❗   Confirm Event")
                            .WithDescription(
                                $"Look over [your event]({productLink}) before you submit. " +
                                "Please check for any inappropriate language, " +
                                "your event will be reviewed by TU20 before it will be posted.\n\n" +
                                "React with ✏ to edit your event or ✅ if you want to submit. ")
                            .WithFooter("To scrap the whole event, run `-cancel-event`.")
                            .Build()),

                    State.Edit => await channel.SendMessageAsync("", false,
                        new EmbedBuilder()
                            .WithColor(Color.Orange)
                            .WithTitle("Edit Event")
                            .WithDescription(
                                "React with any of the following event details you would like to change. " +
                                "React with ✅ when you're done.\n\n" +
                                string.Join("\n\n", stateEmojis.Select(x => $"{x.Key} {x.Value}")))
                            .Build()),

                    _ => throw new Exception("Unhandled case.")
                };
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            // Add reactions, these tend to take a while, so I want to do them separately.
            switch (state) {
                case State.Image:
                case State.Resources:
                    await prompt.AddReactionAsync(new Emoji("➡"));
                    break;

                case State.Tags:
                    await prompt.AddReactionsAsync(Tag.allTags
                        .Select(x => new Emoji(x.emoji) as IEmote)
                        .Append(new Emoji("✅"))
                        .ToArray());
                    break;
                
                
                case State.Confirm:
                    await prompt.AddReactionsAsync(new IEmote[] { new Emoji("✏"), new Emoji("✅") });
                    
                    break;
                
                case State.Edit:
                    await prompt.AddReactionsAsync(stateEmojis
                        .Select(x => new Emoji(x.Key) as IEmote)
                        .Append(new Emoji("✅"))
                        .ToArray());

                    break;
            }
        }

        private async Task<bool> handleDescription(IMessage message) {
            if (!message.Content.Trim().Any())
                return false;

            model.description = message.Content;
            
            await moveState();
            
            return true;
        }

        private async Task<bool> handleImage(IMessage message) {
            string[] imageExtensions = {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp"
            };

            if (message.Content.Trim().ToLower() != "none") {
                if (message.Content.Trim().Any()
                    && Uri.IsWellFormedUriString(message.Content, UriKind.Absolute)
                    && imageExtensions.Contains(Path.GetExtension(message.Content)?.ToLower())) {
                    model.image = message.Content;
                } else if (message.Attachments.Any()) {
                    // a url might automatically be promoted to an attachment, idk really
                    var first = message.Attachments.First();

                    if (imageExtensions.Contains(Path.GetExtension(first.Url)?.ToLower())) {
                        model.image = first.Url;
                    }
                }
            }

            await moveState();

            return true;
        }

        private async Task<bool> handleResources(IMessage message) {
            // Just want to make none explicit for the time being.
            if (message.Content.Trim().ToLower() != "none") {
                var newResources = message.Content
                    .Split(',', ';', '\n')
                    .Select(x => x.Trim())
                    .Where(x => x.Any() && Uri.IsWellFormedUriString(x, UriKind.Absolute));

                model.resources = newResources.ToList();
            }

            await moveState();
            
            return true;
        }

        public async Task addReaction(ulong messageId, SocketReaction reaction) {
            if (reaction.UserId == client.CurrentUser.Id)
                return;

            if (prompt.Id != messageId)
                return;

            switch (state) {
                case State.Image:
                case State.Resources:
                    if (reaction.Emote.Name == "➡")
                        await moveState();
                    break;

                case State.Tags: {
                    if (reaction.Emote.Name == "✅") {
                        await moveState();
                        break;
                    }
                    
                    var tag = Tag.allTags.FirstOrDefault(x => x.emoji == reaction.Emote.Name);
                    if (tag != null && !model.tagIds.Contains(tag.id)) {
                        model.tagIds.Add(tag.id);
                    }

                    await updateProduct();
                    
                    break;
                }

                case State.Confirm:
                    switch (reaction.Emote.Name) {
                        case "✅":
                            await submit();
                            break;
                            
                        case "✏":
                            await moveState(State.Edit);
                            break;
                    }
                    
                    break;
                
                case State.Edit:
                    if (reaction.Emote.Name == "✅")
                        await moveState();
                    
                    if (stateEmojis.TryGetValue(reaction.Emote.Name, out var next))
                        await moveState(next);
                    
                    break;
            }
        }

        public async Task removeReaction(ulong messageId, SocketReaction reaction) {
            if (reaction.UserId == client.CurrentUser.Id)
                return;

            if (prompt.Id != messageId)
                return;

            switch (state) {
                case State.Tags: {
                    var tag = Tag.allTags.FirstOrDefault(x => x.emoji == reaction.Emote.Name);
                    if (tag != null && model.tagIds.Contains(tag.id)) {
                        model.tagIds.Remove(tag.id);
                    }

                    await updateProduct();

                    break;
                }
            }
        }
        
        public async Task<bool> sendMessage(IMessage message) {
            if (message.Channel.Id != channel.Id)
                return false;

            return state switch {
                State.NotStarted => false,
                State.Description => await handleDescription(message),
                State.Image => await handleImage(message),
                State.Resources => await handleResources(message),
                State.Tags => false,
                State.Confirm => false,

                _ => throw new Exception("Unhandled event builder state.")
            };
        }

        private async Task submit() {
            await prompt.DeleteAsync();

            client.config.eventBuilders.Remove(user.Id);

            if (client.GetChannel(client.config.approvalChannelId) is IMessageChannel approvalChannel) {
                var message = await approvalChannel.SendMessageAsync("", false, model.makeEmbed());
                await message.AddReactionsAsync(new IEmote[] { new Emoji("✅"), new Emoji("❌") });

                model.approvalMessage = message.Id;

                var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);

                try {
                    await collection.Indexes.CreateOneAsync(new CreateIndexModel<EventModel>(
                        Builders<EventModel>.IndexKeys.Text(x => x.name)
                    ));
                } catch (Exception e) {
                    Console.WriteLine(e);
                }

                await collection.InsertOneAsync(model);

                await channel.SendMessageAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("🎉 🎊   Event Submitted")
                    .WithDescription($"Your event, **{model.name}**, has been submitted for review! " +
                                     "It might take about a day before your event is approved.\n" +
                                     $"Keep checking <#{client.config.submissionsChannelId}> for updates.")
                    .Build());
            } else {
                await channel.SendMessageAsync(
                    "Failed to submit event for approval, please contact a TU20 executive.");
            }
        }

        public async Task start() {
            product = await channel.SendMessageAsync("", false, model.makeEmbed());
            
            await moveState();
        }

        public static async Task approve(Client client, IUser user, ulong messageId) {
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);

            var value = await collection.FindOneAndUpdateAsync(
                Builders<EventModel>.Filter.Eq(x => x.approvalMessage, messageId),
                Builders<EventModel>.Update.Set(x => x.approvalMessage, (ulong) 0));

            if (value != null &&
                client.GetChannel(client.config.approvalChannelId) is IMessageChannel approveChannel &&
                client.GetChannel(client.config.submissionsChannelId) is IMessageChannel submissionsChannel) {
                await approveChannel.SendMessageAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle($"✅   Event {value.name} submitted by {value.authorUsername} was approved.")
                    .WithFooter($"Approved by {user.Username}#{user.Discriminator}.")
                    .Build());
                
                await approveChannel.DeleteMessageAsync(messageId);

                var submission = await submissionsChannel.SendMessageAsync("", false, value.makeEmbed());
                var submissionLink = $"https://discord.com/channels/@me/{submission.Channel.Id}/{submission.Id}";

                await collection.UpdateOneAsync(
                    Builders<EventModel>.Filter.Eq(x => x.id, value.id),
                    Builders<EventModel>.Update.Set(x => x.submissionLink, submissionLink));
            }
        }

        public static async Task reject(Client client, IUser user, ulong messageId) {
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);

            var value = await collection.FindOneAndDeleteAsync(
                Builders<EventModel>.Filter.Eq(x => x.approvalMessage, messageId));

            if (value != null &&
                client.GetChannel(client.config.approvalChannelId) is IMessageChannel approveChannel) {
                await approveChannel.SendMessageAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle($"❌   Event {value.name} submitted by {value.authorUsername} was rejected.")
                    .WithFooter($"Rejected by {user.Username}#{user.Discriminator}.")
                    .Build());
                
                await approveChannel.DeleteMessageAsync(messageId);
            }
        }

        public EventBuilder(string name, Client client, IUser user, IMessageChannel channel) {
            this.client = client;

            this.user = user;
            this.channel = channel;
            
            model.name = name;
            model.authorId = user.Id;
            model.authorUsername = $"{user.Username}#{user.Discriminator}";
            model.authorAvatar = user.GetAvatarUrl();
        }
    }
}