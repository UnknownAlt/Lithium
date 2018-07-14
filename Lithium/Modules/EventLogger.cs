namespace Lithium.Modules
{
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Group("Event")]
    public class EventLogger : Base
    {
        [Command("SetChannel")]
        [Summary("set the current channel for event logging")]
        public Task EventChannelAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.EventChannel = Context.Channel.Id;
            eventConfig.LogEvents = true;
            eventConfig.Save();
            return ReplyAsync($"Success! Events will now be logged in {Context.Channel.Name}");
        }

        [Command("ToggleLog")]
        [Summary("toggle event logging")]
        public Task LogEventToggleAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.LogEvents = !eventConfig.LogEvents;
            eventConfig.Save();
            return ReplyAsync($"EventLogging: {eventConfig.LogEvents}");
        }

        [Command("ChannelCreated")]
        [Summary("toggle ChannelCreated event logging")]
        public Task ChannelCreatedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.ChannelCreated = !eventConfig.Settings.ChannelCreated;
            eventConfig.Save();
            return ReplyAsync($"Channel Created: {eventConfig.Settings.ChannelCreated}");
        }

        [Command("ChannelDeleted")]
        [Summary("toggle ChannelDeleted event logging")]
        public Task ChannelDeletedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.ChannelDeleted = !eventConfig.Settings.ChannelDeleted;
            eventConfig.Save();
            return ReplyAsync($"Channel Deleted: {eventConfig.Settings.ChannelDeleted}");
        }

        [Command("ChannelUpdated")]
        [Summary("toggle ChannelUpdated event logging")]
        public Task ChannelUpdatedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.ChannelUpdated = !eventConfig.Settings.ChannelUpdated;
            eventConfig.Save();
            return ReplyAsync($"Channel Updated: {eventConfig.Settings.ChannelUpdated}");
        }

        [Command("UserJoined")]
        [Summary("toggle UserJoined event logging")]
        public Task UserJoinedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.GuildUserJoined = !eventConfig.Settings.GuildUserJoined;
            eventConfig.Save();
            return ReplyAsync($"User Joined: {eventConfig.Settings.GuildUserJoined}");
        }

        [Command("UserLeft")]
        [Summary("toggle UserLeft event logging")]
        public Task UserLeftAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.GuildUserLeft = !eventConfig.Settings.GuildUserLeft;
            eventConfig.Save();
            return ReplyAsync($"User Left: {eventConfig.Settings.GuildUserLeft}");
        }

        [Command("UserUpdated")]
        [Summary("toggle UserUpdated event logging")]
        public Task UserUpdatedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.GuildMemberUpdated = !eventConfig.Settings.GuildMemberUpdated;
            eventConfig.Save();
            return ReplyAsync($"User Updated: {eventConfig.Settings.GuildMemberUpdated}");
        }

        [Command("MessageDeleted")]
        [Summary("toggle MessageDeleted event logging")]
        public Task MessageDeletedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.MessageDeleted = !eventConfig.Settings.MessageDeleted;
            eventConfig.Save();
            return ReplyAsync($"Message Deleted: {eventConfig.Settings.MessageDeleted}");
        }

        [Command("MessageUpdated")]
        [Summary("toggle MessageUpdated event logging")]
        public Task MessageUpdatedAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            eventConfig.Settings.MessageUpdated = !eventConfig.Settings.MessageUpdated;
            eventConfig.Save();
            return ReplyAsync($"Message Updated: {eventConfig.Settings.MessageUpdated}");
        }

        [Command("ViewConfig")]
        [Summary("View the event logging config")]
        public Task LogEventConfigAsync()
        {
            var eventConfig = EventConfig.Load(Context.Guild.Id);
            var g = eventConfig.Settings;
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