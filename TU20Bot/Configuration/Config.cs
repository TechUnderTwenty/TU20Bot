using System;
using System.Collections.Generic;

namespace TU20Bot.Configuration {
    public enum LogEvent {
        UserJoin,
        UserLeave,
    }
    
    public class LogEntry {
        public LogEvent logEvent;
        public ulong id;
        public string name;
        public ushort discriminator;
        public DateTime time;
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

        public readonly string[,] origNames = {
            {"Dev","Narula"},
            {"Timur","Khayrullin"},
            {"Zaynah","nolastname"},
            {"Yifan","Wang"},
            {"Tahmeed","Naser"},
            {"Shrena","Sribalan"},
            {"Rick","(O20)"},
            {"Pranav","Vyas"},
            {"Karen","Truong"},
            {"Muhammad Muizz","nolastname"},
            {"Vince","Li"},
            {"Alex","Li"},
            {"nofirstname","Nolan"},
            {"Borna","Sadeghi"},
            {"Hargun","nolastname"},
            {"Markos","Georghiades"},
            {"Muhammad Muizz","Zafar"},
            {"Taylor desgroup","Whatley"},
            {"Viktor fasest","Korolyuk"},
            {"Denys","Linkov"},
            {"Muhammad Ali","Syed"},
        };

        public List<LogEntry> logs = new List<LogEntry>();
    }
}