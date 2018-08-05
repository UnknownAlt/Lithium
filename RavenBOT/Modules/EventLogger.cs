namespace RavenBOT.Modules
{
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Group("Event")]
    public class EventLogger : Base
    {
        private readonly EventServer eventService;

        public EventLogger(EventServer server)
        {
            eventService = server;
        }

        [Command("SetChannel")]
        [Summary("set the current channel for event logging")]
        public async Task EventChannelAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.EventChannel = Context.Channel.Id;
            eventConfig.LogEvents = true;
            await eventConfig.SaveAsync();
            await ReplyAsync($"Success! Events will now be logged in {Context.Channel.Name}");
        }

        [Command("ToggleLog")]
        [Summary("toggle event logging")]
        public async Task LogEventToggleAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.LogEvents = !eventConfig.LogEvents;
            await eventConfig.SaveAsync();
            await ReplyAsync($"EventLogging: {eventConfig.LogEvents}");
        }

        [Command("ChannelCreated")]
        [Summary("toggle ChannelCreated event logging")]
        public async Task ChannelCreatedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.ChannelCreated = !eventConfig.Settings.ChannelCreated;
            await eventConfig.SaveAsync();
            await ReplyAsync($"Channel Created: {eventConfig.Settings.ChannelCreated}");
        }

        [Command("ChannelDeleted")]
        [Summary("toggle ChannelDeleted event logging")]
        public async Task ChannelDeletedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.ChannelDeleted = !eventConfig.Settings.ChannelDeleted;
            await eventConfig.SaveAsync();
            await ReplyAsync($"Channel Deleted: {eventConfig.Settings.ChannelDeleted}");
        }

        [Command("ChannelUpdated")]
        [Summary("toggle ChannelUpdated event logging")]
        public async Task ChannelUpdatedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.ChannelUpdated = !eventConfig.Settings.ChannelUpdated;
            await eventConfig.SaveAsync();
            await ReplyAsync($"Channel Updated: {eventConfig.Settings.ChannelUpdated}");
        }

        [Command("UserJoined")]
        [Summary("toggle UserJoined event logging")]
        public async Task UserJoinedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.GuildUserJoined = !eventConfig.Settings.GuildUserJoined;
            await eventConfig.SaveAsync();
            await ReplyAsync($"User Joined: {eventConfig.Settings.GuildUserJoined}");
        }

        [Command("UserLeft")]
        [Summary("toggle UserLeft event logging")]
        public async Task UserLeftAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.GuildUserLeft = !eventConfig.Settings.GuildUserLeft;
            await eventConfig.SaveAsync();
            await ReplyAsync($"User Left: {eventConfig.Settings.GuildUserLeft}");
        }

        [Command("UserUpdated")]
        [Summary("toggle UserUpdated event logging")]
        public async Task UserUpdatedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.GuildMemberUpdated = !eventConfig.Settings.GuildMemberUpdated;
            await eventConfig.SaveAsync();
            await ReplyAsync($"User Updated: {eventConfig.Settings.GuildMemberUpdated}");
        }

        [Command("MessageDeleted")]
        [Summary("toggle MessageDeleted event logging")]
        public async Task MessageDeletedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.MessageDeleted = !eventConfig.Settings.MessageDeleted;
            await eventConfig.SaveAsync();
            await ReplyAsync($"Message Deleted: {eventConfig.Settings.MessageDeleted}");
        }

        [Command("MessageUpdated")]
        [Summary("toggle MessageUpdated event logging")]
        public async Task MessageUpdatedAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
            eventConfig.Settings.MessageUpdated = !eventConfig.Settings.MessageUpdated;
            await eventConfig.SaveAsync();
            await ReplyAsync($"Message Updated: {eventConfig.Settings.MessageUpdated}");
        }

        [Command("ViewConfig")]
        [Summary("View the event logging config")]
        public async Task LogEventConfigAsync()
        {
            var eventConfig = await eventService.LoadAsync(Context.Guild.Id);
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
            await ReplyAsync(embed);
        }
    }
}