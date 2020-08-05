using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    public class DiscordChannelPayload {
        [JsonProperty("id")]
        public string id { get; set; }
        
        [JsonProperty("name")]
        public string name { get; set; }
    }
}