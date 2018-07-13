namespace Lithium.Modules
{
    using System;
    using System.Threading.Tasks;

    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class AutoModeration : Base
    {
        [Command("AddAction")]
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

            Context.Server.ModerationSetup.Settings.AutoTasks.Add(warns, newAction);
            Context.Server.Save();

            return SimpleEmbedAsync("Success, New AutoAction created\n" + 
                                    $"If a user gets **{warns}** warns, they will be automatically **{action.GetDescription()}**\n" + 
                                    "**[Response]**\n" + 
                                    $"{response}");
        }

        [Command("SetActionTimeout")]
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
    }
}
