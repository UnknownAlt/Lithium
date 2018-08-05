namespace RavenBOT.Core.Bot.Handlers.Timer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.WebSocket;

    using RavenBOT.Core.Bot.Handlers;
    using RavenBOT.Extensions;
    using RavenBOT.Models;

    public class TimerLoops
    {
        public static async void CheckModActions(GuildService.GuildModel guildModel, SocketGuild guild)
        {
            try
            {
                var now = DateTime.UtcNow;
                bool hasChanges = false;
                foreach (var action in guildModel.ModerationSetup.ModActions.Where(a => !a.ExpiredOrRemoved && a.Action != GuildService.GuildModel.Moderation.ModEvent.EventType.Kick && a.ExpiryDate.HasValue))
                {
                    if (action.ExpiryDate > now)
                    {
                        continue;
                    }

                    var gUser = guild.GetUser(action.UserId);

                    if (gUser == null && action.Action != GuildService.GuildModel.Moderation.ModEvent.EventType.Ban)
                    {
                        continue;
                    }

                    action.ExpiredOrRemoved = true;
                    hasChanges = true;
                    await BanMuteClearAsync(guildModel, guild, action, gUser);
                }

                if (hasChanges)
                {
                    guildModel.Save();
                }
            }
            catch (Exception e)
            {
                LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        public static async Task BanMuteClearAsync(GuildService.GuildModel guildModel, SocketGuild guild, GuildService.GuildModel.Moderation.ModEvent action, SocketGuildUser user = null)
        {
            var undoEmbed = new EmbedBuilder
                                {
                                    Fields = new List<EmbedFieldBuilder>
                                                 {
                                                     new EmbedFieldBuilder
                                                         {
                                                             Name =
                                                                 $"{(user?.ToString() ?? guild.GetUser(action.ModId)?.ToString()) ?? $"{action.ModName} [{action.ModId}]"} was Automatically Un{action.Action.GetDescription()}",
                                                             Value =
                                                                 $"**Mod:** {guild.GetUser(action.ModId)?.Mention ?? $"{action.ModName} [{action.ModId}]"}\n" +
                                                                 $"**Expired:** {(action.ExpiryDate.HasValue ? $"{action.ExpiryDate.Value.ToLongDateString()} {action.ExpiryDate.Value.ToLongTimeString()}\n" : "Never\n")}" +
                                                                 (action.AutoModReason == GuildService.GuildModel.Moderation.ModEvent.AutoReason.none ? $"**Reason:** {action.ProvidedReason ?? "N/A"}\n" : $"**Auto-Reason:** {action.AutoModReason}\n")
                                                         }
                                                 },
                                    Color = Color.DarkTeal
                                }.WithCurrentTimestamp();

            if (action.AutoModReason != GuildService.GuildModel.Moderation.ModEvent.AutoReason.none)
            {
                if (action.ReasonTrigger != null)
                {
                    undoEmbed.AddField("Trigger", $"In {user.Guild.GetTextChannel(action.ReasonTrigger.ChannelId)?.Mention}\n**Message:**\n{action.ReasonTrigger.Message}");
                }
            }

            var auditReason = new RequestOptions
                                  {
                                      AuditLogReason =
                                          $"Mod: {action.ModName} [{action.ModId}]\n"
                                          + $"Action: {action.Action}\n"
                                          + (action.AutoModReason == GuildService.GuildModel.Moderation.ModEvent.AutoReason.none ? $"**Reason:** {action.ProvidedReason ?? "N/A"}\n" : $"**Auto-Reason:** {action.AutoModReason}\n")
                                          + $"Trigger: {action.ReasonTrigger?.Message}\n"
                                  };
            
            switch (action.Action)
            {
                case GuildService.GuildModel.Moderation.ModEvent.EventType.Mute:
                    if (guild.GetRole(guildModel.ModerationSetup.Settings.MutedRoleId) is SocketRole role && user.Roles.Any(x => x.Id == role.Id))
                    {
                        try
                        {
                            var _ = Task.Run(() =>
                                {
                                    user.RemoveRoleAsync(role, auditReason).ConfigureAwait(false);
                                    user.Guild.GetTextChannel(guildModel.ModerationSetup.Settings.ModLogChannel)?.SendMessageAsync("", false, undoEmbed.Build()).ConfigureAwait(false);
                                    return Task.CompletedTask;
                                });
                        }
                        catch (Exception e)
                        {
                            LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                        }
                    }

                    break;
                case GuildService.GuildModel.Moderation.ModEvent.EventType.Ban:
                    var bans = await guild.GetBansAsync();
                    if (bans.Any(x => x.User.Id == action.UserId))
                    {
                        try
                        {
                            var _ = Task.Run(() =>
                                {
                                    guild.RemoveBanAsync(action.UserId, auditReason).ConfigureAwait(false);
                                    user.Guild.GetTextChannel(guildModel.ModerationSetup.Settings.ModLogChannel)?.SendMessageAsync("", false, undoEmbed.Build()).ConfigureAwait(false);
                                    return Task.CompletedTask;
                                });
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
