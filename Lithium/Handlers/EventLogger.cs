namespace Lithium.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.WebSocket;

    using Lithium.Discord.Extensions;
    using Lithium.Models;

    public class EventLogger
    {
        private readonly Timer _timer;

        public EventLogger(DiscordShardedClient client)
        {
            _timer = new Timer(_ =>
                                 {
                                     LogHandler.LogMessage("EventLogger Run", LogSeverity.Debug);
                                     foreach (var guild in eventQueue)
                                     {
                                         if (guild.Value.Events.Count(e => e.Value.Type == EventType.messageDeleted) > 20)
                                         {
                                             guild.Value.Events = guild.Value.Events.Where(e => e.Value.Type != EventType.messageDeleted).ToDictionary(k => k.Key, k => k.Value);
                                         }

                                         if (guild.Value.Events.Any())
                                         {
                                             var ordered = guild.Value.Events.OrderBy(x => x.Key).Take(10).ToList();
                                             if (client.GetGuild(guild.Key) is SocketGuild eventGuild)
                                             {
                                                 if (eventGuild.GetTextChannel(guild.Value.EventChannel) is ITextChannel eventChannel)
                                                 {
                                                     var most = GetColor(ordered
                                                         .GroupBy(i => i.Value.Type)
                                                         .OrderByDescending(grp => grp.Count())
                                                         .Select(grp => grp.Key)
                                                         .First());



                                                    var embed = new EmbedBuilder { Fields = ordered.SelectMany(o => o.Value.Fields).ToList(), Color = most };
                                                    eventChannel.SendMessageAsync("", false, embed.Build());
                                                 }
                                             }

                                             foreach (var pair in ordered)
                                             {
                                                 guild.Value.Events.Remove(pair.Key);
                                             }
                                         }
                                     }
                                 },
            null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

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
        
        private readonly Dictionary<ulong, GuildEventInfo> eventQueue = new Dictionary<ulong, GuildEventInfo>();

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

        internal void LogEvent(EventConfig eventConfig, GuildEventInfo.Event _event, IGuild guild)
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

        internal Task GuildMemberUpdatedAsync(SocketGuildUser userBefore, SocketGuildUser userAfter)
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
                return Task.CompletedTask;
            }

            var eventConfig = EventConfig.Load(userAfter.Guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.GuildMemberUpdated)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.guildMemberUpdated, $"**User:** {userAfter.Mention}\n**ID:** {userAfter.Id}\n\n" + logMessage), userAfter.Guild);
            }

            return Task.CompletedTask;
        }

        internal Task MessageUpdatedAsync(Cacheable<IMessage, ulong> messageOld, SocketMessage messageNew, ISocketMessageChannel channel)
        {
            if (messageNew.Author.IsBot)
            {
                return Task.CompletedTask;
            }

            if (string.Equals(messageOld.Value.Content, messageNew.Content, StringComparison.CurrentCultureIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (messageOld.Value?.Embeds.Count > 0 || messageNew.Embeds.Count > 0)
            {
                return Task.CompletedTask;
            }

            var guild = ((SocketGuildChannel)channel).Guild;

            var eventConfig = EventConfig.Load(guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.MessageUpdated)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.messageUpdated, $"**Author:** {messageNew.Author}\n" +
                                                                                $"**Author ID:** {messageNew.Author.Id}\n" +
                                                                                $"**Channel:** {messageNew.Channel.Name}\n" +
                                                                                $"**Embeds:** {messageNew.Embeds.Any()}\n" +
                                                                                $"**OLD:**\n{messageOld.Value.Content}\n\n**NEW:**\n{messageNew.Content}"), guild);
            }

            return Task.CompletedTask;
        }

        internal Task ChannelUpdatedAsync(SocketChannel s1, SocketChannel s2)
        {
            var channelBefore = s1 as SocketGuildChannel;
            var channelAfter = s2 as SocketGuildChannel;
            var eventConfig = EventConfig.Load(channelAfter.Guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.ChannelUpdated)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.channelUpdated, $"**Name:** {channelBefore.Name ?? "[No_Name]"} => {channelAfter.Name ?? "[No_Name]"}\n" + 
                                                                                $"**Position:** {channelBefore.Position} => {channelAfter.Position}\n" + 
                                                                                $"**Permission Changes:** {!Equals(channelBefore.PermissionOverwrites, channelAfter.PermissionOverwrites)}"), channelAfter.Guild);
            }

            return Task.CompletedTask;
        }

        internal Task ChannelDeletedAsync(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var eventConfig = EventConfig.Load(guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.ChannelDeleted)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.channelDeleted, $"{((SocketGuildChannel)sChannel)?.Name ?? "[Unknown_Name]"}"), guild);
            }

            return Task.CompletedTask;
        }

        internal Task ChannelCreatedAsync(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var eventConfig = EventConfig.Load(guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.ChannelCreated)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.channelCreated, $"{((SocketGuildChannel)sChannel)?.Name ?? "[Unknown_Name]"}"), guild);
            }

            return Task.CompletedTask;
        }

        internal Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var guild = ((SocketGuildChannel)channel).Guild;
            var eventConfig = EventConfig.Load(guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.MessageDeleted)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.messageDeleted, $"**Author:** {(message.HasValue ? message.Value.Author.ToString() : "[Unknown Author]")}\n" +
                                                                                $"**Channel:** {channel.Name}\n**Message:**\n{(message.HasValue ? $"{message.Value.Content ?? "[Empty]"}" : "Message unable to be retrieved")}"), guild);
            }

            return Task.CompletedTask;
        }

        internal Task UserUnbannedAsync(SocketUser user, SocketGuild guild)
        {
            var eventConfig = EventConfig.Load(guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.GuildUserUnBanned)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userUnbanned, $"**Username:** {user.Mention} {user} [{user.Id}]"), guild);
            }

            return Task.CompletedTask;
        }

        internal Task UserBannedAsync(SocketUser user, SocketGuild guild)
        {
            var eventConfig = EventConfig.Load(guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.GuildUserBanned)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userBanned, $"**Username:** {user.Mention} {user} [{user.Id}]"), guild);
            }

            return Task.CompletedTask;
        }

        internal Task UserLeftAsync(SocketGuildUser user)
        {
            var eventConfig = EventConfig.Load(user.Guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.GuildUserLeft)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userLeft, $"{user.Mention} {user}\n" +
                                                                          $"ID: {user.Id}"), user.Guild);
            }

            return Task.CompletedTask;
        }

        internal Task UserJoinedAsync(SocketGuildUser user)
        {
            var eventConfig = EventConfig.Load(user.Guild.Id);
            if (eventConfig == null)
            {
                return Task.CompletedTask;
            }

            if (!eventConfig.Settings.GuildUserJoined)
            {
                return Task.CompletedTask;
            }

            if (eventConfig.LogEvents)
            {
                LogEvent(eventConfig, QuickEvent(EventType.userJoined, $"{user.Mention} {user}\n" +
                                                                              $"ID: {user.Id}"), user.Guild);
            }

            return Task.CompletedTask;
        }
    }
}
