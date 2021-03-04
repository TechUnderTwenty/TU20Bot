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

        // Some examples, add up to 20 total tags before bad things happen.
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
    
    public enum EventState {
        Draft,
        Confirmed,
    }

    public class EventModel {
        [BsonId]
        public ObjectId id;
        
        public const string collectionName = "events";

        public ulong authorId;
        
        public ulong messageId;
        public string messageLink;
        public string messageContent;

        public ulong? promptId;
        
        public List<string> tagIds = new List<string>();

        // Used by MongoDB to sort.
        public double? textScore;

        public EventState state;
    }
}