namespace RavenBOT.Core.Configuration.LocalConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public class Initialization
    {
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "setup/Config.json");

        public static Task InitializeAsync()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }

            if (!File.Exists(Path.Combine(ConfigPath)))
            {
                // Initialize the local config for the bot.
                CreateConfigAsync();
            }

            return Task.CompletedTask;
        }

        public static Task CreateConfigAsync()
        {
            var config = new Config();

            Console.WriteLine("Please input the url to your RavenDB instance (DEFAULT: http://127.0.0.1:8080)");
            var databaseUrl = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(databaseUrl))
            {
                databaseUrl = "http://127.0.0.1:8080";
            }

            config.DatabaseConfig.DatabaseUrls = new List<string>
                                                     {
                                                           databaseUrl
                                                     };

            Console.WriteLine("Please input your database name (DEFAULT: PassiveBOT)");
            var databaseName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = "PassiveBOT";
            }

            config.DatabaseConfig.DatabaseName = databaseName;

            Console.WriteLine("New Config Created!");
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            return Task.CompletedTask;
        }

        public static Config GetConfig()
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
        }
    }
}
