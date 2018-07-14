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
    using Lithium.Discord.Preconditions;
    using Lithium.Models;
    using Lithium.Discord.Extensions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class ModInfo : Base
    {
        [Priority(5)]
        [Command("GetAction")]
        [Summary("Get a moderation log action via ID")]
        public Task GetActionAsync(int actionID)
        {
            var action = Context.Server.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
            if (action == null)
            {
                throw new Exception("No action found with that ID");
            }

            return ReplyAsync(new EmbedBuilder { Title = $"Action ${actionID}", Fields = new List<EmbedFieldBuilder> { action.GetLongField(Context.Guild) } });
        }

        [Priority(3)]
        [Command("GetActions")]
        [Summary("Get mod logs of a specific type (optionally specify a user)")]
        public Task GetActionsAsync(GuildModel.Moderation.ModEvent.EventType type, SocketGuildUser user = null)
        {
            return GetModActionsAsync(type, user?.Id);
        }

        [Priority(4)]
        [Command("GetActions")]
        [Summary("Get mod logs of a specific type by user ID")]
        public Task GetActionsAsync(GuildModel.Moderation.ModEvent.EventType type, ulong userID)
        {
            return GetModActionsAsync(type, userID);
        }

        [Command("GetLongActions")]
        [Summary("Get full mod logs of a specific type (optionally specify a user)")]
        public Task GetLongActionsAsync(GuildModel.Moderation.ModEvent.EventType type, SocketGuildUser user = null)
        {
            return GetModActionsAsync(type, user?.Id, true);
        }

        [Command("GetLongActions")]
        [Summary("Get full mod logs of a specific type by user ID")]
        public Task GetLongActionsAsync(GuildModel.Moderation.ModEvent.EventType type, ulong userID)
        {
            return GetModActionsAsync(type, userID, true);
        }

        [Command("ModLog")]
        [Summary("Get the mod logs of a user by ID")]
        public Task ViewModLogAsync(ulong userId)
        {
            return GetModActionsAsync(null, userId);
        }

        [Command("LongModLog")]
        [Summary("Get all mod logs (optionally filter for a user)")]
        public Task ViewLongModLogAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(null, user?.Id, true);
        }

        [Command("LongModLog")]
        [Summary("Get all mod logs by user ID")]
        public Task ViewLongModLogAsync(ulong userId)
        {
            return GetModActionsAsync(null, userId, true);
        }

        public Task GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType? type, ulong? userID = null, bool showExpired = false)
        {
            IEnumerable<GuildModel.Moderation.ModEvent> modEvents = Context.Server.ModerationSetup.ModActions;

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
                return SimpleEmbedAsync("There are no actions that apply to your specifications");
            }

            foreach (var modGroup in enumerable.ToList().SplitList(5))
            {
                pages.Add(new PaginatedMessage.Page
                              {
                                  Fields = modGroup.Select(m => m.GetLongField(Context.Guild)).ToList()
                              });
            }

            return PagedReplyAsync(new PaginatedMessage
                                       {
                                           Pages = pages, 
                                           Title = userID.HasValue ? $"{Context.Guild.GetUser(userID.Value)}{(type == null ? null : $" {type}")} {enumerable.Count} Actions" : null
                                       }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }
    }
}
