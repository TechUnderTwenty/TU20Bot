using System.Collections.Generic;
using System.Linq;
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

        public UserMatch toUserMatch() {
            return new UserMatch {
                role = ulong.Parse(role),
                details = details.Select(x => new UserDetails {
                    email = x.email,
                    firstName = x.firstName,
                    lastName = x.lastName
                }).ToList()
            };
        }
    }
}
