namespace RavenBOT.Core.Configuration.RavenDB
{
    using System;
    using System.Threading.Tasks;
    
    using Passive.Services.DatabaseService;

    using RavenBOT.Core.Configuration.BotConfig;

    public class BotConfiguration
    {
        private DatabaseService DatabaseService { get; }

        public BotConfiguration(DatabaseService dbService)
        {
            DatabaseService = dbService;
        }

        public async Task<Config> EnsureConfigCreatedAsync()
        {
            using (var session = DatabaseService.Store.OpenAsyncSession())
            {
                {
                    if (await session.Advanced.ExistsAsync("Config"))
                    {
                        return await session.LoadAsync<Config>("Config");
                    }

                    Console.WriteLine("Please enter your bot's token (found at https://discordapp.com/developers/applications/me)");
                    var token = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        throw new Exception("Token must be provided");
                    }

                    var config = new Config();
                    config.Token = token;

                    /*
                    Console.WriteLine("If you intend to use multiple shards, please indicate your shard count (guild count % 2000) (DEFAULT: 1)");

                    var shardString = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(shardString))
                    {
                        shardString = "1";
                    }

                    var shards = int.Parse(shardString);
                    config.Shards = shards;
                    */

                    await session.StoreAsync(config, "Config");
                    await session.SaveChangesAsync();
                    return config;
                }
            }
        }

        public Task<Config> GetConfigAsync()
        {
            return EnsureConfigCreatedAsync();
        }
    }
}
