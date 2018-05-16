using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts;
using Lithium.Models;
using Lithium.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Lithium.Handlers
{
    public class EventHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly TimerService _timerservice;
        private readonly List<NoSpamGuild> NoSpam = new List<NoSpamGuild>();
        private readonly Perspective.Api ToxicityAPI;
        public List<Delays> AntiSpamMsgDelays = new List<Delays>();

        public IServiceProvider Provider;

        public EventHandler(IServiceProvider provider)
        {
            Provider = provider;
            _client = Provider.GetService<DiscordSocketClient>();
            _commands = new CommandService();
            _timerservice = new TimerService(_client);
            ToxicityAPI = new Perspective.Api(Config.Load().ToxicityToken);

            _client.MessageReceived += DoCommand;
            _client.JoinedGuild += _client_JoinedGuild;
            _client.Ready += _client_Ready;


            //Guild Event Logging
            //User
            _client.UserJoined += _client_UserJoined;
            _client.UserLeft += _client_UserLeft;
            _client.UserBanned += _client_UserBanned;
            _client.UserUnbanned += _client_UserUnbanned;
            _client.GuildMemberUpdated += _client_GuildMemberUpdated;
            //Message
            _client.MessageUpdated += _client_MessageUpdated;
            _client.MessageDeleted += _client_MessageDeleted;
            //Channel
            _client.ChannelCreated += _client_ChannelCreated;
            _client.ChannelDestroyed += _client_ChannelDestroyed;
            _client.ChannelUpdated += _client_ChannelUpdated;
            
            
        }

        #region EventChannelLogging
        private async Task _client_GuildMemberUpdated(SocketGuildUser UserBefore, SocketGuildUser UserAfter)
        {
            var logmsg = "";
            if (UserBefore.Nickname != UserAfter.Nickname)
            { 
                logmsg += "__**NickName Updated**__\n" +
                          $"OLD: {UserBefore.Nickname ?? UserBefore.Username}\n" +
                          $"AFTER: {UserAfter.Nickname ?? UserAfter.Username}\n";
            }

            if (UserBefore.Roles.Count < UserAfter.Roles.Count)
            {
                var result = UserAfter.Roles.Where(b => UserBefore.Roles.All(a => b.Id != a.Id)).ToList();
                logmsg += "__**Role Added**__\n" +
                          $"{result[0].Name}\n";
            }
            else if (UserBefore.Roles.Count > UserAfter.Roles.Count)
            {
                var result = UserBefore.Roles.Where(b => UserAfter.Roles.All(a => b.Id != a.Id)).ToList();
                logmsg += "__**Role Removed**__\n" +
                          $"{result[0].Name}\n";
            }

            if (logmsg == "") return;
            var GuildConfig = DatabaseHandler.GetGuild(UserAfter.Guild.Id);
            if (GuildConfig.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "User Updated",
                    Description = $"**User:** {UserAfter.Mention}\n" +
                                  $"**ID:** {UserAfter.Id}\n\n" + logmsg,
                    ThumbnailUrl = UserAfter.GetAvatarUrl(),
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC"
                    },
                    Color = Color.Blue
                };
                await GuildConfig.EventLog(embed, UserAfter.Guild);
            }
        }

        private async Task _client_MessageUpdated(Cacheable<IMessage, ulong> messageOld, SocketMessage messageNew, ISocketMessageChannel cchannel)
        {
            if (messageNew.Author.IsBot)
                return;

            if (string.Equals(messageOld.Value.Content, messageNew.Content, StringComparison.CurrentCultureIgnoreCase))
                return;

            if (messageOld.Value?.Embeds.Count > 0 || messageNew.Embeds.Count > 0)
                return;

            var guild = ((SocketGuildChannel)cchannel).Guild;

            var guildobj = DatabaseHandler.GetGuild(guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "Message Updated",
                    ThumbnailUrl = messageNew.Author.GetAvatarUrl(),
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"
                    },
                    Color = Color.Blue
                };
                embed.AddField("Old Message:", $"{messageOld.Value.Content}");
                embed.AddField("New Message:", $"{messageNew.Content}");
                embed.AddField("Info",
                    $"**Author:** {messageNew.Author.Username}\n" +
                    $"**Author ID:** {messageNew.Author.Id}\n" +
                    $"**Channel:** {messageNew.Channel.Name}\n" +
                    $"**Embeds:** {messageNew.Embeds.Any()}");

                await guildobj.EventLog(embed, guild);
            }
        }

        private async Task _client_ChannelUpdated(SocketChannel s1, SocketChannel s2)
        {
            var ChannelBefore = s1 as SocketGuildChannel;
            var ChannelAfter = s2 as SocketGuildChannel;
            var guildobj = DatabaseHandler.GetGuild(ChannelAfter.Guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                if (ChannelBefore.Position != ChannelAfter.Position)
                    return;
                var embed = new EmbedBuilder
                {
                    Title = "Channel Updated",
                    Description = ChannelAfter.Name,
                    Color = Color.Blue,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"
                    }
                };
                await guildobj.EventLog(embed, ChannelAfter.Guild);
            }
        }

        private async Task _client_ChannelDestroyed(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var guildobj = DatabaseHandler.GetGuild(guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "Channel Deleted",
                    Description = ((SocketGuildChannel)sChannel)?.Name,
                    Color = Color.DarkTeal,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"
                    }
                };
                await guildobj.EventLog(embed, guild);
            }
        }

        private async Task _client_ChannelCreated(SocketChannel sChannel)
        {
            var guild = ((SocketGuildChannel)sChannel).Guild;
            var guildobj = DatabaseHandler.GetGuild(guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "Channel Created",
                    Description = ((SocketGuildChannel)sChannel)?.Name,
                    Color = Color.Green,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"
                    }
                };
                await guildobj.EventLog(embed, guild);
            }
        }

        private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var guild = ((SocketGuildChannel)channel).Guild;
            var guildobj = DatabaseHandler.GetGuild(guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder();
                try
                {
                    embed.AddField("Message Deleted", $"Message: {message.Value.Content}\n" +
                                                      $"Author: {message.Value.Author}\n" +
                                                      $"Channel: {channel.Name}");
                }
                catch
                {
                    embed.AddField("Message Deleted", "Message was unable to be retrieved\n" +
                                                      $"Channel: {channel.Name}");
                }

                embed.WithFooter(x => { x.WithText($"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"); });
                embed.Color = Color.DarkTeal;

                await guildobj.EventLog(embed, guild);
            }
        }

        private async Task _client_UserUnbanned(SocketUser User, SocketGuild Guild)
        {
            var guildobj = DatabaseHandler.GetGuild(Guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "User UnBanned",
                    ThumbnailUrl = User.GetAvatarUrl(),
                    Description = $"**Username:** {User.Username}",
                    Color = Color.DarkTeal,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"
                    }
                };
                await guildobj.EventLog(embed, Guild);
            }
        }

        private async Task _client_UserBanned(SocketUser User, SocketGuild Guild)
        {
            var guildobj = DatabaseHandler.GetGuild(Guild.Id);
            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "User Banned",
                    ThumbnailUrl = User.GetAvatarUrl(),
                    Description = $"**Username:** {User.Username}",
                    Color = Color.DarkRed,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC TIME"
                    }
                };
                await guildobj.EventLog(embed, Guild);
            }
        }

        private async Task _client_UserLeft(SocketGuildUser user)
        {
            var guildobj = DatabaseHandler.GetGuild(user.Guild.Id);

            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "User Left",
                    Description = $"{user.Mention} {user.Username}#{user.Discriminator}\n" +
                                  $"ID: {user.Id}",
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = Color.Red,
                    Footer = new EmbedFooterBuilder
                        { Text = $"{DateTime.UtcNow} UTC TIME" }
                };
                await guildobj.EventLog(embed, user.Guild);
            }
        }

        private async Task _client_UserJoined(SocketGuildUser user)
        {
            var guildobj = DatabaseHandler.GetGuild(user.Guild.Id);

            if (guildobj.EventLogger.LogEvents)
            {
                var embed = new EmbedBuilder
                {
                    Title = "User Joined",
                    Description = $"{user.Mention} {user.Username}#{user.Discriminator}\n" +
                                  $"ID: {user.Id}",
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = Color.Green,
                    Footer = new EmbedFooterBuilder
                        { Text = $"{DateTime.UtcNow} UTC TIME" }
                };
                await guildobj.EventLog(embed, user.Guild);
            }
        }
        #endregion


        private async Task _client_Ready()
        {
            try
            {
                DatabaseHandler.DatabaseInitialise(_client);
                Log.Information("Database Initialised");
                var application = await _client.GetApplicationInfoAsync();
                Log.Information($"Invite: https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=2146958591");
                var dblist = DatabaseHandler.GetFullConfig();
                foreach (var guild in _client.Guilds.Where(g => dblist.All(x => x.GuildID != g.Id)))
                {
                    DatabaseHandler.AddGuild(guild.Id);
                }

                _timerservice.Restart();
            }
            catch (Exception e)
            {
                Logger.LogMessage(e.ToString(), LogSeverity.Error);
            }

            await _client.SetGameAsync($"{Config.Load().DefaultPrefix}help // {_client.Guilds.Sum(x => x.MemberCount)} Users!");
        }

        private async Task _client_JoinedGuild(SocketGuild guild)
        {
            try
            {
                var dblist = DatabaseHandler.GetFullConfig();
                if (dblist.All(x => x.GuildID != guild.Id))
                {
                    foreach (var missingguild in _client.Guilds.Where(g => dblist.All(x => x.GuildID != g.Id)))
                    {
                        DatabaseHandler.AddGuild(missingguild.Id);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage("Joined Guild Setup Error", LogSeverity.Error);
                Logger.LogMessage(e.ToString(), LogSeverity.Error);
            }

            try
            {
                //Ensure that we notify new servers how to use the bot by telling them how to get use th ehelp command.
                var embed = new EmbedBuilder
                {
                    Title = guild.CurrentUser.Username,
                    Description = $"Hi there, I am {guild.CurrentUser.Username}. Type `{Config.Load().DefaultPrefix}help` to see a list of my commands",
                    Color = Color.Blue
                };
                //await guild.DefaultChannel?.SendMessageAsync("", false, embed.Build());
                var defaultchannel = guild.TextChannels?.FirstOrDefault(x => string.Equals(x.Name, "general", StringComparison.CurrentCultureIgnoreCase));
                if (defaultchannel != null)
                {
                    try
                    {
                        await defaultchannel.SendMessageAsync("", false, embed.Build());
                    }
                    catch (Exception e)
                    {
                        Logger.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage("Joined Guild Notify Error", LogSeverity.Error);
                Logger.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        public bool CheckHidden(LithiumContext context)
        {
            if (context.Guild == null) return false;
            var guild = context.Server;
            if (guild.Settings.DisabledParts.BlacklistedCommands.Any() || guild.Settings.DisabledParts.BlacklistedModules.Any())
            {
                CommandInfo CMDCheck = null;
                var argPos = 0;
                var cmdSearch = _commands.Search(context, argPos);
                if (cmdSearch.IsSuccess)
                {
                    CMDCheck = cmdSearch.Commands.FirstOrDefault().Command;
                }

                if (CMDCheck != null)
                {
                    var guser = (IGuildUser) context.User;
                    if (!guser.GuildPermissions.Administrator && !guild.ModerationSetup.AdminRoles.Any(x => guser.RoleIds.Contains(x)))
                    {
                        if (guild.Settings.DisabledParts.BlacklistedCommands.Any(x => string.Equals(x, CMDCheck.Name, StringComparison.CurrentCultureIgnoreCase)) ||
                            guild.Settings.DisabledParts.BlacklistedModules.Any(x => string.Equals(x, CMDCheck.Module.Name, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public async Task<bool> antispam(LithiumContext context)
        {
            if (context.Guild == null) return false;
            if (context.Channel is IDMChannel) return false;
            if (context.Server == null) return false;
            try
            {
                var guild = context.Server;

                var exemptcheck = new List<GuildModel.Guild.antispams.IgnoreRole>();
                if (guild.Antispam.IgnoreRoles.Any())
                {
                    exemptcheck = guild.Antispam.IgnoreRoles.Where(x => ((IGuildUser) context.User).RoleIds.Contains(x.RoleID)).ToList();
                }

                if (guild.Antispam.Antispam.NoSpam)
                {
                    var detected = false;
                    var SpamGuild = NoSpam.FirstOrDefault(x => x.GuildID == ((SocketGuildUser) context.User).Guild.Id);
                    if (SpamGuild == null)
                    {
                        NoSpam.Add(new NoSpamGuild
                        {
                            GuildID = context.Guild.Id,
                            Users = new List<NoSpamGuild.NoSpam>
                            {
                                new NoSpamGuild.NoSpam
                                {
                                    UserID = context.User.Id,
                                    Messages = new List<NoSpamGuild.NoSpam.Msg>
                                    {
                                        new NoSpamGuild.NoSpam.Msg
                                        {
                                            LastMessage = context.Message.Content,
                                            LastMessageDate = DateTime.UtcNow
                                        }
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
                        var user = SpamGuild.Users.FirstOrDefault(x => x.UserID == context.User.Id);
                        if (user == null)
                        {
                            SpamGuild.Users.Add(new NoSpamGuild.NoSpam
                            {
                                UserID = context.User.Id,
                                Messages = new List<NoSpamGuild.NoSpam.Msg>
                                {
                                    new NoSpamGuild.NoSpam.Msg
                                    {
                                        LastMessage = context.Message.Content,
                                        LastMessageDate = DateTime.UtcNow
                                    }
                                }
                            });
                        }
                        else
                        {
                            user.Messages.Add(new NoSpamGuild.NoSpam.Msg
                            {
                                LastMessage = context.Message.Content,
                                LastMessageDate = DateTime.UtcNow
                            });
                            if (user.Messages.Count >= 2)
                            {
                                var msgs = user.Messages.Where(x => x.LastMessageDate > DateTime.UtcNow - TimeSpan.FromSeconds(10)).ToList();
                                //Here we detect spam based on wether or not a user is sending the same message repeatedly
                                //Or wether they have sent a message more than 3 times in the last 5 seconds
                                if (msgs.GroupBy(n => n.LastMessage.ToLower()).Any(c => c.Count() > 1) || msgs.Count(x => x.LastMessageDate > DateTime.UtcNow - TimeSpan.FromSeconds(5)) > 3)
                                {
                                    detected = true;
                                }
                            }

                            if (user.Messages.Count > 10)
                            {
                                //Filter out messages so that we only keep a log of the most recent ones within the last 10 seconds.
                                var msgs = user.Messages.OrderBy(x => x.LastMessageDate).ToList();
                                msgs.RemoveRange(0, 1);
                                msgs = msgs.Where(x => x.LastMessageDate > DateTime.UtcNow - TimeSpan.FromSeconds(10)).ToList();
                                user.Messages = msgs;
                            }

                            if (detected)
                            {
                                var BypassAntispam = exemptcheck.Any(x => x.AntiSpam);
                                if (!BypassAntispam)
                                {
                                    if (!guild.Antispam.Antispam.AntiSpamSkip.Any(x => context.Message.Content.ToLower().Contains(x.ToLower())))
                                    {
                                        await context.Message.DeleteAsync();
                                        var delay = AntiSpamMsgDelays.FirstOrDefault(x => x.GuildID == guild.GuildID);
                                        if (delay != null)
                                        {
                                            if (delay._delay > DateTime.UtcNow)
                                            {
                                                return true;
                                            }

                                            delay._delay = DateTime.UtcNow.AddSeconds(5);
                                            var emb = new EmbedBuilder
                                            {
                                                Title = $"{context.User} - No Spamming!!"
                                            };
                                            await context.Channel.SendMessageAsync("", false, emb.Build());
                                            if (guild.Antispam.Antispam.WarnOnDetection)
                                            {
                                                await guild.AddWarn("AutoMod - AntiSpam", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                                guild.Save();
                                            }
                                        }
                                        else
                                        {
                                            AntiSpamMsgDelays.Add(new Delays
                                            {
                                                _delay = DateTime.UtcNow.AddSeconds(5),
                                                GuildID = guild.GuildID
                                            });
                                        }


                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (guild.Antispam.Advertising.Invite)
                {
                    var BypassInvite = exemptcheck.Any(x => x.Advertising);
                    if (!BypassInvite)
                    {
                        if (Regex.Match(context.Message.Content, @"(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?(d+i+s+c+o+r+d+|a+p+p)+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$").Success)
                        {
                            await context.Message.DeleteAsync();
                            var emb = new EmbedBuilder
                            {
                                Description = guild.Antispam.Advertising.NoInviteMessage ?? $"{context.User.Mention} - no sending invite links... the admins might get angry"
                            };
                            await context.Channel.SendMessageAsync("", false, emb.Build());
                            //if
                            // 1. The server Has Invite Deletions turned on
                            // 2. The user is not an admin
                            // 3. The user does not have one of the invite excempt roles
                            if (guild.Antispam.Advertising.WarnOnDetection)
                            {
                                await guild.AddWarn("AutoMod - Anti Advertising", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                guild.Save();
                            }

                            return true;
                        }
                    }
                }

                if (guild.Antispam.Mention.RemoveMassMention || guild.Antispam.Mention.MentionAll)
                {
                    var BypassMention = exemptcheck.Any(x => x.Mention);

                    if (!BypassMention)
                    {
                        if (guild.Antispam.Mention.RemoveMassMention)
                        {
                            if (context.Message.MentionedRoleIds.Count + context.Message.MentionedUserIds.Count >= 5)
                            {
                                await context.Message.DeleteAsync();
                                var emb = new EmbedBuilder
                                {
                                    Title =
                                        $"{context.User} - This server does not allow you to mention 5+ roles or uses at once"
                                };
                                await context.Channel.SendMessageAsync("", false, emb.Build());
                                if (guild.Antispam.Mention.WarnOnDetection)
                                {
                                    await guild.AddWarn("AutoMod - Mass Mention", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                    guild.Save();
                                }

                                return true;
                            }
                        }

                        if (guild.Antispam.Mention.MentionAll)
                        {
                            if (context.Message.Content.Contains("@everyone") || context.Message.Content.Contains("@here"))
                            {
                                await context.Message.DeleteAsync();
                                var emb = new EmbedBuilder();
                                if (guild.Antispam.Mention.MentionAllMessage != null)
                                {
                                    emb.Description = guild.Antispam.Mention.MentionAllMessage;
                                }
                                else
                                {
                                    emb.Title = $"{context.User} - This server has disabled the ability for you to mention @everyone and @here";
                                }

                                await context.Channel.SendMessageAsync("", false, emb.Build());
                                if (guild.Antispam.Mention.WarnOnDetection)
                                {
                                    await guild.AddWarn("AutoMod - Mention All", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                    guild.Save();
                                }

                                return true;
                                //if
                                // 1. The server Has Mention Deletions turned on
                                // 2. The user is not an admin
                                // 3. The user does not have one of the mention excempt roles
                            }
                        }
                    }
                }


                if (guild.Antispam.Privacy.RemoveIPs)
                {
                    var BypassIP = exemptcheck.Any(x => x.Privacy);

                    if (!BypassIP)
                    {
                        if (Regex.IsMatch(context.Message.Content, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                        {
                            await context.Message.DeleteAsync();
                            var emb = new EmbedBuilder
                            {
                                Title = $"{context.User} - This server does not allow you to post IP addresses"
                            };
                            await context.Channel.SendMessageAsync("", false, emb.Build());
                            if (guild.Antispam.Privacy.WarnOnDetection)
                            {
                                await guild.AddWarn("AutoMod - Anti IP", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                guild.Save();
                            }

                            return true;
                        }
                    }
                }


                if (guild.Antispam.Blacklist.BlacklistWordSet.Any() || guild.Antispam.Toxicity.UsePerspective)
                {
                    CommandInfo CMDCheck = null;
                    var argPos = 0;
                    var cmdSearch = _commands.Search(context, argPos);
                    if (cmdSearch.IsSuccess)
                    {
                        CMDCheck = cmdSearch.Commands.FirstOrDefault().Command;
                    }

                    if (guild.Antispam.Blacklist.BlacklistWordSet.Any())
                    {
                        if (CMDCheck == null)
                        {
                            var BypassBlacklist = exemptcheck.Any(x => x.Blacklist);

                            if (!BypassBlacklist)
                            {
                                var blacklistdetected = false;
                                var blacklistmessage = guild.Antispam.Blacklist.DefaultBlacklistMessage;
                                var detectedblacklistmodule = guild.Antispam.Blacklist.BlacklistWordSet.FirstOrDefault(blist => blist.WordList.Any(x => context.Message.Content.ToLower().Contains(x.ToLower())));
                                if (detectedblacklistmodule != null)
                                {
                                    blacklistdetected = true;
                                    blacklistmessage = detectedblacklistmodule.BlacklistResponse ?? guild.Antispam.Blacklist.DefaultBlacklistMessage;
                                }

                                if (blacklistdetected)
                                {
                                    await context.Message.DeleteAsync();

                                    if (!string.IsNullOrEmpty(blacklistmessage))
                                    {
                                        var result = Regex.Replace(blacklistmessage, "{user}", context.User.Username,
                                            RegexOptions.IgnoreCase);
                                        result = Regex.Replace(result, "{user.mention}", context.User.Mention,
                                            RegexOptions.IgnoreCase);
                                        result = Regex.Replace(result, "{guild}", context.Guild.Name, RegexOptions.IgnoreCase);
                                        result = Regex.Replace(result, "{channel}", context.Channel.Name, RegexOptions.IgnoreCase);
                                        result = Regex.Replace(result, "{channel.mention}",
                                            ((SocketTextChannel) context.Channel).Mention, RegexOptions.IgnoreCase);
                                        await context.Channel.SendMessageAsync(result);
                                    }

                                    if (guild.Antispam.Blacklist.WarnOnDetection)
                                    {
                                        await guild.AddWarn("AutoMod - Blacklist", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                        guild.Save();
                                    }

                                    return true;
                                }
                            }
                        }
                    }

                    if (guild.Antispam.Toxicity.UsePerspective)
                    {
                        var BypassToxicity = exemptcheck.Any(x => x.Toxicity);

                        if (!BypassToxicity)
                        {
                            var CheckUsingToxicity = CMDCheck == null;

                            if (ToxicityAPI != null && CheckUsingToxicity && !string.IsNullOrWhiteSpace(context.Message.Content))
                            {
                                try
                                {
                                    var res = ToxicityAPI.QueryToxicity(context.Message.Content);
                                    if (res.attributeScores.TOXICITY.summaryScore.value * 100 > guild.Antispam.Toxicity.ToxicityThreshHold)
                                    {
                                        await context.Message.DeleteAsync();
                                        var emb = new EmbedBuilder
                                        {
                                            Title = "Toxicity Threshhold Breached",
                                            Description = $"{context.User.Mention}"
                                        };
                                        await context.Channel.SendMessageAsync("", false, emb.Build());

                                        /*
                                        if (context.Client.GetChannel(guild.ModLogChannel) is IMessageChannel modchannel)
                                        {
                                            try
                                            {
                                                emb.Description = "Message Auto-Removed.\n" +
                                                                    $"User: {context.User.Mention}\n" +
                                                                    $"Channel: {context.Channel.Name}\n" +
                                                                    $"Toxicity %: {res.attributeScores.TOXICITY.summaryScore.value * 100}\n" +
                                                                    "Message: \n" +
                                                                    $"{context.Message.Content}";
                                                await modchannel.SendMessageAsync("", false, emb.Build());
                                            }
                                            catch
                                            {
                                                //
                                            }
                                        }
                                        */

                                        if (guild.Antispam.Blacklist.WarnOnDetection)
                                        {
                                            await guild.AddWarn("AutoMod - Toxicity", context.User as IGuildUser, context.Client.CurrentUser, context.Channel);
                                            guild.Save();
                                        }

                                        return true;
                                    }
                                }
                                catch
                                {
                                    //
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage(e.ToString(), LogSeverity.Error);
            }


            return false;
        }

        public async Task DoCommand(SocketMessage parameterMessage)
        {
            try
            {
                if (!(parameterMessage is SocketUserMessage message)) return;
                var argPos = 0;
                var context = new LithiumContext(_client, message, Provider);

                //Do not react to commands initiated by a bot
                if (context.User.IsBot) return;

                if (await antispam(context)) return;

                //Ensure that commands are only executed if they start with the bot's prefix
                if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(Config.Load().DefaultPrefix, ref argPos) || message.HasStringPrefix(context.Server?.Settings.Prefix, ref argPos))) return;

                //Ensure that the message passes all checks before running as a command
                if (CheckHidden(context)) return;

                var result = await _commands.ExecuteAsync(context, argPos, Provider);

                var commandsuccess = result.IsSuccess;

                if (!commandsuccess)
                {
                    var embed = new EmbedBuilder
                    {
                        Title = $"ERROR: {result.Error.ToString().ToUpper()}",
                        Description = $"Command: {context.Message}\n" +
                                      $"Error: {result.ErrorReason}"
                    };
                    await context.Channel.SendMessageAsync("", false, embed.Build());
                    Logger.LogMessage($"{message.Content} || {message.Author}", LogSeverity.Error);
                }
                else
                {
                    Logger.LogMessage($"{message.Content} || {message.Author}");
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }




        private class NoSpamGuild
        {
            public ulong GuildID { get; set; }
            public List<NoSpam> Users { get; set; } = new List<NoSpam>();

            public class NoSpam
            {
                public ulong UserID { get; set; }
                public List<Msg> Messages { get; set; } = new List<Msg>();

                public class Msg
                {
                    public string LastMessage { get; set; }
                    public DateTime LastMessageDate { get; set; }
                }
            }
        }

        public class Delays
        {
            public DateTime _delay { get; set; } = DateTime.UtcNow;
            public ulong GuildID { get; set; }
        }
    }
}