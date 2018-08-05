namespace RavenBOT.Core.Bot.Handlers
{
    using System;
    using System.Threading.Tasks;

    using Discord;
    using Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;

    using RavenBOT.Core.Configuration.RavenDB;

    using EventHandler = Events.EventHandler;

    public class BotHandler
    {
        private DiscordShardedClient Client { get; }
        
        private IServiceProvider Provider { get; }

        private EventHandler EventHandler { get; }

        public BotHandler(DiscordShardedClient client, EventHandler eventHandler, IServiceProvider provider)
        {
            Client = client;
            Provider = provider;
            EventHandler = eventHandler;
        }

        public async Task InitializeAsync()
        {
            Client.Log += message =>
                {
                    LogHandler.LogMessage(message.Message, message.Severity);
                    return Task.CompletedTask;
                };

            Client.MessageReceived += EventHandler.MessageReceivedAsync;
            Client.ShardReady += EventHandler.ShardReadyAsync;
            Client.ShardConnected += EventHandler.ShardConnectedAsync;
            Client.ReactionAdded += EventHandler.ReactionAddedAsync;
            Client.JoinedGuild += EventHandler.JoinedGuildAsync;
            Client.LeftGuild += EventHandler.LeftGuildAsync;


            // Log-able Events
            Client.MessageDeleted += EventHandler.MessageDeletedAsync;
            Client.MessageUpdated += EventHandler.MessageUpdatedAsync;
            Client.ChannelCreated += EventHandler.ChannelCreatedAsync;
            Client.ChannelDestroyed += EventHandler.ChannelDeletedAsync;
            Client.ChannelUpdated += EventHandler.ChannelUpdatedAsync;
            Client.UserBanned += EventHandler.UserBannedAsync;
            Client.UserJoined += EventHandler.UserJoinedAsync;
            Client.UserLeft += EventHandler.UserLeftAsync;
            Client.UserUnbanned += EventHandler.UserUnbannedAsync;
            Client.GuildMemberUpdated += EventHandler.GuildMemberUpdatedAsync;
            
            var config = await Provider.GetRequiredService<BotConfiguration>().GetConfigAsync();

            await Client.LoginAsync(TokenType.Bot, config.Token);
            await Client.StartAsync();
            await EventHandler.InitializeAsync();
        }
    }
}
