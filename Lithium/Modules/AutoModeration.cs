namespace Lithium.Modules
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class AutoModeration : Base
    {
        [Command("AddAction")]
        [Summary("Add a new action to be taken when users exceed the specified amount of warns")]
        public Task AddActionAsync(int warns, GuildModel.Moderation.ModerationSettings.WarnLimitAction action, [Remainder]string response = null)
        {
            if (action == GuildModel.Moderation.ModerationSettings.WarnLimitAction.NoAction)
            {
                throw new Exception("You cannot use NoAction for this");
            }

            TimeSpan? expiry;
            if (action == GuildModel.Moderation.ModerationSettings.WarnLimitAction.Mute)
            {
                expiry = Context.Server.ModerationSetup.Settings.AutoMuteExpiry;
            }
            else if (action == GuildModel.Moderation.ModerationSettings.WarnLimitAction.Ban)
            {
                expiry = Context.Server.ModerationSetup.Settings.AutoBanExpiry;
            }
            else
            {
                expiry = null;
            }

            var newAction = new GuildModel.Moderation.ModerationSettings.AutoAction
                                {
                                    LimitAction = action,
                                    WarnLimit = warns,
                                    Response = response,
                                    AutoActionExpiry = expiry
                                };

            Context.Server.ModerationSetup.Settings.AutoTasks.TryGetValue(warns, out var matchAction);
            if (matchAction != null)
            {
                throw new Exception("There is already an auto-action set for that amount of warns");
            }

            if (Context.Server.ModerationSetup.Settings.AutoTasks.Any(a => a.Value.LimitAction == action))
            {
                throw new Exception($"There is already a {action} auto-action");
            }

            Context.Server.ModerationSetup.Settings.AutoTasks.Add(warns, newAction);
            Context.Server.Save();

            return SimpleEmbedAsync("Success, New AutoAction created\n" + 
                                    $"If a user gets **{warns}** warns, they will be automatically **{action.GetDescription()}**\n" + 
                                    "**[Response]**\n" + 
                                    $"{response}");
        }

        [Command("DelAction")]
        [Summary("Delete an action via warns")]
        public Task DeleteActionAsync(int warns)
        {
            Context.Server.ModerationSetup.Settings.AutoTasks.Remove(warns, out var action);
            if (action == null)
            {
                throw new Exception("There is no auto-action set for that amount of warns");
            }

            Context.Server.Save();
            return SimpleEmbedAsync($"Removed Auto Action\n**{action.LimitAction}** on **{action.WarnLimit}** warns");
        }
        
        [Command("GetActions")]
        [Summary("List all actions that have been setup in the current server")]
        public Task GetActionsAsync()
        {
            var embed = new EmbedBuilder();
            foreach (var task in Context.Server.ModerationSetup.Settings.AutoTasks)
            {
                embed.AddField(
                    task.Value.LimitAction.ToString(),
                    $"Users will be **{task.Value.LimitAction.GetDescription()}** after **{task.Value.WarnLimit}** Warns\n"
                    + $"**Expires?:** {(task.Value.AutoActionExpiry.HasValue ? $"after {task.Value.AutoActionExpiry.Value.TotalMinutes} minutes" : "Never")}");
            }

            return ReplyAsync(embed);
        }

        [Command("SetActionTimeout")]
        [Summary("Set the default timeout for a specific action, ie. Minutes before a user is unmuted or unbanned")]
        public Task AddActionAsync(int warns, int minutes)
        {
            Context.Server.ModerationSetup.Settings.AutoTasks.TryGetValue(warns, out var matchAction);
            if (matchAction == null)
            {
                throw new Exception("There is no auto-action set for that amount of warns");
            }

            if (matchAction.LimitAction == GuildModel.Moderation.ModerationSettings.WarnLimitAction.Kick)
            {
                throw new Exception("Kicks cannot expire");
            }

            TimeSpan? expiry;
            if (minutes <= 0)
            {
                expiry = null;
            }
            else
            {
                expiry = TimeSpan.FromMinutes(minutes);
            }

            Context.Server.ModerationSetup.Settings.AutoTasks.Remove(warns);
            matchAction.AutoActionExpiry = expiry;
            Context.Server.ModerationSetup.Settings.AutoTasks.Add(warns, matchAction);
            Context.Server.Save();

            return SimpleEmbedAsync("Success, AutoAction edited\n" + 
                                    $"After **{warns}** warns, users will be **{matchAction.LimitAction.GetDescription()}** and this will expire {(expiry.HasValue ? $"after {expiry.Value.TotalMinutes} minutes" : "Never")}");
        }

        
        [Command("AutoMuteExpiry")]
        [Summary("set the amount of minutes it takes for an auto mute to expire")]
        public Task WarnExpiryTimeAsync(int minutes = 0)
        {
            TimeSpan? time = TimeSpan.FromMinutes(minutes);
            if (minutes == 0)
            {
                time = null;
            }

            Context.Server.ModerationSetup.Settings.AutoMuteExpiry = time;
            Context.Server.Save();
            return ReplyAsync($"Success! After {minutes} minutes, auto-mutes will automatically expire");
        }

        [Command("AutoBanExpiry")]
        [Summary("set the amount of hours it takes for an auto ban to expire")]
        public Task BanExpiryTimeAsync(int hours = 0)
        {
            TimeSpan? time = TimeSpan.FromHours(hours);
            if (hours == 0)
            {
                time = null;
            }

            Context.Server.ModerationSetup.Settings.AutoBanExpiry = time;
            Context.Server.Save();
            return ReplyAsync($"Success! After {hours} hours, auto-bans will automatically expire");
        }
    }
}
