using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Xml.Serialization;
using System.Collections.Generic;

using EmbedIO.Utilities;

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

    public class FactoryDescription {
        public ulong id;
        public string name;
        public int maxChannels;
        public readonly List<ulong> channels = new List<ulong>();
        
        [XmlIgnore]
        public Timer timer;
    }
    
    public class UserDetails {
        public string firstName;
        public string lastName;
        public string email;

        public string fullName => firstName + " " + lastName;
    }

    public class UserMatch {
        public List<UserDetails> userDetailInformation = new List<UserDetails>();
        public ulong role;
    }

    public class Config {
        private const string tokenVariableName = "tu20_bot_token";
        private const string mongoVariableName = "tu20_mongodb_url";
        private const string databaseVariableName = "tu20_database_name";

        private const string defaultDatabaseName = "tu20bot";
        
        public string token;
        public string mongoUrl;
        public string databaseName = defaultDatabaseName;
        
        public const string defaultPath = "config.xml";
        public ulong guildId = 230737273350520834; // TU20
        public ulong welcomeChannelId = 736741911150198835; // #bot-testing

        public List<string> welcomeMessages = new List<string> {
            "Welcome",
            "Greetings"
        };

        public readonly List<UserMatch> userRoleMatches = new List<UserMatch>();
        public readonly List<LogEntry> logs = new List<LogEntry>();
        public readonly List<FactoryDescription> factories = new List<FactoryDescription>();

        public static void save(Config config, string path = defaultPath) {
            var serializer = new XmlSerializer(typeof(Config));
            var stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, config);
        }

        public static Config load(string path = defaultPath) {
            if (!File.Exists(path))
                return null;
            
            var serializer = new XmlSerializer(typeof(Config));
            var stream = new FileStream(path, FileMode.Open);
            return (Config)serializer.Deserialize(stream);
        }

        // Environment variables only. Provided for tests.
        public static Config configure() {
            var environmentToken = Environment.GetEnvironmentVariable(tokenVariableName);
            var environmentMongo = Environment.GetEnvironmentVariable(mongoVariableName);
            var environmentDatabase = Environment.GetEnvironmentVariable(databaseVariableName);

            if (string.IsNullOrEmpty(environmentToken))
                return null;
            
            Console.WriteLine("Configuring from environment variables...");
            
            return new Config {
                token = environmentToken,
                mongoUrl = environmentMongo?.NullIfEmpty(),
                databaseName = environmentDatabase?.NullIfEmpty() ?? defaultDatabaseName
            };
        }
        
        public static Config configure(string[] args) {
            if (args.Length >= 2 && args.First() == "init") {
                Console.WriteLine("Configuring from command line...");
                
                return new Config {
                    token = args[1],
                    mongoUrl = args.Length > 2 ? args[2].NullIfEmpty() : null,
                    databaseName = (args.Length > 3 ? args[3].NullIfEmpty() : null) ?? defaultDatabaseName
                };
            }

            // Check environment variables.
            var environment = configure();

            if (environment != null)
                return environment;

            Console.WriteLine("Configuring from console input...");

            Console.Write(" * Discord Bot Token (required): ");
            var token = Console.ReadLine();
            if (string.IsNullOrEmpty(token)) {
                Console.WriteLine("Discord Bot Token is required. Exiting...");
                Environment.Exit(1);
            }

            Console.Write(" * MongoDB URL (optional): ");
            var databaseUrl = Console.ReadLine()?.NullIfEmpty();

            string databaseName = null;
            
            if (databaseUrl != null) {
                Console.Write(" * MongoDB Database Name (optional): ");
                databaseName = Console.ReadLine()?.NullIfEmpty();
            }
            
            var result = new Config {
                token = token,
                mongoUrl = databaseUrl,
                databaseName = databaseName ?? defaultDatabaseName
            };
            
            Console.Write(" * Commit this config (y/N)? ");
            var commit = Console.ReadLine()?.ToLower();

            if (commit != null && (commit == "y" || commit == "yes"))
                save(result);

            return result;
        }
    }
}
