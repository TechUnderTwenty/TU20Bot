using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    public class DiscordServerPayload {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("icon")]
        public string icon { get; set; }
    }
}