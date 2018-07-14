namespace Lithium.Discord.Services
{
    using System;
    using System.Threading;

    using global::Discord;
    using global::Discord.WebSocket;

    using Lithium.Handlers;
    using Lithium.Models;

    using Microsoft.Extensions.DependencyInjection;

    public class TimerService
    {
        public static DateTime LastFireTime { get; set; } = DateTime.MinValue;

        public static int FirePeriod { get; set; } = 1;

        private DatabaseHandler Handler { get; }

        private readonly Timer _timer;

        public TimerService(DiscordShardedClient client, IServiceProvider provider)
        {
            Handler = provider.GetRequiredService<DatabaseHandler>();

            _timer = new Timer(_ =>
                {
                    LogHandler.LogMessage("TimerService Run", LogSeverity.Debug);
                    try
                    {
                        foreach (var guild in client.Guilds)
                        {
                            var model = Handler.Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, guild.Id);
                            if (model != null)
                            {
                                TimerLoops.CheckModActions(model, guild);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }

                    try
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    }
                    catch
                    {
                        // Ignored
                    }

                    LastFireTime = DateTime.UtcNow;
                },
                null, TimeSpan.Zero, TimeSpan.FromMinutes(FirePeriod));
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart()
        {
            _timer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(FirePeriod));
        }

        public void ChangeRate(int newPeriod = 10)
        {
            FirePeriod = newPeriod;
            _timer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(FirePeriod));
        }
    }
}