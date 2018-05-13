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
                        TimerLoops.checkmutes(guild);
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
            try
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
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }

        public static async void checkmutes(IGuild guild)
        {
            var guildobj = DatabaseHandler.GetGuild(guild.Id);
            if (!guildobj.ModerationSetup.Mutes.MutedUsers.Any()) return;
            var mutedrole = guild.Roles.FirstOrDefault(x => x.Id == guildobj.ModerationSetup.Mutes.mutedrole);
            if (mutedrole == null) return;
            foreach (var mute in guildobj.ModerationSetup.Mutes.MutedUsers.ToList())
            {
                try
                {
                    if (!mute.expires) continue;
                    bool removemute = false;
                    var muteduser = await guild.GetUserAsync(mute.userid);
                    if (muteduser == null)
                    {
                        removemute = true;
                    }
                    else
                    {
                        if (mute.expiry < DateTime.UtcNow)
                        {
                            removemute = true;
                            if (!muteduser.RoleIds.Contains(mutedrole.Id))
                            {
                                await muteduser.RemoveRoleAsync(mutedrole);
                            }
                        }
                    }

                    if (removemute)
                    {
                        guildobj.ModerationSetup.Mutes.MutedUsers.Remove(mute);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                }
            }
            guildobj.Save();
        }
    }
}