using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using TU20Bot.Configuration.Payloads;

namespace TU20Bot.Configuration {
    public class EmailChecker {
        public class EmailMatchResult {
            public readonly ulong id;
            public readonly UserMatch match;
            public readonly UserDetails detail;
        }
        
        private readonly Config config;
        private readonly Client client;

        public EmailChecker(Config config, Client client) {
            this.config = config;
            this.client = client;
        }
    }
}
