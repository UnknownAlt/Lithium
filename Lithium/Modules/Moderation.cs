namespace Lithium.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;
    using global::Discord.WebSocket;

    using Lithium.Discord;
    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Handlers;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.Moderators)]
    public class Moderation : Base
    {
        [Command("ClearAction")]
        public Task ClearWarnsAsync(int actionID)
        {
            var action = Context.Server.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
            if (action == null)
            {
                throw new Exception("No action found with that ID");
            }

            action.ExpiredOrRemoved = true;
            Context.Server.Save();

            return ReplyAsync(new EmbedBuilder { Title = "Action was cleared:", Fields = new List<EmbedFieldBuilder> { action.GetLongField(Context.Guild) } });
        }

        [Command("ClearWarn")]
        public Task ClearWarnsAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Warn, userId);
        }

        [Command("ClearKick")]
        public Task ClearKicksAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Kick, userId);
        }

        [Command("ClearMute")]
        public Task ClearMutesAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Mute, userId);
        }

        [Command("ClearBan")]
        public Task ClearBansAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Ban, userId);
        }

        [Command("ClearAll")]
        public Task ClearAllAsync(ulong userId)
        {
            return ClearResponseAsync(null, userId);
        }

        [Command("ClearWarn")]
        public Task ClearWarnsAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Warn, user.Id);
        }

        [Command("ClearKick")]
        public Task ClearKicksAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user.Id);
        }

        [Command("ClearMute")]
        public Task ClearMutesAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Mute, user.Id);
        }

        [Command("ClearBan")]
        public Task ClearBansAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Ban, user.Id);
        }

        [Command("ClearAll")]
        public Task ClearAllAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(null, user.Id);
        }

        public Task ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType? type, ulong userId)
        {
            var actions = new List<GuildModel.Moderation.ModEvent>();
            var sort = type == null ? Context.Server.ModerationSetup.ModActions.Where(a => a.ExpiredOrRemoved == false && a.UserId == userId) : Context.Server.ModerationSetup.ModActions.Where(a => a.ExpiredOrRemoved == false && a.UserId == userId && a.Action == type);
            
            foreach (var action in sort)
            {
                action.ExpiredOrRemoved = true;
                actions.Add(action);
            }

            Context.Server.Save();

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
                return PagedReplyAsync(new PaginatedMessage
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

            return SimpleEmbedAsync("There were no ModActions of this type for this user to be cleared");
        }

        [Command("UnMute")]
        [Remarks("UnMute the specified user")]
        public Task UnMuteAsync(SocketGuildUser user)
        {
            var mutes = Context.Server.ModerationSetup.ModActions.Where(m => m.Action == GuildModel.Moderation.ModEvent.EventType.Mute && !m.ExpiredOrRemoved && m.UserId == user.Id);
            foreach (var mute in mutes)
            {
                mute.ExpiredOrRemoved = true;
            }

            if (user.Roles.Any(x => x.Id == Context.Server.ModerationSetup.Settings.MutedRoleId))
            {
                try
                {
                    user.RemoveRoleAsync(Context.Guild.GetRole(Context.Server.ModerationSetup.Settings.MutedRoleId));
                    SimpleEmbedAsync("Success, user has been un-muted");
                    Context.Server.Save();
                }
                catch (Exception e)
                {
                    LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    throw new Exception("Unable to modify the target user's role");
                }
            }
            else
            {
                Context.Server.Save();
                SimpleEmbedAsync("Success Removing mute from Mod History, but the user did not have the muted role :shrug:");
            }

            return Task.CompletedTask;
        }
        
        [Priority(1)]
        [Command("Mute")]
        [Remarks("Warn the specified user")]
        public Task MuteUserAsync(SocketGuildUser user, [Remainder]string reason = null)
        {
            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel.CastToSocketTextChannel(), reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Mute, null, null);
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Priority(2)]
        [Command("Mute")]
        [Remarks("Warn the specified user")]
        public Task MuteUserAsync(SocketGuildUser user, int minutes = 0, [Remainder]string reason = null)
        {
            TimeSpan? time = TimeSpan.FromMinutes(minutes);
            if (minutes == 0)
            {
                time = null;
            }

            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel.CastToSocketTextChannel(), reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Mute, null, time);
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Warn")]
        [Remarks("Warn the specified user")]
        public Task WarnUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel.CastToSocketTextChannel(), reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Warn, null, null);
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Kick")]
        [Remarks("Kick the specified user")]
        public Task KickUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.KickMembers)
            {
                throw new Exception("This user has admin or user kick permissions, therefore I cannot perform this action on them");
            }


            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel.CastToSocketTextChannel(), reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Kick, null, null);
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Ban")]
        [Remarks("Ban the specified user")]
        public Task BanUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.BanMembers)
            {
                throw new Exception("This user has admin or user ban permissions, therefore I cannot perform this action on them");
            }

            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel.CastToSocketTextChannel(), reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Ban, null, null);
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("SoftBan")]
        [Remarks("Ban the specified user for the specified amount of hours")]
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
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel.CastToSocketTextChannel(), reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Ban, null, time);
            }

            throw new InvalidOperationException("Target user has higher rank than current user");
        }

        public List<IMessage> GetMessagesAsync(int count = 100)
        {
            var flatten = Context.Channel.GetMessagesAsync(count).Flatten();
            return flatten.Where(x => x.Timestamp.UtcDateTime + TimeSpan.FromDays(14) > DateTime.UtcNow).ToList().Result;
        }

        // TODO Hastebin upload prune log
        [Priority(2)]
        [Command("prune")]
        [Alias("purge", "clear")]
        [Remarks("removes specified amount of messages")]
        public Task PruneAsync(ulong countOrUserId = 100, [Remainder]string reason = null)
        {
            if (countOrUserId < 1000)
            {
                return PruneAsync(PruneType.All, null, (int)countOrUserId, reason);
            }

            return PruneAsync(PruneType.User, countOrUserId, 100, reason);
        }

        [Priority(3)]
        [Command("prune")]
        [Alias("purge", "clear")]
        [Remarks("removes messages from a user in the last 100 messages")]
        public Task PruneAsync(IUser user, int amount = 100, [Remainder]string reason = null)
        {
            return PruneAsync(PruneType.User, user.Id, amount, reason);
        }

        [Priority(2)]
        [Command("prune")]
        [Alias("purge", "clear")]
        [Remarks("removes messages from a role in the last 100 messages")]
        public Task PruneAsync(IRole role, int amount = 100, [Remainder]string reason = null)
        {
            return PruneAsync(PruneType.Role, role.Id, amount, reason);
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

            Context.Server.ModLogAsync(
                Context.Guild,
                new EmbedBuilder
                    {
                        Description =
                            $"Pruned **{messages.Count}** messages in **{Context.Channel.Name}**\n"
                            + $"**Moderator:** {Context.User.Mention} [{Context.User.Id}]\n" + 
                            $"{response}" + 
                            $"**Reason:**\n{reason ?? "N/A"}\n" + 
                            $"**Log:** {responseText.Result ?? "N/A"}",
                        Color = Color.DarkMagenta
                    });

            return SimpleEmbedAsync($"Cleared {messages.Count} messages");
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
                return Haste.HasteBin(string.Join("\n", messages.Select(
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