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

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Handlers;
    using Lithium.Models;

    [CustomPermissions(true)]
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

            return ReplyAsync(new EmbedBuilder { Title = "Action was cleared:", Fields = new List<EmbedFieldBuilder> { action.GetLongField() } });
        }

        [Command("ClearWarn")]
        public Task ClearWarnsAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.warn, userId);
        }

        [Command("ClearKick")]
        public Task ClearKicksAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Kick, userId);
        }

        [Command("ClearMute")]
        public Task ClearMutesAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.mute, userId);
        }

        [Command("ClearBan")]
        public Task ClearBansAsync(ulong userId)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.ban, userId);
        }

        [Command("ClearAll")]
        public Task ClearAllAsync(ulong userId)
        {
            return ClearResponseAsync(null, userId);
        }

        [Command("ClearWarn")]
        public Task ClearWarnsAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.warn, user.Id);
        }

        [Command("ClearKick")]
        public Task ClearKicksAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user.Id);
        }

        [Command("ClearMute")]
        public Task ClearMutesAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.mute, user.Id);
        }

        [Command("ClearBan")]
        public Task ClearBansAsync(SocketGuildUser user)
        {
            return ClearResponseAsync(GuildModel.Moderation.ModEvent.EventType.ban, user.Id);
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
                                      Fields = actionGroup.Select(a => a.GetLongField()).ToList()
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
            var mutes = Context.Server.ModerationSetup.ModActions.Where(m => m.Action == GuildModel.Moderation.ModEvent.EventType.mute && !m.ExpiredOrRemoved && m.UserId == user.Id);
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
                return Context.Server.ModActionAsync(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.mute, null, null);
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
                return Context.Server.ModActionAsync(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.mute, null, time);
            }

            throw new InvalidOperationException("Target user has higher permissions than current user");
        }

        [Command("Warn")]
        [Remarks("Warn the specified user")]
        public Task WarnUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (Context.User.CastToSocketGuildUser().IsHigherRankedThan(user))
            {
                return Context.Server.ModActionAsync(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.warn, null, null);
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
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Kick, null, null);
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
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.ban, null, null);
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
                return Context.Server.ModActionAsync(user, Context.User.CastToSocketGuildUser(), Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.ban, null, time);
            }

            throw new InvalidOperationException("Target user has higher rank than current user");
        }

        public List<IMessage> GetMessagesAsync(int count = 100)
        {
            var flatten = Context.Channel.GetMessagesAsync(count).Flatten();
            return flatten.Where(x => x.Timestamp.UtcDateTime + TimeSpan.FromDays(14) > DateTime.UtcNow).ToList().Result;
        }

        // TODO Kicks warns bans list commands

        // TODO Logging for prune commands
        // TODO Optional reason for pruning
        // TODO Hastebin upload prune log
        [Command("prune")]
        [Alias("purge", "clear")]
        [Remarks("removes specified amount of messages")]
        public async Task PruneAsync(int count = 100)
        {
            if (count < 1)
            {
                await ReplyAsync("**ERROR: **Please Specify the amount of messages you want to clear");
            }
            else if (count > 100)
            {
                await ReplyAsync("**Error: **I can only clear 100 Messages at a time!");
            }
            else
            {
                await Context.Message.DeleteAsync().ConfigureAwait(false);
                var limit = count < 100 ? count : 100;

                // var enumerable = await Context.Channel.GetMessagesAsync(limit).Flatten().ConfigureAwait(false);
                var enumerable = GetMessagesAsync(limit);
                try
                {
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(enumerable).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored
                }

                await ReplyAsync($"Cleared **{enumerable.Count}** Messages");
            }
        }

        [Command("prune")]
        [Alias("purge", "clear")]
        [Remarks("removes messages from a user in the last 100 messages")]
        public async Task PruneAsync(IUser user)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var enumerable = GetMessagesAsync();
            var list = enumerable.Where(x => x.Author == user).ToList();
            try
            {
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(list).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }

            await ReplyAsync($"Cleared **{user.Username}'s** Messages (Count = {list.Count})");
        }

        [Command("pruneID")]
        [Remarks("removes messages from a user ID in the last 100 messages")]
        public async Task PruneAsync(ulong userID)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var enumerable = GetMessagesAsync();
            var list = enumerable.Where(x => x.Author.Id == userID).ToList();
            try
            {
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(list).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }

            await ReplyAsync($"Cleared Messages (Count = {list.Count})");
        }

        [Command("prune")]
        [Alias("purge", "clear")]
        [Remarks("removes messages from a role in the last 100 messages")]
        public async Task PruneAsync(IRole role)
        {
            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var enumerable = GetMessagesAsync();
            var list = enumerable.ToList().Where(x =>
                Context.Guild.GetUser(x.Author.Id) != null &&
                Context.Guild.GetUser(x.Author.Id).Roles.Any(r => r.Id == role.Id)).ToList();

            try
            {
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(list).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }

            await ReplyAsync($"Cleared Messages (Count = {list.Count})");
        }
    }
}