namespace RavenBOT.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Addons.Interactive;
    using Discord.Commands;
    using Discord.WebSocket;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Core.Bot.Handlers;
    using RavenBOT.Extensions;
    using RavenBOT.Models;
    using RavenBOT.Modules.Extras;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Moderators)]
    public class Moderation : Base
    {
        [Command("ClearAction")]
        [Summary("Expire a mod log action by specifying it's ID")]
        public Task ClearWarnsAsync(int actionID)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        var action = g.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
                        if (action == null)
                        {
                            throw new Exception("No action found with that ID");
                        }

                        // TODO Ensure this is also edited ie, remove user from warn role/unban user
                        action.ExpiredOrRemoved = true;

                        return ReplyAsync(
                            new EmbedBuilder
                                {
                                    Title = "Action was cleared:",
                                    Fields = new List<EmbedFieldBuilder>
                                                 {
                                                     action.GetLongField(Context.Guild),
                                                     new EmbedFieldBuilder
                                                         {
                                                             Name = "Info",
                                                             Value = "Note, for warns and bans you will need to manually undo roles/unban the user"
                                                         }
                                                 }
                                });
                    });
        }

        [Command("ClearWarns")]
        [Summary("Clear all warns on a user by user ID")]
        public Task ClearWarnsAsync(ulong userId)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Warn, userId);
        }

        [Command("ClearKicks")]
        [Summary("Clear all kicks on a user by user ID")]
        public Task ClearKicksAsync(ulong userId)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Kick, userId);
        }

        [Command("ClearMutes")]
        [Summary("Clear all mutes on a user by user ID")]
        public Task ClearMutesAsync(ulong userId)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Mute, userId);
        }

        [Command("ClearBans")]
        [Summary("Clear all bans on a user by user ID")]
        public Task ClearBansAsync(ulong userId)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Ban, userId);
        }

        [Command("ClearAll")]
        [Summary("Clear all mod actions on a user via user iD")]
        public Task ClearAllAsync(ulong userId)
        {
            return ClearResponseAsync(null, userId);
        }

        [Command("ClearWarns")]
        [Summary("Clear all warns on a user")]
        public Task ClearWarnsAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Warn, user.Id);
        }

        [Command("ClearKicks")]
        [Summary("Clear all kicks on a user")]
        public Task ClearKicksAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Kick, user.Id);
        }

        [Command("ClearMutes")]
        [Summary("Clear all mutes on a user")]
        public Task ClearMutesAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Mute, user.Id);
        }

        [Command("ClearBans")]
        [Summary("Clear all bans on a user")]
        public Task ClearBansAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType.Ban, user.Id);
        }

        [Command("ClearAll")]
        [Summary("Clear all mod actions on a user")]
        public Task ClearAllAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(null, user.Id);
        }

        public async Task ClearResponseAsync(GuildService.GuildModel.Moderation.ModEvent.EventType? type, ulong userId)
        {
            var actions = new List<GuildService.GuildModel.Moderation.ModEvent>();
            await Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        var sort = type == null ? g.ModerationSetup.ModActions.Where(a => a.ExpiredOrRemoved == false && a.UserId == userId) : g.ModerationSetup.ModActions.Where(a => a.ExpiredOrRemoved == false && a.UserId == userId && a.Action == type);

                        foreach (var action in sort)
                        {
                            action.ExpiredOrRemoved = true;
                            actions.Add(action);
                        }

                        return Task.CompletedTask;
                    });

            if (actions.Any())
            {
                var pages = new List<PaginatedMessage.Page>();
                foreach (var actionGroup in actions.OrderByDescending(a => a.TimeStamp).ToList().SplitList(5))
                {
                    pages.Add(new PaginatedMessage.Page
                    {
                        Fields = actionGroup.Select(a => a.GetLongField(Context.Guild)).ToList()
                    });
                }

                await PagedReplyAsync(new PaginatedMessage
                {
                    Pages = pages,
                    Title = "Cleared the following actions from the provided user"
                }, new ReactionList
                {
                    Forward = true,
                    Backward = true,
                    Trash = true
                });
            }

            await SimpleEmbedAsync("There were no ModActions of this type for this user to be cleared");
        }

        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Command("UnMute")]
        [Summary("UnMute the specified user")]
        public Task UnMuteAsync(SocketGuildUser user)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        var mutes = g.ModerationSetup.ModActions.Where(m => m.Action == GuildService.GuildModel.Moderation.ModEvent.EventType.Mute && !m.ExpiredOrRemoved && m.UserId == user.Id);
                        foreach (var mute in mutes)
                        {
                            mute.ExpiredOrRemoved = true;
                        }

                        if (user.Roles.Any(x => x.Id == g.ModerationSetup.Settings.MutedRoleId))
                        {
                            try
                            {
                                user.RemoveRoleAsync(Context.Guild.GetRole(g.ModerationSetup.Settings.MutedRoleId));
                                SimpleEmbedAsync("Success, user has been un-muted");
                            }
                            catch (Exception e)
                            {
                                LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                                throw new Exception("Unable to modify the target user's role");
                            }
                        }
                        else
                        {
                            SimpleEmbedAsync("Success Removing mute from Mod History, but the user did not have the muted role :shrug:");
                        }

                        return Task.CompletedTask;
                    });
        }

        [Priority(1)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Command("Mute")]
        [Summary("Warn the specified user")]
        public Task MuteUserAsync(SocketGuildUser user, [Remainder]string reason = null)
        {
            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g => g.ModActionAsync(
                        user,
                        Context.User.CastToSocketGuildUser(),
                        Context.Channel.CastToSocketTextChannel(),
                        reason,
                        GuildService.GuildModel.Moderation.ModEvent.AutoReason.none,
                        GuildService.GuildModel.Moderation.ModEvent.EventType.Mute,
                        null,
                        null,
                        GuildService.GuildModel.AdditionalDetails.Default()));
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Priority(2)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Command("Mute")]
        [Summary("Warn the specified user")]
        public Task MuteUserAsync(SocketGuildUser user, int minutes = 0, [Remainder]string reason = null)
        {
            TimeSpan? time = TimeSpan.FromMinutes(minutes);
            if (minutes == 0)
            {
                time = null;
            }

            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g => g.ModActionAsync(
                        user,
                        Context.User.CastToSocketGuildUser(),
                        Context.Channel.CastToSocketTextChannel(),
                        reason,
                        GuildService.GuildModel.Moderation.ModEvent.AutoReason.none,
                        GuildService.GuildModel.Moderation.ModEvent.EventType.Mute,
                        null,
                        time,
                        GuildService.GuildModel.AdditionalDetails.Default()));
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Warn")]
        [Summary("Warn the specified user")]
        public Task WarnUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g => g.ModActionAsync(
                        user,
                        Context.User.CastToSocketGuildUser(),
                        Context.Channel.CastToSocketTextChannel(),
                        reason,
                        GuildService.GuildModel.Moderation.ModEvent.AutoReason.none,
                        GuildService.GuildModel.Moderation.ModEvent.EventType.Warn,
                        null,
                        null,
                        GuildService.GuildModel.AdditionalDetails.Default()));
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Kick")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [Summary("Kick the specified user")]
        public Task KickUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.KickMembers)
            {
                throw new Exception("This user has admin or user kick permissions, therefore I cannot perform this action on them");
            }

            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g => g.ModActionAsync(
                        user,
                        Context.User.CastToSocketGuildUser(),
                        Context.Channel.CastToSocketTextChannel(),
                        reason,
                        GuildService.GuildModel.Moderation.ModEvent.AutoReason.none,
                        GuildService.GuildModel.Moderation.ModEvent.EventType.Kick,
                        null,
                        null,
                        GuildService.GuildModel.AdditionalDetails.Default()));
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Ban")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Ban the specified user")]
        public Task BanUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.BanMembers)
            {
                throw new Exception("This user has admin or user ban permissions, therefore I cannot perform this action on them");
            }

            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g => g.ModActionAsync(
                        user,
                        Context.User.CastToSocketGuildUser(),
                        Context.Channel.CastToSocketTextChannel(),
                        reason,
                        GuildService.GuildModel.Moderation.ModEvent.AutoReason.none,
                        GuildService.GuildModel.Moderation.ModEvent.EventType.Ban,
                        null,
                        null,
                        GuildService.GuildModel.AdditionalDetails.Default()));
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        // TODO Unban

        [Command("SoftBan")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [Summary("Ban the specified user for the specified amount of hours")]
        public Task SoftBanUserAsync(SocketGuildUser user, int hours, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.BanMembers)
            {
                throw new Exception("This user has admin or user ban permissions, therefore I cannot perform this action on them");
            }

            TimeSpan? time = TimeSpan.FromHours(hours);
            if (hours == 0)
            {
                time = null;
            }

            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g => g.ModActionAsync(
                        user,
                        Context.User.CastToSocketGuildUser(),
                        Context.Channel.CastToSocketTextChannel(),
                        reason,
                        GuildService.GuildModel.Moderation.ModEvent.AutoReason.none,
                        GuildService.GuildModel.Moderation.ModEvent.EventType.Ban,
                        null,
                        time,
                        GuildService.GuildModel.AdditionalDetails.Default()));
            }

            throw new InvalidOperationException("Target user has higher rank than current user");
        }

        public List<IMessage> GetMessagesAsync(int count = 100)
        {
            var flatten = Context.Channel.GetMessagesAsync(count).Flatten();
            return flatten.Where(x => x.Timestamp.UtcDateTime + TimeSpan.FromDays(14) > DateTime.UtcNow).ToList().Result;
        }

        [Priority(2)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Command("prune")]
        [Alias("purge", "clear")]
        [Summary("removes specified amount of messages")]
        public Task PruneAsync(ulong countOrUserId = 100, [Remainder]string reason = null)
        {
            if (countOrUserId < 1000)
            {
                return PruneAsync(PruneType.All, null, (int)countOrUserId, reason);
            }

            return PruneAsync(PruneType.User, countOrUserId, 100, reason);
        }

        [Priority(3)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Command("prune")]
        [Alias("purge", "clear")]
        [Summary("removes messages from a user in the last 100 messages")]
        public Task PruneAsync(IUser user, int amount = 100, [Remainder]string reason = null)
        {
            return PruneAsync(PruneType.User, user.Id, amount, reason);
        }

        [Priority(2)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Command("prune")]
        [Alias("purge", "clear")]
        [Summary("removes messages from a role in the last 100 messages")]
        public Task PruneAsync(IRole role, int amount = 100, [Remainder]string reason = null)
        {
            return PruneAsync(PruneType.Role, role.Id, amount, reason);
        }

        [Command("SetReason")]
        [Summary("Set the reason for a specific mod action")]
        [Remarks("Can only be run by the original mod of the action")]
        public Task SetReasonAsync(int actionID, [Remainder]string reason = null)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        var action = g.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
                        if (action == null)
                        {
                            throw new Exception("No action found with that ID");
                        }

                        if (action.ModId != Context.User.Id)
                        {
                            throw new Exception("You are not the initial moderator for this action");
                        }

                        var oldAction = action.ProvidedReason;
                        action.ProvidedReason = reason;

                        return ReplyAsync(new EmbedBuilder
                        {
                            Title = "Action Reason was updated:",
                            Fields = new List<EmbedFieldBuilder>
                                        {
                                            action.GetLongField(Context.Guild),
                                            new EmbedFieldBuilder
                                                {
                                                    Name = "Reason Update",
                                                    Value = $"**Previous:** {oldAction ?? "N/A"}\n" +
                                                            $"**New:** {reason ?? "N/A"}"
                                                }
                                        },
                            Color = Color.DarkRed
                        });
                    });
        }

        public enum PruneType
        {
            Role,
            User,
            All
        }

        public enum PruneMethod
        {
            Bulk,
            Individual
        }

        public Task PruneAsync(PruneType type, ulong? id, int count = 100, string reason = null, PruneMethod method = PruneMethod.Bulk)
        {
            if (type != PruneType.All && id == null)
            {
                throw new Exception("ID Must be provided for role and user prunes");
            }

            Context.Message.DeleteAsync().ConfigureAwait(false);
            var enumerable = GetMessagesAsync(count);

            List<IMessage> messages;
            string response;
            if (id.HasValue)
            {
                if (type == PruneType.Role)
                {
                    messages = enumerable.Where(m => !m.Author.IsWebhook && Context.Guild.GetUser(m.Author.Id)?.Roles.Any(r => r.Id == id.Value) == true).ToList();
                    response = $"**Pruned Role:** {Context.Guild.GetRole(id.Value).Mention}\n";
                }
                else if (type == PruneType.User)
                {
                    messages = enumerable.Where(m => m.Author.Id == id).ToList();
                    response = $"**Pruned User:** {Context.Guild.GetUser(id.Value)?.Mention ?? $"[{id.Value}]"}\n";
                }
                else
                {
                    throw new Exception("ID cannot be specified for all message pruning");
                }
            }
            else
            {
                messages = enumerable;
                response = "**Pruned Messages:** All Messages\n";
            }

            if (messages.Count == 0)
            {
                return SimpleEmbedAsync("No messages found with the given parameters");
            }

            var responseText = PruneMessagesAsync(messages, method);

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.ModLogAsync(
                            Context.Guild,
                            new EmbedBuilder
                            {
                                Description =
                                        $"Pruned **{messages.Count}** messages in **{Context.Channel.Name}**\n"
                                        + $"**Moderator:** {Context.User.Mention} [{Context.User.Id}]\n"
                                        + $"{response}" + $"**Reason:**\n{reason ?? "N/A"}\n"
                                        + $"**Log:** {responseText.Result ?? "N/A"}",
                                Color = Color.DarkMagenta
                            });

                        return SimpleEmbedAsync($"Cleared {messages.Count} messages");
                    });
        }

        public Task<string> PruneMessagesAsync(List<IMessage> messages, PruneMethod method)
        {
            if (method == PruneMethod.Bulk)
            {
                if (messages.Count > 100)
                {
                    var msgGroups = messages.ToList().SplitList(100);
                    foreach (var msgGroup in msgGroups)
                    {
                        try
                        {
                            Context.Channel.CastToSocketTextChannel().DeleteMessagesAsync(msgGroup).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                        }
                    }
                }
                else
                {
                    Context.Channel.CastToSocketTextChannel().DeleteMessagesAsync(messages).ConfigureAwait(false);
                }
            }
            else
            {
                if (messages.Count > 20)
                {
                    throw new Exception("Please use bulk delete for more than 20 messages");
                }

                foreach (var message in messages)
                {
                    message.DeleteAsync();
                }
            }

            try
            {
                return Haste.HasteBinAsync(string.Join("\n", messages.Select(
                    m => $"{m.Author.ToString()} at {m.Timestamp}\n" +
                         $"{m.Content}")), ".txt");
            }
            catch
            {
                return Task.FromResult("");
            }
        }
    }
}