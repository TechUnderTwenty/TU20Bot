using System;

using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    public class LogEntryPayload {
        [JsonProperty("type")]
        public string type { get; set; }
        
        [JsonProperty("id")]
        public string id { get; set; }
        
        [JsonProperty("username")]
        public string username { get; set; }
        [JsonProperty("discriminator")]
        public ushort discriminator { get; set; }
        
        [JsonProperty("nickname")]
        public string nickname { get; set; }
        
        [JsonProperty("joinDate")]
        public DateTime? joinDate { get; set; }
    }
}