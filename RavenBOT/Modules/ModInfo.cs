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
    using RavenBOT.Extensions;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class ModInfo : Base
    {
        [Priority(5)]
        [Command("GetAction")]
        [Summary("Get a moderation log action via ID")]
        public async Task GetActionAsync(int actionID)
        {
            var g = await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}");
            var action = g.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
            if (action == null)
            {
                throw new Exception("No action found with that ID");
            }

            await ReplyAsync(new EmbedBuilder { Title = $"Action #{actionID}", Fields = new List<EmbedFieldBuilder> { action.GetLongField(Context.Guild) }, Color = Color.DarkRed });
        }

        [Priority(3)]
        [Command("GetActions")]
        [Summary("Get mod logs of a specific type (optionally specify a user)")]
        public Task GetActionsAsync(GuildService.GuildModel.Moderation.ModEvent.EventType type, SocketGuildUser user = null) => GetModActionsAsync(type, user?.Id);

        [Priority(4)]
        [Command("GetActions")]
        [Summary("Get mod logs of a specific type by user ID")]
        public Task GetActionsAsync(GuildService.GuildModel.Moderation.ModEvent.EventType type, ulong userID) => GetModActionsAsync(type, userID);

        [Command("GetLongActions")]
        [Summary("Get full mod logs of a specific type (optionally specify a user)")]
        public Task GetLongActionsAsync(GuildService.GuildModel.Moderation.ModEvent.EventType type, SocketGuildUser user = null) => GetModActionsAsync(type, user?.Id, true);

        [Command("GetLongActions")]
        [Summary("Get full mod logs of a specific type by user ID")]
        public Task GetLongActionsAsync(GuildService.GuildModel.Moderation.ModEvent.EventType type, ulong userID) => GetModActionsAsync(type, userID, true);

        [Command("ModLog")]
        [Summary("Get the mod logs of a user by ID")]
        public Task ViewModLogAsync(ulong userId) => GetModActionsAsync(null, userId);

        [Command("ModLog")]
        [Summary("Get mod logs (optionally filter for a user)")]
        public Task ViewModLogAsync(SocketGuildUser user = null) => GetModActionsAsync(null, user?.Id);

        [Command("LongModLog")]
        [Summary("Get all mod logs (optionally filter for a user)")]
        public Task ViewLongModLogAsync(SocketGuildUser user = null) => GetModActionsAsync(null, user?.Id, true);

        [Command("LongModLog")]
        [Summary("Get all mod logs by user ID")]
        public Task ViewLongModLogAsync(ulong userId) => GetModActionsAsync(null, userId, true);

        public async Task GetModActionsAsync(GuildService.GuildModel.Moderation.ModEvent.EventType? type, ulong? userID = null, bool showExpired = false)
        {
            var g = await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}");
            IEnumerable<GuildService.GuildModel.Moderation.ModEvent> modEvents = g.ModerationSetup.ModActions;

            if (!showExpired)
            {
                modEvents = modEvents.Where(m => !m.ExpiredOrRemoved).OrderByDescending(m => m.TimeStamp);
            }

            if (type != null && userID == null)
            {
                // Show the given mod action for ALL users
                modEvents = modEvents.Where(x => x.Action == type);
            }
            else if (type == null && userID == null)
            {
                // Show ALL Mod actions for ALL users
                // modEvents = modEvents.ToList();
                // Do nothing here as we are only using the default list
            }
            else
            {
                modEvents = type == null
                                ? modEvents.Where(x => x.UserId == userID)
                                : modEvents.Where(x => x.UserId == userID && x.Action == type);
            }
            
            var pages = new List<PaginatedMessage.Page>();
            var enumerable = modEvents.ToList();

            if (enumerable.Count == 0)
            {
                await SimpleEmbedAsync("There are no actions that apply to your specifications");
                return;
            }

            foreach (var modGroup in enumerable.ToList().SplitList(5))
            {
                pages.Add(new PaginatedMessage.Page
                              {
                                  Fields = modGroup.Select(m => m.GetLongField(Context.Guild)).ToList()
                              });
            }

            await PagedReplyAsync(new PaginatedMessage
                                       {
                                           Pages = pages, 
                                           Color = Color.DarkRed,
                                           Title = userID.HasValue ? $"{Context.Guild.GetUser(userID.Value)}{(type == null ? null : $" {type}")} {enumerable.Count} Actions" : null
                                       }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }
    }
}
