using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

using MongoDB.Driver;
using TU20Bot.Models;

using Tag = TU20Bot.Models.Tag;

namespace TU20Bot.Commands {
    public class Events : ModuleBase<SocketCommandContext> {
        private Client client => (Client) Context.Client;

        private Task printResults(IReadOnlyCollection<EventModel> results, string query) {
            if (!results.Any()) {
                // If no events are found, feel free to inform the user.
                return ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle($"No results matching {query}.")
                    .Build());
            }

            const int maxEntries = 6;
            const int maxLengthPerEntry = 40;

            // This method takes the first `maxLengthPerEntry` characters of EventModel's messageContent.
            // This is to make sure that we don't go over the message limit.
            static string summary(EventModel model) =>
                model.messageContent.Substring(
                    0, Math.Min(maxLengthPerEntry, model.messageContent.Length));
            
            // Print a summary of the first `maxEntries` results.
            return ReplyAsync("", false, new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{results.Count} results.")
                .WithDescription(string.Join("\n", results
                    .Take(maxEntries)
                    .Select((x, y) => $"{y + 1}. [{summary(x)}]({x.messageLink})")))
                .Build());
        }
        
        [Command("search-events")]
        public async Task searchEvents([Remainder] string name) {
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);
            
            // Some options for an unrestricted MongoDB search.
            // Their search isn't really fuzzy but it's kinda okay.
            var searchOptions =  new TextSearchOptions {
                CaseSensitive = false,
                DiacriticSensitive = false
            };
            var findOptions = new FindOptions<EventModel> {
                Projection = Builders<EventModel>.Projection.MetaTextScore("textScore"),
                Sort = Builders<EventModel>.Sort.MetaTextScore("textScore")
            };
            
            // Perform the text search. Requires a text index.
            var resultCursor = await collection.FindAsync(
                Builders<EventModel>.Filter.Text(name.Trim(), searchOptions), findOptions);
            
            // Print the results.
            await printResults(await resultCursor.ToListAsync(), name);
        }

        [Command("list-events")]
        public async Task listEvents([Remainder] string name) {
            // Split the command parameters, and try to turn them into tags by common name.
            // Any spaces between common names would screw things up.
            var tags = name
                .Split(' ')
                .Select(x => Tag.allTags
                    .FirstOrDefault(y => string.Equals(
                        x.Trim(), y.commonName, StringComparison.CurrentCultureIgnoreCase))?.id
                )
                .Where(x => x != null)
                .ToList();

            if (!tags.Any()) {
                // If there are no tags, we should probably tell the user something special went wrong.
                var allTags = string.Join(", ", Tag.allTags.Select(x => x.commonName));
                await ReplyAsync($"No tags matching {name}. Valid tags are {allTags}.");

                return;
            }
            
            // Search for any events that contains any of the tags.
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);
            var resultCursor = await collection.FindAsync(
                Builders<EventModel>.Filter.AnyIn(x => x.tagIds, tags));

            // Print the results.
            await printResults(await resultCursor.ToListAsync(), name);
        }

        [Command("list-events")]
        public async Task listEvents() {
            var collection = client.database.GetCollection<EventModel>(EventModel.collectionName);

            // Get the last 5 events in the database.
            var resultCursor = await collection.FindAsync(
                Builders<EventModel>.Filter.Empty, new FindOptions<EventModel> {
                    Sort = Builders<EventModel>.Sort.Descending(e => e.id),
                    Limit = 5
                });

            // Print the results.
            await printResults(await resultCursor.ToListAsync(), "No events available");
        }
    }
}