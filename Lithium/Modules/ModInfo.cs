namespace Lithium.Modules
{
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

    [CustomPermissions(true)]
    public class ModInfo : Base
    {
        [Command("Bans")]
        public Task ViewBansAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.ban, user);
        }

        [Command("Kicks")]
        public Task ViewKicksAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user);
        }

        [Command("Warns")]
        public Task ViewWarnsAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.warn, user);
        }

        [Command("Mutes")]
        public Task ViewMutesAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.mute, user);
        }

        [Command("ModLog")]
        public Task ViewModLogAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(null, user);
        }

        public Task GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType? type, SocketGuildUser user = null)
        {
            List<GuildModel.Moderation.ModEvent> modEvents;
            if (type != null && user == null)
            {
                // Show the given mod action for ALL users
                modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.Action == type).OrderByDescending(x => x.TimeStamp).ToList();
            }
            else if (type == null && user == null)
            {
                // Show ALL Mod actions for ALL users
                modEvents = Context.Server.ModerationSetup.ModActions.OrderByDescending(x => x.TimeStamp).ToList();
            }
            else
            {
                if (type == null)
                {
                    modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.UserId == user.Id).OrderByDescending(x => x.TimeStamp).ToList();
                }
                else
                {
                    modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.UserId == user.Id && x.Action == type).OrderByDescending(x => x.TimeStamp).ToList();
                }
            }

            var pages = new List<PaginatedMessage.Page>();
            foreach (var modGroup in modEvents.SplitList(5))
            {
                pages.Add(new PaginatedMessage.Page
                              {
                                  Fields = modGroup.Select(m => new EmbedFieldBuilder
                                                                    {
                                                                        Name = $"{((Context.Guild.GetUser(m.UserId)?.Nickname ?? Context.Guild.GetUser(m.UserId)?.Username) ?? $"{m.UserName} [{m.UserId}]")} {(m.ExpiredOrRemoved ? "Expired" : null)}",
                                                                        Value = $"**Action:** {m.Action}\n" +
                                                                                $"**Mod:** {Context.Guild.GetUser(m.ModId)?.Mention ?? $"{m.ModName} [{m.ModId}]"}\n" +
                                                                                $"**Expiry:** {(m.ExpiryDate.HasValue ? $"{m.ExpiryDate.Value.ToLongDateString()} {m.ExpiryDate.Value.ToLongTimeString()}" : "Never")}\n" +
                                                                                (m.AutoModReason == GuildModel.Moderation.ModEvent.AutoReason.none ? $"**Reason**: {m.ProvidedReason ?? "N/A"}" : $"**AutoModReason:** {m.AutoModReason}\n{(m.ReasonTrigger == null ? null : $"Trigger ({Context.Guild.GetTextChannel(m.ReasonTrigger.ChannelId)?.Mention ?? m.ReasonTrigger.ChannelId.ToString()}): {m.ReasonTrigger.Message}")}")
                                                                                
                                                                    }).ToList()
                              });
            }

            return PagedReplyAsync(new PaginatedMessage { Pages = pages }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }
    }
}
