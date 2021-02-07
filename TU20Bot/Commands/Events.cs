using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MongoDB.Bson;
using MongoDB.Driver;
using TU20Bot.Models;
using TU20Bot.Support;
using Tag = TU20Bot.Models.Tag;

namespace TU20Bot.Commands {
    public class Events : ModuleBase<SocketCommandContext> {
        private Client client => (Client) Context.Client;

        private EventBuilder existingBuilder =>
            client.config.eventBuilders.TryGetValue(Context.User.Id, out var builder)
                ? builder : null;
        
        [Command("cancel-event")]
        public async Task cancelEvent() {
            var builder = existingBuilder;
            
            if (builder == null) {
                await ReplyAsync("You haven't started an event. " +
                    "Start event with the `-create-event Event Name` command.");

                return;
            }

            client.config.eventBuilders.Remove(Context.User.Id);

            await ReplyAsync($"Event {builder.model.name} cancelled.");
        }

        [Command("create-event")]
        public async Task createEvent([Remainder] string name = "") {
            const int maxPendingEvents = 2;
            
            if (client.config.eventBuilders.ContainsKey(Context.User.Id)) {
                await ReplyAsync("Please finish your existing event before creating a new event. " +
                    "If you wish to cancel your current event, use the `-cancel-event` command.");

                return;
            }
            
            if (name.Length == 0) {
                await ReplyAsync(
                    "Run the command with the name of your event, like `-create-event My Event`.");
                
                return;
            }

            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);
            var result = await collection.CountDocumentsAsync(
                Builders<EventModel>.Filter.And(
                    Builders<EventModel>.Filter.Ne(x => x.approvalMessage, (ulong)0),
                    Builders<EventModel>.Filter.Eq(x => x.authorId, Context.Message.Author.Id)
                ));

            if (result >= maxPendingEvents) {
                await ReplyAsync($"You already have {maxPendingEvents} events pending review, " +
                    "please wait until they are reviewed before submitting another.");

                return;
            }
            
            var builder = new EventBuilder(name, client, Context.User, Context.Channel);
            client.config.eventBuilders[Context.User.Id] = builder;
            
            await builder.start();
        }

        Task printResults(List<EventModel> results, string query) {
            if (!results.Any()) {
                return ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle($"No results matching {query}.")
                    .Build());
            }
            
            return ReplyAsync("", false, new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{results.Count} results.")
                .WithDescription(string.Join("\n", results
                    .Select((x, y) => $"{y + 1}. [{x.name}]({x.submissionLink})")))
                .Build());
        }
        
        [Command("search-events")]
        public async Task searchEvents([Remainder] string name) {
            const int maxOptions = 6;
            
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);
            var resultCursor = await collection.FindAsync(
                Builders<EventModel>.Filter.Text(name.Trim(), new TextSearchOptions {
                    CaseSensitive = false,
                    DiacriticSensitive = false
                }),
                new FindOptions<EventModel> {
                    Limit = maxOptions,
                    Projection = Builders<EventModel>.Projection.MetaTextScore("textScore"),
                    Sort = Builders<EventModel>.Sort.MetaTextScore("textScore")
                });
            
            await printResults(await resultCursor.ToListAsync(), name);
        }

        [Command("list-events")]
        public async Task listEvents([Remainder] string name) {
            const int maxOptions = 6;

            var tags = name
                .Split(' ')
                .Select(x => Tag.allTags
                    .FirstOrDefault(y => string.Equals(
                        x.Trim(), y.commonName, StringComparison.CurrentCultureIgnoreCase))?.id
                )
                .Where(x => x != null)
                .ToList();

            if (!tags.Any()) {
                var allTags = string.Join(", ", Tag.allTags.Select(x => x.commonName));
                await ReplyAsync($"No tags matching {name}. Valid tags are {allTags}.");

                return;
            }
            
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);
            var resultCursor = await collection.FindAsync(
                Builders<EventModel>.Filter.AnyIn(x => x.tagIds, tags),
                new FindOptions<EventModel> {
                    Limit = maxOptions
                });

            await printResults(await resultCursor.ToListAsync(), name);
        }
    }
}