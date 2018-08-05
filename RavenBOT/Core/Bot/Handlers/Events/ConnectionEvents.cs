namespace RavenBOT.Core.Bot.Handlers.Events
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.WebSocket;

    using RavenBOT.Models;

    /// <summary>
    /// The event handler.
    /// </summary>
    public partial class EventHandler
    {
        internal async Task ShardReadyAsync(DiscordSocketClient socketClient)
        {
            LogHandler.LogMessage($"Shard {socketClient.ShardId} || Guilds: {socketClient.Guilds.Count} || Users: {socketClient.Guilds.Sum(x => x.MemberCount)}");

            await socketClient.SetGameAsync($"{PrefixService.GetDefaultPrefix()}help || Shard {socketClient.ShardId}");

            var _ = Task.Run(() =>
                {
                    return DBService.RunInSessionAsync(
                        async s =>
                            {
                                foreach (var guild in socketClient.Guilds)
                                {
                                    if (!await s.Advanced.ExistsAsync($"{guild.Id}"))
                                    {
                                        await s.StoreAsync(new GuildService.GuildModel(guild.Id), $"{guild.Id}");
                                        LogHandler.LogMessage($"Initialized New GuildModel: {guild.Id}");
                                    }
                                }

                                await s.SaveChangesAsync();
                            });
                });
        }

        internal async Task ShardConnectedAsync(DiscordSocketClient socketClient)
        {
            // Ignored
        }

        internal Task LeftGuildAsync(SocketGuild guild)
        {
            DBService.DeleteAsync($"{guild.Id}");
            return Task.CompletedTask;
        }

        internal Task JoinedGuildAsync(SocketGuild guild)
        {
            return DBService.UpdateOrStoreAsync($"{guild.Id}", new GuildService.GuildModel(guild.Id));
        }
    }
}
