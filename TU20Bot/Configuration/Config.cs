using System;
using System.Collections.Generic;

namespace TU20Bot.Configuration {
    public class UserJoinInfo {
        public ulong id;
        public DateTimeOffset time;
    }

    public class Config {
        public ulong guildId = 230737273350520834; // TU20
        
        public ulong welcomeChannelId = 736741911150198835; // #bot-testing
        
        public List<string> welcomeMessages = new List<string> {
            "Hello there!",
            "Whats poppin",
            "Wagwan",
            "Hi",
            "AHOY",
            "Welcome",
            "Greetings",
            "Howdy"
        };
        
        public List<UserJoinInfo> usersJoined = new List<UserJoinInfo>();
    }
}