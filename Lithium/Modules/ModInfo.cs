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
        [Command("GetAction")]
        public Task ClearWarnsAsync(int actionID)
        {
            var action = Context.Server.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
            if (action == null)
            {
                throw new Exception("No action found with that ID");
            }

            return ReplyAsync(new EmbedBuilder { Title = $"Action ${actionID}", Fields = new List<EmbedFieldBuilder> { action.GetLongField(Context.Guild) } });
        }

        [Command("Bans")]
        public Task ViewBansAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Ban, user?.Id);
        }

        [Command("Bans")]
        public Task ViewBansAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Ban, userId);
        }
                
        [Command("LongBans")]
        public Task ViewLongBansAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Ban, user?.Id, true);
        }

        [Command("LongBans")]
        public Task ViewLongBansAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Ban, userId, true);
        }

        [Command("Kicks")]
        public Task ViewKicksAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user?.Id);
        }

        [Command("Kicks")]
        public Task ViewKicksAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, userId);
        }
        
        [Command("LongKicks")]
        public Task ViewLongKicksAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user?.Id, true);
        }

        [Command("LongKicks")]
        public Task ViewLongKicksAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, userId, true);
        }

        [Command("Warns")]
        public Task ViewWarnsAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Warn, user?.Id);
        }

        [Command("Warns")]
        public Task ViewWarnsAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Warn, userId);
        }
                
        [Command("LongWarns")]
        public Task ViewLongWarnsAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Warn, user?.Id, true);
        }

        [Command("LongWarns")]
        public Task ViewLongWarnsAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Warn, userId, true);
        }

        [Command("Mutes")]
        public Task ViewMutesAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Mute, user?.Id);
        }

        [Command("Mutes")]
        public Task ViewMutesAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Mute, userId);
        }
        
        [Command("LongMutes")]
        public Task ViewLongMutesAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Mute, user?.Id, true);
        }

        [Command("LongMutes")]
        public Task ViewLongMutesAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Mute, userId, true);
        }

        [Command("ModLog")]
        public Task ViewModLogAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(null, user?.Id);
        }

        [Command("ModLog")]
        public Task ViewModLogAsync(ulong userId)
        {
            return GetModActionsAsync(null, userId);
        }

        [Command("LongModLog")]
        public Task ViewLongModLogAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(null, user?.Id, true);
        }

        [Command("LongModLog")]
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
