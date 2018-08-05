namespace RavenBOT.Core.Bot.Handlers.Events
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.WebSocket;

    using RavenBOT.Extensions;
    using RavenBOT.Models;

    /// <summary>
    /// The event handler.
    /// </summary>
    public partial class EventHandler
    {
        public Color GetColor(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.messageDeleted:
                    return Color.Gold;
                case EventType.messageUpdated:
                    return Color.DarkMagenta;
                case EventType.userBanned:
                    return Color.DarkRed;
                case EventType.guildMemberUpdated:
                    return Color.DarkPurple;
                case EventType.channelCreated:
                case EventType.channelUpdated:
                case EventType.channelDeleted:
                    return Color.Blue;
                case EventType.userUnbanned:
                case EventType.userJoined:
                    return Color.Green;
                case EventType.userLeft:
                    return Color.Red;
                default:
                    return Color.Teal;
            }
        }

        public void Restart()
        {
            _timer.Change(TimeSpan.FromMinutes(0), TimeSpan.FromSeconds(10));
        }
        
        public enum EventType
        {
            [Description("Message Deleted")]
            messageDeleted,
            [Description("Message Updated")]
            messageUpdated,
            [Description("Channel Created")]
            channelCreated,
            [Description("Channel Deleted")]
            channelDeleted,
            [Description("Channel Updated")]
            channelUpdated,
            [Description("Member Updated")]
            guildMemberUpdated,
            [Description("User Left")]
            userLeft,
            [Description("User Joined")]
            userJoined,
            [Description("User Banned")]
            userBanned,
            [Description("User Unbanned")]
            userUnbanned
        }

        public static GuildEventInfo.Event QuickEvent(EventType type, string value)
        {
            return new GuildEventInfo.Event { Type = type, Fields = new List<EmbedFieldBuilder> { new EmbedFieldBuilder { Name = type.GetDescription(), Value = value.FixLength() } } };
        }

        public class GuildEventInfo
        {
            public Dictionary<DateTime, Event> Events { get; set; } = new Dictionary<DateTime, Event>();

            public ulong EventChannel { get; set; }

            public class Event
            {
                public EventType Type { get; set; }

                public List<EmbedFieldBuilder> Fields { get; set; }
            }
        }

        internal void LogEvent(EventServer.EventConfig eventConfig, GuildEventInfo.Event _event, IGuild guild)
        {
            eventQueue.Remove(guild.Id, out var Queue);

            if (Queue == null)
            {
                Queue = new GuildEventInfo
                            {
                                EventChannel = eventConfig.EventChannel
                            };
            }

            Queue.Events.Add(DateTime.UtcNow, _event);
            eventQueue.Add(guild.Id, Queue);
        }

        internal async Task GuildMemberUpdatedAsync(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            string logMessage = null;
            if (userBefore.Nickname != userAfter.Nickname)
            {
                logMessage += "__**NickName Updated**__\n" +
                          $"OLD: {userBefore.Nickname ?? userBefore.Username}\n" +
                          $"AFTER: {userAfter.Nickname ?? userAfter.Username}\n";
            }

            if (userBefore.Roles.Count < userAfter.Roles.Count)
            {
                var result = userAfter.Roles.Where(b => userBefore.Roles.All(a => b.Id != a.Id)).ToList();
                logMessage += "__**Role Added**__\n" +
                          $"{result[0].Name}\n";
            }
            else if (userBefore.Roles.Count > userAfter.Roles.Count)
            {
                var result = userBefore.Roles.Where(b => userAfter.Roles.All(a => b.Id != a.Id)).ToList();
                logMessage += "__**Role Removed**__\n" +
                          $"{result[0].Name}\n";
            }

            if (logMessage == null)
            {
                return;
            }

            var eventConfig = await Events.LoadAsync(userAfter.Guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.GuildMemberUpdated)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.guildMemberUpdated, $"**User:** {userAfter.Mention}\n**ID:** {userAfter.Id}\n\n" + logMessage), userAfter.Guild);
            }
        }

        internal async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> messageOld, SocketMessage messageNew, ISocketMessageChannel channel)
        {
            if (messageNew.Author.IsBot)
            {
                return;
            }

            if (string.Equals(messageOld.Value.Content, messageNew.Content, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            if (messageOld.Value?.Embeds.Count > 0 || messageNew.Embeds.Count > 0)
            {
                return;
            }

            var guild = ((SocketGuildChannel)channel).Guild;

            var eventConfig = await Events.LoadAsync(guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.MessageUpdated)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.messageUpdated, $"**Author:** {messageNew.Author}\n" +
                                                                                $"**Author ID:** {messageNew.Author.Id}\n" +
                                                                                $"**Channel:** {messageNew.Channel.Name}\n" +
                                                                                $"**Embeds:** {messageNew.Embeds.Any()}\n" +
                                                                                $"**OLD:**\n{messageOld.Value.Content}\n\n**NEW:**\n{messageNew.Content}"), guild);
            }
        }

        internal async Task ChannelUpdatedAsync(SocketChannel s1, SocketChannel s2)
        {
            var channelBefore = s1 as SocketGuildChannel;
            var channelAfter = s2 as SocketGuildChannel;
            var eventConfig = await Events.LoadAsync(channelAfter.Guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.ChannelUpdated)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.channelUpdated, $"**Name:** {channelBefore.Name ?? "[No_Name]"} => {channelAfter.Name ?? "[No_Name]"}\n" + 
                                                                                $"**Position:** {channelBefore.Position} => {channelAfter.Position}\n" + 
                                                                                $"**Permission Changes:** {!Equals(channelBefore.PermissionOverwrites, channelAfter.PermissionOverwrites)}"), channelAfter.Guild);
            }
        }

        internal async Task ChannelDeletedAsync(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var eventConfig = await Events.LoadAsync(guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.ChannelDeleted)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.channelDeleted, $"{((SocketGuildChannel)sChannel)?.Name ?? "[Unknown_Name]"}"), guild);
            }
        }

        internal async Task ChannelCreatedAsync(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var eventConfig = await Events.LoadAsync(guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.ChannelCreated)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.channelCreated, $"{((SocketGuildChannel)sChannel)?.Name ?? "[Unknown_Name]"}"), guild);
            }
        }

        internal async Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var guild = ((SocketGuildChannel)channel).Guild;
            var eventConfig = await Events.LoadAsync(guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.MessageDeleted)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.messageDeleted, $"**Author:** {(message.HasValue ? message.Value.Author.ToString() : "[Unknown Author]")}\n" +
                                                                                $"**Channel:** {channel.Name}\n**Message:**\n{(message.HasValue ? $"{message.Value.Content ?? "[Empty]"}" : "Message unable to be retrieved")}"), guild);
            }
        }

        internal async Task UserUnbannedAsync(SocketUser user, SocketGuild guild)
        {
            var eventConfig = await Events.LoadAsync(guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.GuildUserUnBanned)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userUnbanned, $"**Username:** {user.Mention} {user} [{user.Id}]"), guild);
            }
        }

        internal async Task UserBannedAsync(SocketUser user, SocketGuild guild)
        {
            var eventConfig = await Events.LoadAsync(guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.GuildUserBanned)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userBanned, $"**Username:** {user.Mention} {user} [{user.Id}]"), guild);
            }
        }

        internal async Task UserLeftAsync(SocketGuildUser user)
        {
            var eventConfig = await Events.LoadAsync(user.Guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.GuildUserLeft)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userLeft, $"{user.Mention} {user}\n" +
                                                                          $"ID: {user.Id}"), user.Guild);
            }
        }

        internal async Task UserJoinedAsync(SocketGuildUser user)
        {
            var eventConfig = await Events.LoadAsync(user.Guild.Id);
            if (eventConfig == null)
            {
                return;
            }

            if (!eventConfig.Settings.GuildUserJoined)
            {
                return;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userJoined, $"{user.Mention} {user}\n" +
                                                                              $"ID: {user.Id}"), user.Guild);
            }
        }
    }
}
