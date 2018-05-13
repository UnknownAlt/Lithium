using System;
using System.Collections.Generic;
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

        public IServiceProvider Provider;

        public EventHandler(IServiceProvider provider)
        {
            Provider = provider;
            _client = Provider.GetService<DiscordSocketClient>();
            _commands = new CommandService();
            _timerservice = new TimerService(_client);

            _client.MessageReceived += DoCommand;
            _client.JoinedGuild += _client_JoinedGuild;
            _client.Ready += _client_Ready;
        }

        private async Task _client_Ready()
        {
            try
            {
                await DatabaseHandler.DatabaseCheck(_client);
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
                Console.WriteLine(e);
            }

            await _client.SetGameAsync($"{Config.Load().DefaultPrefix}help // {_client.Guilds.Sum(x => x.MemberCount)} Users!");
        }

        private async Task _client_JoinedGuild(SocketGuild guild)
        {
            //Ensure that we notify new servers how to use the bot by telling them how to get use th ehelp command.
            var embed = new EmbedBuilder
            {
                Title = guild.CurrentUser.Username,
                Description = $"Hi there, I am {guild.CurrentUser.Username}. Type `{Config.Load().DefaultPrefix}help` to see a list of my commands",
                Color = Color.Blue
            };
            await guild.DefaultChannel.SendMessageAsync("", false, embed.Build());
            var dblist = DatabaseHandler.GetFullConfig();
            foreach (var guildb in _client.Guilds.Where(g => dblist.All(x => x.GuildID != g.Id)))
            {
                DatabaseHandler.AddGuild(guildb.Id);
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
                    var guser = (IGuildUser)context.User;
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
            var guild = context.Server;

            var exemptcheck = new List<GuildModel.Guild.antispams.IgnoreRole>();
            if (guild.Antispam.IgnoreRoles.Any())
            {
                exemptcheck = guild.Antispam.IgnoreRoles.Where(x => ((IGuildUser)context.User).RoleIds.Contains(x.RoleID)).ToList();
            }

            if (guild.Antispam.Antispam.NoSpam)
            {
                var detected = false;
                var SpamGuild = NoSpam.FirstOrDefault(x => x.GuildID == ((SocketGuildUser) context.User).Guild.Id);
                if (SpamGuild == null)
                {
                    NoSpam.Add(new NoSpamGuild
                    {
                        GuildID = ((SocketGuildUser) context.User).Guild.Id,
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

                        if (detected && guild.Antispam.Antispam.NoSpam)
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
                            Description =
                                guild.Antispam.Advertising.NoInviteMessage ??
                                $"{context.User.Mention} - no sending invite links... the admins might get angry"
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



            if (guild.Antispam.Blacklist.BlacklistWordSet.Any())
            {
                CommandInfo CMDCheck = null;
                var argPos = 0;
                var cmdSearch = _commands.Search(context, argPos);
                if (cmdSearch.IsSuccess)
                {
                    CMDCheck = cmdSearch.Commands.FirstOrDefault().Command;
                }

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

            return false;
        }
        public List<Delays> AntiSpamMsgDelays = new List<Delays>();
        public class Delays
        {
            public DateTime _delay { get; set; } = DateTime.UtcNow;
            public ulong GuildID { get; set; }
        }

        public GuildModel.Guild.Moderation.warn QuickWarn(string reason, IUser user, IUser mod)
        {
            return new GuildModel.Guild.Moderation.warn
            {
                modname = mod.Username,
                modID = mod.Id,
                reason = reason,
                username = user.Username,
                userID = user.Id
            };
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



                //Ensure that commands are only executed if they start with the bot's prefix
                if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(Config.Load().DefaultPrefix, ref argPos) || message.HasStringPrefix(context.Server?.Settings.Prefix , ref argPos))) return;

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
                    Logger.LogError($"{message.Content} || {message.Author}");
                }
                else
                {
                    Logger.LogInfo($"{message.Content} || {message.Author}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}