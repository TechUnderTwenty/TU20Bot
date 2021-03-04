using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TU20Bot.Models {
    public class UserModel {
        public const string collectionName = "email-pivot";
        
        [BsonId]
        public ObjectId id;
        
        public string discordId;
        public string email;
    }
}