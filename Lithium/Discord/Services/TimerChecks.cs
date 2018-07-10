namespace Lithium.Discord.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;

    using global::Discord.WebSocket;

    using Lithium.Handlers;
    using Lithium.Models;

    public class TimerLoops
    {
        public static async void CheckModActions(GuildModel guildModel, SocketGuild guild)
        {
            try
            {
                if (guildModel.ModerationSetup.ModActions.Any(x => x.Action != GuildModel.Moderation.ModEvent.EventType.Kick && x.ExpiredOrRemoved == false))
                {
                    foreach (var action in guildModel.ModerationSetup.ModActions)
                    {
                        if (action.ExpiryDate == null)
                        {
                            continue;
                        }

                        if (!(action.ExpiryDate > DateTime.UtcNow))
                        {
                            continue;
                        }

                        action.ExpiredOrRemoved = true;

                        if (!(guild.GetUser(action.UserId) is SocketGuildUser user))
                        {
                            continue;
                        }

                        await BanMuteClearAsync(guildModel, guild, action, user);
                    }
                }
            }
            catch (Exception e)
            {
                LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        public static async Task BanMuteClearAsync(GuildModel guildModel, SocketGuild guild, GuildModel.Moderation.ModEvent action, SocketGuildUser user)
        {
            switch (action.Action)
            {
                case GuildModel.Moderation.ModEvent.EventType.mute:
                    if (guild.GetRole(guildModel.ModerationSetup.Settings.MutedRoleId) is SocketRole role && user.Roles.Any(x => x.Id == role.Id))
                    {
                        try
                        {
                            var _ = Task.Run(() => user.RemoveRoleAsync(role, new RequestOptions
                                                                                  {
                                                                                      AuditLogReason = 
                                                                                          "Auto UnMuted user.\n"
                                                                                          + $"Mod: {action.ModName} [{action.ModId}]\n"
                                                                                          + $"AutoTrigger?: {action.AutoModReason}\n"
                                                                                          + "Original Reason:\n"
                                                                                          + $"{action.ProvidedReason}\n"
                                                                                          + $"Message: {action.ReasonTrigger?.Message}"
                                                                                  }).ConfigureAwait(false));
                        }
                        catch (Exception e)
                        {
                            LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                        }
                    }

                    break;
                case GuildModel.Moderation.ModEvent.EventType.ban:
                    var bans = await guild.GetBansAsync();
                    if (bans.Any(x => x.User.Id == user.Id))
                    {
                        try
                        {
                            var _ = Task.Run(() => guild.RemoveBanAsync(
                                user.Id,
                                new RequestOptions
                                {
                                    AuditLogReason =
                                            "Auto Unbanned user.\n"
                                            + $"Mod: {action.ModName} [{action.ModId}]\n"
                                            + $"AutoTrigger?: {action.AutoModReason}\n"
                                            + "Original Reason:\n"
                                            + $"{action.ProvidedReason}\n"
                                            + $"Message: {action.ReasonTrigger?.Message}"
                                }).ConfigureAwait(false));
                        }
                        catch (Exception e)
                        {
                            LogHandler.LogMessage(e.ToString(), LogSeverity.Debug);
                        }
                    }

                    break;
            }
        }
    }
}
