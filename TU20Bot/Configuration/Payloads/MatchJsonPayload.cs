using System.Collections.Generic;

using Swan.Formatters;

namespace TU20Bot.Configuration.Payloads {
    internal class UserJsonPayload {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
    }
    
    internal class MatchJsonPayload {
        [JsonProperty("role")]
        public string role { get; set; }

        [JsonProperty("details")]
        public List<UserJsonPayload> details { get; set; }
    }
}
