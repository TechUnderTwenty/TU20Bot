using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    public class FactoryJsonPayload {
        [JsonProperty("name")]
        public string name { get; set; }
        
        [JsonProperty("maxChannels")]
        public int maxChannels { get; set; }
    }
}