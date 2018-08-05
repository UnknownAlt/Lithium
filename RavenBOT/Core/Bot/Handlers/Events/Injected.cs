namespace RavenBOT.Core.Bot.Handlers.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Addons.PrefixService;
    using Discord.Commands;
    using Discord.WebSocket;

    using Passive.Services.DatabaseService;

    using RavenBOT.Models;

    /// <summary>
    /// The injected elements of the event handler
    /// </summary>
    public partial class EventHandler
    {
        private PrefixService PrefixService { get; }

        private DiscordShardedClient Client { get; }

        private CommandService CommandService { get; }

        private DatabaseService DBService { get; }

        private IServiceProvider Provider { get; }

        private AutoModerator AutoMod { get; }
        
        private readonly Timer _timer;

        private EventServer Events { get; }

        private readonly Dictionary<ulong, GuildEventInfo> eventQueue = new Dictionary<ulong, GuildEventInfo>();
        
        public EventHandler(DiscordShardedClient client, CommandService commands, AutoModerator autoMod, DatabaseService dbService, EventServer events, PrefixService prefix, IServiceProvider provider)
        {
            PrefixService = prefix;
            Client = client;
            Events = events;
            CommandService = commands;
            DBService = dbService;
            Provider = provider;
            AutoMod = autoMod;
            _timer = new Timer(_ =>
                     {
                         LogHandler.LogMessage("EventLogger Run", LogSeverity.Verbose);
                         foreach (var guild in eventQueue)
                         {
                             if (guild.Value.Events.Count(e => e.Value.Type == EventType.messageDeleted) > 20)
                             {
                                 guild.Value.Events = guild.Value.Events.Where(e => e.Value.Type != EventType.messageDeleted).ToDictionary(k => k.Key, k => k.Value);
                             }

                             if (guild.Value.Events.Any())
                             {
                                 var ordered = guild.Value.Events.OrderBy(x => x.Key).Take(10).ToList();
                                 if (client.GetGuild(guild.Key) is SocketGuild eventGuild)
                                 {
                                     if (eventGuild.GetTextChannel(guild.Value.EventChannel) is ITextChannel eventChannel)
                                     {
                                         var most = GetColor(ordered
                                             .GroupBy(i => i.Value.Type)
                                             .OrderByDescending(grp => grp.Count())
                                             .Select(grp => grp.Key)
                                             .First());

                                        var embed = new EmbedBuilder { Fields = ordered.SelectMany(o => o.Value.Fields).ToList(), Color = most };
                                        eventChannel.SendMessageAsync("", false, embed.Build());
                                     }
                                 }

                                 foreach (var pair in ordered)
                                 {
                                     guild.Value.Events.Remove(pair.Key);
                                 }
                             }
                         }
                     },
            null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public Task InitializeAsync() => CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Provider);
    }
}
