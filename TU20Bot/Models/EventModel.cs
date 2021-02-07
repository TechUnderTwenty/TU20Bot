using System.Linq;
using System.Collections.Generic;

using Discord;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TU20Bot.Models {
    class Tag {
        // C# 9's { get; init; } would be so nice here...
        public string id;
        
        // I want to keep common name separate from ID so we have space to put nice things.
        public string commonName;
        public string emoji;

        public static readonly Tag[] allTags = {
            new Tag {
                id = "hangout",
                commonName = "Hangout",
                emoji = "😃"
            },
            new Tag {
                id = "technology",
                commonName = "Technology",
                emoji = "🔧"
            },
            new Tag {
                id = "business",
                commonName = "Business",
                emoji = "🕴"
            },
            new Tag {
                id = "hackathon",
                commonName = "Hackathon",
                emoji = "💻"
            }
        };
    }
    
    public class EventModel {
        [BsonId]
        public ObjectId id;
        
        public const string collectionName = "events";

        public string name;
        public string description;
        public string image;
        public List<string> resources = new List<string>();

        public ulong approvalMessage;
        public string submissionLink;

        public List<string> tagIds = new List<string>();

        public ulong authorId;
        public string authorAvatar;
        public string authorUsername;

        public double? textScore;

        public Embed makeEmbed() {
            var builder = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle(name)
                .WithFooter(authorUsername, authorAvatar);
            
            if (description != null)
                builder.Description = description;
            if (image != null)
                builder.ThumbnailUrl = image;
            if (resources.FirstOrDefault() != null)
                builder.Url = resources.First();
            if (tagIds.Any()) {
                builder.Fields.Add(new EmbedFieldBuilder()
                    .WithName("Tags")
                    .WithValue(string.Join(", ", tagIds
                        .Select(x => Tag.allTags.FirstOrDefault(y => y.id == x))
                        .Where(x => x != null)
                        .Select(x => $"{x.commonName}")))
                    .WithIsInline(false));
            }

            if (resources.Any()) {
                builder.Fields.Add(new EmbedFieldBuilder()
                    .WithName("Resources")
                    .WithValue(string.Join("\n", resources.Select((x, i) => $"[{i + 1}] {x}")))
                    .WithIsInline(false));
            }

            return builder.Build();
        }
    }
}