using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    internal class IdJsonPayload {
        [JsonProperty("id")]
        public string id { get; set; }
    }
}