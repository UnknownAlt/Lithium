namespace Lithium.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;
    using global::Discord.WebSocket;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

    [CustomPermissions(true)]
    [Group("Mod")]
    public class Moderation : Base
    {
        [Command("UnMute")]
        [Remarks("UnMute the specified user")]
        public Task UnMuteAsync(SocketGuildUser user)
        {
            // TODO Response for unmute
            var mutes = Context.Server.ModerationSetup.ModActions.Where(m => m.Action == GuildModel.Moderation.ModEvent.EventType.mute && !m.ExpiredOrRemoved);
            foreach (var mute in mutes)
            {
                mute.ExpiredOrRemoved = true;
            }

            if (user.Roles.Any(x => x.Id == Context.Server.ModerationSetup.Settings.MutedRoleId))
            {
                user.RemoveRoleAsync(Context.Guild.GetRole(Context.Server.ModerationSetup.Settings.MutedRoleId));
            }

            return Task.CompletedTask;
        }

        [Command("Mute")]
        [Remarks("Warn the specified user")]
        public Task MuteUserAsync(SocketGuildUser user, int minutes = 0)
        {
            TimeSpan? time = TimeSpan.FromMinutes(minutes);
            if (minutes == 0)
            {
                time = null;
            }

            // TODO Check Hierarchy before performing action
            return Context.Server.ModAction(user, Context.User as SocketGuildUser, Context.Channel, null, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.mute, null, time);
        }

        [Command("Warn")]
        [Remarks("Warn the specified user")]
        public Task WarnUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            // TODO Check Hierarchy before performing action
            return Context.Server.ModAction(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.warn, null, null);
        }

        [Command("Kick")]
        [Remarks("Kick the specified user")]
        public Task KickUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.KickMembers)
            {
                throw new Exception("This user has admin or user kick permissions, therefore I cannot perform this action on them");
            }

            // TODO Check Hierarchy before performing action
            return Context.Server.ModAction(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.Kick, null, null);
        }

        [Command("Ban")]
        [Remarks("Ban the specified user")]
        public Task BanUserAsync(SocketGuildUser user, [Remainder] string reason = null)
        {
            if (user.GuildPermissions.Administrator || user.GuildPermissions.BanMembers)
            {
                throw new Exception("This user has admin or user ban permissions, therefore I cannot perform this action on them");
            }

            // TODO Check Hierarchy before performing action
            return Context.Server.ModAction(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.ban, null, null);
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

            // TODO Check Hierarchy before performing action
            return Context.Server.ModAction(user, Context.User as SocketGuildUser, Context.Channel, reason, GuildModel.Moderation.ModEvent.AutoReason.none, GuildModel.Moderation.ModEvent.EventType.ban, null, time);
        }

        public List<IMessage> GetMessagesAsync(int count = 100)
        {
            var flatten = Context.Channel.GetMessagesAsync(count).Flatten();
            return flatten.Where(x => x.Timestamp.UtcDateTime + TimeSpan.FromDays(14) > DateTime.UtcNow).ToList().Result;
        }

        // TODO Kicks warns bans list commands

        // TODO Logging for prune commands
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