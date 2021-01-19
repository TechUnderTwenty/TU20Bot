using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    internal class FactoryJsonPayload {
        [JsonProperty("name")]
        public string name { get; set; }
        
        [JsonProperty("maxChannels")]
        public int maxChannels { get; set; }
    }
}