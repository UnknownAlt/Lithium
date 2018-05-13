using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Lithium.Handlers;
using Lithium.Models;

namespace Lithium.Services
{
    public class TimerService
    {
        public static DateTime LastFireTime = DateTime.MinValue;
        public static int FirePreiod = 1;
        private readonly Timer _timer;


        public TimerService(DiscordSocketClient client)
        {
            _timer = new Timer(async _ =>
                {
                    foreach (var guild in client.Guilds)
                    {
                        TimerLoops.checkbans(guild);
                    }
                    LastFireTime = DateTime.UtcNow;
                },
                null, TimeSpan.Zero, TimeSpan.FromMinutes(FirePreiod));
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart()
        {
            _timer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(FirePreiod));
        }

        public void ChangeRate(int newperiod = 10)
        {
            FirePreiod = newperiod;
            _timer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(FirePreiod));
        }
    }

    public class TimerLoops
    {
        public static async void checkbans(IGuild guild)
        {
            var guildobj = DatabaseHandler.GetGuild(guild.Id);

            if (!guildobj.ModerationSetup.Bans.Any(x => x.Expires && x.ExpiryDate < DateTime.UtcNow)) return;
            var bans = await guild.GetBansAsync();
            foreach (var ban in guildobj.ModerationSetup.Bans.Where(x => x.Expires && x.ExpiryDate < DateTime.UtcNow).ToList())
            {
                var gban = bans.FirstOrDefault(x => x.User.Id == ban.userID);
                if (gban == null) continue;
                try
                {
                    await guild.RemoveBanAsync(ban.userID);
                    guildobj.ModerationSetup.Bans.Remove(ban);
                }
                catch
                {
                    //
                }
            }

            guildobj.Save();
        }
    }
}