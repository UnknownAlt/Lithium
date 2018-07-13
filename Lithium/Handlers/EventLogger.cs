namespace Lithium.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.WebSocket;

    using Lithium.Models;

    public class EventLogger
    {
        public List<EventLogDelay> EventLogDelays { get; set; } = new List<EventLogDelay>();

        public class EventLogDelay
        {
            public ulong GuildID { get; set; }

            public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

            public int Updates { get; set; }
        }

        internal async Task LogEventAsync(EventConfig eventConfig, IGuild guild, EmbedBuilder embed)
        {
            if (EventLogDelays.All(x => x.GuildID != guild.Id))
            {
                EventLogDelays.Add(new EventLogDelay
                {
                    GuildID = guild.Id,
                    LastUpdate = DateTime.UtcNow,
                    Updates = 0
                });
            }
            else
            {
                var delay = EventLogDelays.First(x => x.GuildID == guild.Id);

                // Ensure that we are only logging 1 event per second (to reduce lag from the bot and overall spam)
                if (delay.LastUpdate + TimeSpan.FromSeconds(5) >= DateTime.UtcNow)
                {
                    delay.Updates++;
                }
                else
                {
                    delay.LastUpdate = DateTime.UtcNow;
                    delay.Updates = 0;
                }

                if (delay.Updates >= 3 && delay.LastUpdate + TimeSpan.FromSeconds(5) > DateTime.UtcNow)
                {
                    LogHandler.LogMessage($"RateLimiting Events in {guild.Name}", LogSeverity.Verbose);
                    return;
                }

                if ((await guild.GetTextChannelAsync(eventConfig.EventChannel)) is ITextChannel LogChannel && eventConfig.LogEvents)
                {
                    try
                    {
                        await LogChannel.SendMessageAsync("", false, embed.Build());
                    }
                    catch (Exception e)
                    {
                        LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }
            }
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

            var eventConfig = EventConfig.Load(userAfter.Guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "User Updated",
                    Description = $"**User:** {userAfter.Mention}\n" +
                                  $"**ID:** {userAfter.Id}\n\n" + logMessage,
                    ThumbnailUrl = userAfter.GetAvatarUrl(),
                    Color = Color.Blue
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, userAfter.Guild, embed);
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

            var eventConfig = EventConfig.Load(guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "Message Updated",
                    ThumbnailUrl = messageNew.Author.GetAvatarUrl(),
                    Color = Color.Blue
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                embed.AddField("Old Message:", $"{messageOld.Value.Content}");
                embed.AddField("New Message:", $"{messageNew.Content}");
                embed.AddField("Info",
                    $"**Author:** {messageNew.Author}\n" +
                    $"**Author ID:** {messageNew.Author.Id}\n" +
                    $"**Channel:** {messageNew.Channel.Name}\n" +
                    $"**Embeds:** {messageNew.Embeds.Any()}");
                
                await LogEventAsync(eventConfig, guild, embed);
            }
        }

        internal async Task ChannelUpdatedAsync(SocketChannel s1, SocketChannel s2)
        {
            var channelBefore = s1 as SocketGuildChannel;
            var channelAfter = s2 as SocketGuildChannel;
            var eventConfig = EventConfig.Load(channelAfter.Guild.Id);
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
                if (channelBefore.Position != channelAfter.Position)
                {
                    return;
                }

                var embed = new EmbedBuilder
                {
                    Title = "Channel Updated",
                    Description = channelAfter.Name,
                    Color = Color.Blue
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, channelAfter.Guild, embed);
            }
        }

        internal async Task ChannelDeletedAsync(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var eventConfig = EventConfig.Load(guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "Channel Deleted",
                    Description = ((SocketGuildChannel)sChannel)?.Name,
                    Color = Color.DarkTeal
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, guild, embed);
            }
        }

        internal async Task ChannelCreatedAsync(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var eventConfig = EventConfig.Load(guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "Channel Created",
                    Description = ((SocketGuildChannel)sChannel)?.Name,
                    Color = Color.Green
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, guild, embed);
            }
        }

        internal async Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var guild = ((SocketGuildChannel)channel).Guild;
            var eventConfig = EventConfig.Load(guild.Id);
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
                var embed = new EmbedBuilder();

                embed.AddField("Message Deleted", $"Author: {message.Value.Author}\n" +
                                                  $"Channel: {channel.Name}");
                embed.AddField("Message", $"{(message.HasValue ? $"{message.Value.Content}" : "Message unable to be retrieved")}");

                /*
                if (guild.CurrentUser.GuildPermissions.ViewAuditLog)
                {
                    var logs = await guild.GetAuditLogsAsync(100).FlattenAsync();
                    var logMatch = logs.Where(a => a.Action == ActionType.MessageDeleted && (a.Data as MessageDeleteAuditLogData)?.ChannelId == channel.Id && a.User.Id != message.Value?.Author.Id);
                    if (logMatch.FirstOrDefault() != null)
                    {
                        embed.AddField($"Message deleted by", logMatch.User.Mention);
                    }
                }
                */

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                embed.Color = Color.DarkTeal;

                await LogEventAsync(eventConfig, guild, embed);
            }
        }

        internal async Task UserUnbannedAsync(SocketUser user, SocketGuild guild)
        {
            var eventConfig = EventConfig.Load(guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "User UnBanned",
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Description = $"**Username:** {user}",
                    Color = Color.DarkTeal
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, guild, embed);
            }
        }

        internal async Task UserBannedAsync(SocketUser user, SocketGuild guild)
        {
            var eventConfig = EventConfig.Load(guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "User Banned",
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Description = $"**Username:** {user}",
                    Color = Color.DarkRed
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, guild, embed);
            }
        }

        internal async Task UserLeftAsync(SocketGuildUser user)
        {
            var eventConfig = EventConfig.Load(user.Guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "User Left",
                    Description = $"{user.Mention} {user}\n" +
                                  $"ID: {user.Id}",
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = Color.Red
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, user.Guild, embed);
            }
        }

        internal async Task UserJoinedAsync(SocketGuildUser user)
        {
            var eventConfig = EventConfig.Load(user.Guild.Id);
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
                var embed = new EmbedBuilder
                {
                    Title = "User Joined",
                    Description = $"{user.Mention} {user}\n" +
                                  $"ID: {user.Id}",
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = Color.Green
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);
                await LogEventAsync(eventConfig, user.Guild, embed);
            }
        }
    }
}
