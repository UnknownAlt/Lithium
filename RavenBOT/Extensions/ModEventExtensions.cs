namespace RavenBOT.Extensions
{
    using Discord;
    using Discord.WebSocket;

    using RavenBOT.Models;

    public static class ModEventExtensions
    {
        public static EmbedFieldBuilder GetShortField(this GuildService.GuildModel.Moderation.ModEvent modEvent)
        {
            return new EmbedFieldBuilder
                {
                    Name =
                        $"{modEvent.UserName} [{modEvent.UserId}] was {modEvent.Action.GetDescription()} [#{modEvent.ActionId}] {(modEvent.ExpiredOrRemoved ? "Expired" : null)}",
                    Value = $"**Mod:** {modEvent.ModName} [{modEvent.ModId}]\n"
                            + $"**Expires:** {(modEvent.ExpiryDate.HasValue ? $"{modEvent.ExpiryDate.Value.ToLongDateString()} {modEvent.ExpiryDate.Value.ToLongTimeString()}\n" : "Never\n")}"
                            + (modEvent.AutoModReason
                               == GuildService.GuildModel.Moderation.ModEvent.AutoReason.none
                                   ? $"**Reason:** {modEvent.ProvidedReason ?? "N/A"}\n"
                                   : $"**Auto-Reason:** {modEvent.AutoModReason}")
                            .FixLength()
                };
        }

        /*
        public static EmbedFieldBuilder GetLongField(this GuildModel.Moderation.ModEvent modEvent)
        {
            return new EmbedFieldBuilder
                       {
                           Name =
                               $"{modEvent.UserName} [{modEvent.UserId}] was {modEvent.Action.GetDescription()} [#{modEvent.ActionId}]",
                           Value = $"**Mod:** {modEvent.ModName} [{modEvent.ModId}]\n"
                                   + $"**Expires:** {(modEvent.ExpiryDate.HasValue ? $"{modEvent.ExpiryDate.Value.ToLongDateString()} {modEvent.ExpiryDate.Value.ToLongTimeString()}\n" : "Never\n")}"
                                   + (modEvent.AutoModReason
                                      == GuildModel.Moderation.ModEvent.AutoReason.none
                                          ? $"**Reason:** {modEvent.ProvidedReason ?? "N/A"}\n"
                                          : $"**Auto-Reason:** {modEvent.AutoModReason}\n**Trigger: ({modEvent.ReasonTrigger?.ChannelId ?? 0})**\n{modEvent.ReasonTrigger?.Message ?? "N/A"}\n")
                                   .FixLength()
                       };
        }
        */

        public static EmbedFieldBuilder GetLongField(this GuildService.GuildModel.Moderation.ModEvent modEvent, SocketGuild guild)
        {
            return new EmbedFieldBuilder
                       {
                           Name =
                               $"{guild.GetUser(modEvent.UserId)?.ToString() ?? $"{modEvent.UserName} [{modEvent.UserId}]"} was {modEvent.Action.GetDescription()} [#{modEvent.ActionId}] {(modEvent.ExpiredOrRemoved ? "Expired" : null)}",
                           Value = $"**Mod:** {guild.GetUser(modEvent.ModId)?.Mention ?? $"{modEvent.ModName} [{modEvent.ModId}]"}\n"
                                   + $"**Expires:** {(modEvent.ExpiryDate.HasValue ? $"{modEvent.ExpiryDate.Value.ToLongDateString()} {modEvent.ExpiryDate.Value.ToLongTimeString()}\n" : "Never\n")}"
                                   + (modEvent.AutoModReason
                                      == GuildService.GuildModel.Moderation.ModEvent.AutoReason.none
                                          ? $"**Reason:** {modEvent.ProvidedReason?.FixLength(256) ?? "N/A"}\n"
                                          : $"**Auto-Reason:** {modEvent.AutoModReason}\n**Trigger: ({guild.GetTextChannel(modEvent.ReasonTrigger?.ChannelId ?? 0)?.Mention ?? "0"})**\n{modEvent.ReasonTrigger?.Message?.FixLength(256) ?? "N/A"}\n")
                                   .FixLength()
                       };
        }
    }
}
