namespace RavenBOT
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Discord.Addons.PrefixService;
    using Discord.Commands;
    using Discord.WebSocket;

    using Lithium.Discord.Services;

    using Microsoft.Extensions.DependencyInjection;
    
    using Passive.Services.DatabaseService;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Core.Bot.Handlers;
    using RavenBOT.Core.Bot.Handlers.Timer;
    using RavenBOT.Core.Configuration.LocalConfig;
    using RavenBOT.Core.Configuration.RavenDB;
    using RavenBOT.Models;

    using EventHandler = Core.Bot.Handlers.Events.EventHandler;

    public class Program
    {
        public static void Main(string[] args)
        {
            StartAsync().GetAwaiter().GetResult();
        }

        public static async Task StartAsync()
        {
            await Initialization.InitializeAsync();
            var config = Initialization.GetConfig();
            var provider = new ServiceCollection()
                .AddSingleton<DatabaseService>()
                .AddSingleton(new CommandService(new CommandServiceConfig
                                                     {
                                                         ThrowOnError = false,
                                                         CaseSensitiveCommands = false,
                                                         IgnoreExtraArgs = false,
                                                         DefaultRunMode = RunMode.Sync,
                                                         LogLevel = config.LogLevel
                                                     }))
                .AddSingleton<HttpClient>()
                .AddSingleton(new Random(Guid.NewGuid().GetHashCode()))
                .AddSingleton<Management>()
                .AddSingleton<BotConfiguration>()
                .AddSingleton(x => new PrefixService(config.DefaultPrefix, x.GetRequiredService<DatabaseService>().Store))
                .AddSingleton(x => new DiscordShardedClient(new DiscordSocketConfig
                                                                {
                                                                    AlwaysDownloadUsers = false,
                                                                    MessageCacheSize = 50,
                                                                    LogLevel = config.LogLevel,
                                                                    TotalShards = 1
                                                                }))
                .AddSingleton<EventHandler>()
                .AddSingleton<BotHandler>()
                .AddSingleton<Interactive>()
                .AddSingleton<EventServer>()
                .AddSingleton<TicketService>()
                .AddSingleton<Perspective.Api>()
                .AddSingleton<AutoModerator>()
                .AddSingleton<TimerService>()
                .BuildServiceProvider();

            await provider.GetRequiredService<DatabaseService>().InitializeAsync(config.DatabaseConfig);
            await provider.GetRequiredService<Management>().CheckDatabaseCreationAsync();
            await provider.GetRequiredService<PrefixService>().InitializeAsync();
            await provider.GetRequiredService<BotHandler>().InitializeAsync();
            GuildService.Initialize(provider.GetRequiredService<DatabaseService>());
            provider.GetRequiredService<Perspective.Api>().Initialize((await provider.GetRequiredService<BotConfiguration>().GetConfigAsync()).PerspectiveToken);
            provider.GetRequiredService<TimerService>().Restart();

            await Task.Delay(-1);
        }
    }
}
