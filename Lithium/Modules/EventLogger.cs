namespace Lithium.Modules
{
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Group("Event")]
    public class EventLogger : Base
    {
        [Command("SetChannel")]
        [Summary("Event SetChannel")]
        [Remarks("set the current channel for event logging")]
        public Task EventChannelAsync()
        {
            Context.Server.EventLogger.EventChannel = Context.Channel.Id;
            Context.Server.EventLogger.LogEvents = true;
            Context.Server.Save();
            return ReplyAsync($"Success! Events will now be logged in {Context.Channel.Name}");
        }

        [Command("ToggleLog")]
        [Summary("Event ToggleLog")]
        [Remarks("toggle event logging")]
        public Task LogEventToggleAsync()
        {
            Context.Server.EventLogger.LogEvents = !Context.Server.EventLogger.LogEvents;
            Context.Server.Save();
            return ReplyAsync($"EventLogging: {Context.Server.EventLogger.LogEvents}");
        }

        [Command("ChannelCreated")]
        [Summary("Event ChannelCreated")]
        [Remarks("toggle ChannelCreated event logging")]
        public Task ChannelCreatedAsync()
        {
            Context.Server.EventLogger.Settings.ChannelCreated = !Context.Server.EventLogger.Settings.ChannelCreated;
            Context.Server.Save();
            return ReplyAsync($"Channel Created: {Context.Server.EventLogger.Settings.ChannelCreated}");
        }

        [Command("ChannelDeleted")]
        [Summary("Event ChannelDeleted")]
        [Remarks("toggle ChannelDeleted event logging")]
        public Task ChannelDeletedAsync()
        {
            Context.Server.EventLogger.Settings.ChannelDeleted = !Context.Server.EventLogger.Settings.ChannelDeleted;
            Context.Server.Save();
            return ReplyAsync($"Channel Deleted: {Context.Server.EventLogger.Settings.ChannelDeleted}");
        }

        [Command("ChannelUpdated")]
        [Summary("Event ChannelUpdated")]
        [Remarks("toggle ChannelUpdated event logging")]
        public Task ChannelUpdatedAsync()
        {
            Context.Server.EventLogger.Settings.ChannelUpdated = !Context.Server.EventLogger.Settings.ChannelUpdated;
            Context.Server.Save();
            return ReplyAsync($"Channel Updated: {Context.Server.EventLogger.Settings.ChannelUpdated}");
        }

        [Command("UserJoined")]
        [Summary("Event UserJoined")]
        [Remarks("toggle UserJoined event logging")]
        public Task UserJoinedAsync()
        {
            Context.Server.EventLogger.Settings.GuildUserJoined = !Context.Server.EventLogger.Settings.GuildUserJoined;
            Context.Server.Save();
            return ReplyAsync($"User Joined: {Context.Server.EventLogger.Settings.GuildUserJoined}");
        }

        [Command("UserLeft")]
        [Summary("Event UserLeft")]
        [Remarks("toggle UserLeft event logging")]
        public Task UserLeftAsync()
        {
            Context.Server.EventLogger.Settings.GuildUserLeft = !Context.Server.EventLogger.Settings.GuildUserLeft;
            Context.Server.Save();
            return ReplyAsync($"User Left: {Context.Server.EventLogger.Settings.GuildUserLeft}");
        }

        [Command("UserUpdated")]
        [Summary("Event UserUpdated")]
        [Remarks("toggle UserUpdated event logging")]
        public Task UserUpdatedAsync()
        {
            Context.Server.EventLogger.Settings.GuildMemberUpdated = !Context.Server.EventLogger.Settings.GuildMemberUpdated;
            Context.Server.Save();
            return ReplyAsync($"User Updated: {Context.Server.EventLogger.Settings.GuildMemberUpdated}");
        }

        [Command("MessageDeleted")]
        [Summary("Event MessageDeleted")]
        [Remarks("toggle MessageDeleted event logging")]
        public Task MessageDeletedAsync()
        {
            Context.Server.EventLogger.Settings.MessageDeleted = !Context.Server.EventLogger.Settings.MessageDeleted;
            Context.Server.Save();
            return ReplyAsync($"Message Deleted: {Context.Server.EventLogger.Settings.MessageDeleted}");
        }

        [Command("MessageUpdated")]
        [Summary("Event MessageUpdated")]
        [Remarks("toggle MessageUpdated event logging")]
        public Task MessageUpdatedAsync()
        {
            Context.Server.EventLogger.Settings.MessageUpdated = !Context.Server.EventLogger.Settings.MessageUpdated;
            Context.Server.Save();
            return ReplyAsync($"Message Updated: {Context.Server.EventLogger.Settings.MessageUpdated}");
        }

        [Command("ViewConfig")]
        [Summary("Event ViewConfig")]
        [Remarks("View the event logging config")]
        public Task LogEventConfigAsync()
        {
            var g = Context.Server.EventLogger.Settings;
            var embed = new EmbedBuilder
            {
                Description = $"User Updated: {g.GuildMemberUpdated}\n" +
                              $"User Joined: {g.GuildUserJoined}\n" +
                              $"User Left: {g.GuildUserLeft}\n" +
                              $"User Banned: {g.GuildUserBanned}\n" +
                              $"User UnBanned: {g.GuildUserUnBanned}\n" +
                              $"Channel Created: {g.ChannelCreated}\n" +
                              $"Channel Deleted: {g.ChannelDeleted}\n" +
                              $"Channel Updated: {g.ChannelUpdated}\n" +
                              $"Message Updated: {g.MessageUpdated}\n" +
                              $"Message Deleted: {g.MessageDeleted}",
                Color = Color.Blue,
                Title = "Event Config"
            };
            return ReplyAsync(embed);
        }
    }
}