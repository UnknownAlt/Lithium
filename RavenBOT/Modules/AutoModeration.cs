namespace RavenBOT.Modules
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Extensions;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class AutoModeration : Base
    {
        [Command("AddAction")]
        [Summary("Add a new action to be taken when users exceed the specified amount of warns")]
        public Task AddActionAsync(int warns, GuildService.GuildModel.Moderation.ModerationSettings.WarnLimitAction action, [Remainder]string response = null)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (action == GuildService.GuildModel.Moderation.ModerationSettings.WarnLimitAction.NoAction)
                        {
                            throw new Exception("You cannot use NoAction for this");
                        }

                        TimeSpan? expiry;
                        if (action == GuildService.GuildModel.Moderation.ModerationSettings.WarnLimitAction.Mute)
                        {
                            expiry = g.ModerationSetup.Settings.AutoMuteExpiry;
                        }
                        else if (action == GuildService.GuildModel.Moderation.ModerationSettings.WarnLimitAction.Ban)
                        {
                            expiry = g.ModerationSetup.Settings.AutoBanExpiry;
                        }
                        else
                        {
                            expiry = null;
                        }

                        var newAction = new GuildService.GuildModel.Moderation.ModerationSettings.AutoAction { LimitAction = action, WarnLimit = warns, Response = response, AutoActionExpiry = expiry };

                        g.ModerationSetup.Settings.AutoTasks.TryGetValue(warns, out var matchAction);
                        if (matchAction != null)
                        {
                            throw new Exception("There is already an auto-action set for that amount of warns");
                        }

                        if (g.ModerationSetup.Settings.AutoTasks.Any(a => a.Value.LimitAction == action))
                        {
                            throw new Exception($"There is already a {action} auto-action");
                        }

                        g.ModerationSetup.Settings.AutoTasks.Add(warns, newAction);

                        return SimpleEmbedAsync("Success, New AutoAction created\n" + $"If a user gets **{warns}** warns, they will be automatically **{action.GetDescription()}**\n" + "**[Response]**\n" + $"{response}");
                    });
        }

        [Command("DelAction")]
        [Summary("Delete an action via warns")]
        public Task DeleteActionAsync(int warns)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g =>
                    {
                        g.ModerationSetup.Settings.AutoTasks.Remove(warns, out var action);
                        if (action == null)
                        {
                            throw new Exception("There is no auto-action set for that amount of warns");
                        }

                        return SimpleEmbedAsync($"Removed Auto Action\n**{action.LimitAction}** on **{action.WarnLimit}** warns");
                    });
        }

        [Command("GetActions")]
        [Summary("List all actions that have been setup in the current server")]
        public async Task GetActionsAsync()
        {
            var embed = new EmbedBuilder();
            foreach (var task in (await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}")).ModerationSetup.Settings.AutoTasks)
            {
                embed.AddField(
                    task.Value.LimitAction.ToString(),
                    $"Users will be **{task.Value.LimitAction.GetDescription()}** after **{task.Value.WarnLimit}** Warns\n"
                    + $"**Expires?:** {(task.Value.AutoActionExpiry.HasValue ? $"after {task.Value.AutoActionExpiry.Value.TotalMinutes} minutes" : "Never")}");
            }

            await ReplyAsync(embed);
        }

        [Command("SetActionTimeout")]
        [Summary("Set the default timeout for a specific action, ie. Minutes before a user is un-muted or unbanned")]
        public Task AddActionAsync(int warns, int minutes)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g =>
                        {
                            g.ModerationSetup.Settings.AutoTasks.TryGetValue(warns, out var matchAction);
                            if (matchAction == null)
                            {
                                throw new Exception("There is no auto-action set for that amount of warns");
                            }

                            if (matchAction.LimitAction == GuildService.GuildModel.Moderation.ModerationSettings.WarnLimitAction.Kick)
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

                            g.ModerationSetup.Settings.AutoTasks.Remove(warns);
                            matchAction.AutoActionExpiry = expiry;
                            g.ModerationSetup.Settings.AutoTasks.Add(warns, matchAction);

                            return SimpleEmbedAsync("Success, AutoAction edited\n" +
                                                    $"After **{warns}** warns, users will be **{matchAction.LimitAction.GetDescription()}** and this will expire {(expiry.HasValue ? $"after {expiry.Value.TotalMinutes} minutes" : "Never")}");
                        });
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

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    g =>
                        {
                            g.ModerationSetup.Settings.AutoMuteExpiry = time;
                            return ReplyAsync($"Success! After {minutes} minutes, auto-mutes will automatically expire");
                        });
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

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.ModerationSetup.Settings.AutoBanExpiry = time;
                        return ReplyAsync($"Success! After {hours} hours, auto-bans will automatically expire");
                    });
        }
    }
}
