using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Contexts.Paginator;
using Lithium.Discord.Preconditions;
using Lithium.Models;

namespace Lithium.Modules.Administration
{
    [RequireRole.RequireAdmin]
    [Group("Admin")]
    public class Administration : Base
    {
        private readonly CommandService _service;

        private Administration(CommandService service)
        {
            _service = service;
        }

        [Command("SetEventChannel")]
        [Summary("Admin SetEventChannel")]
        [Remarks("set the current channel for event logging")]
        public async Task EventChannel()
        {
            Context.Server.EventLogger.EventChannel = Context.Channel.Id;
            Context.Server.EventLogger.LogEvents = true;
            Context.Server.Save();
            await ReplyAsync($"Success! Events will now be logged in {Context.Channel.Name}");
        }
        [Command("LogEvents")]
        [Summary("Admin LogEvents")]
        [Remarks("toggle event logging")]
        public async Task LogEventToggle()
        {
            Context.Server.EventLogger.LogEvents = !Context.Server.EventLogger.LogEvents;
            Context.Server.Save();
            await ReplyAsync($"EventLogging: {Context.Server.EventLogger.LogEvents}");
        }

        [Command("SetMutedRole")]
        [Summary("Admin SetMutedRole <@Role>")]
        [Remarks("set role users are given upon being muted")]
        public async Task MuteRole(IRole role)
        {
            Context.Server.ModerationSetup.Mutes.mutedrole = role.Id;
            Context.Server.Save();
            await ReplyAsync($"Success! Users will be given the role {role.Name} upon being muted.");

            string perms;
            var channels = "";
            try
            {
                var unverifiedPerms =
                    new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny);
                foreach (var channel in Context.Socket.Guild.TextChannels)
                    try
                    {
                        await channel.AddPermissionOverwriteAsync(role, unverifiedPerms);
                        channels += $"`#{channel.Name}` Perms Modified\n";
                    }
                    catch
                    {
                        channels += $"`#{channel.Name}` Perms Not Modified\n";
                    }

                perms = "Role Can No longer Send Messages, or Add Reactions";
            }
            catch
            {
                perms = "Role Unable to be modified, ask an administrator to do this manually.";
            }


            await ReplyAsync("Channels Modified for Mute Role:\n" +
                             $"{perms}\n" +
                             $"{channels}");
        }

        [Command("SetWarnLimit")]
        [Summary("Admin SetWarnLimit <Limit>")]
        [Remarks("set amount of warns before autokick or autoban")]
        public async Task WarnLimit(int limit = int.MaxValue)
        {
            Context.Server.ModerationSetup.Settings.warnlimit = limit;
            Context.Server.Save();
            await ReplyAsync($"Success! After {limit} warnings, an auto-action will be taken on the user");
        }

        [Command("SetWarnLimitAction")]
        [Summary("Admin SetWarnLimitAction <Limit>")]
        [Remarks("set what happens to users who exceed the warn limit")]
        public async Task WarnAction(GuildModel.Guild.Moderation.msettings.warnLimitAction action)
        {
            Context.Server.ModerationSetup.Settings.WarnLimitAction = action;
            Context.Server.Save();
            await ReplyAsync($"Success! {action.ToString()} will be taken upon warn limit being exceeded");
        }

        [Command("SetWarnLimitAction")]
        [Summary("Admin SetWarnLimitAction")]
        [Remarks("Overload With Info")]
        public async Task WarnAction()
        {
            await ReplyAsync("Types:\n" +
                             $"`{GuildModel.Guild.Moderation.msettings.warnLimitAction.NoAction}`\n" +
                             $"`{GuildModel.Guild.Moderation.msettings.warnLimitAction.Kick}`\n" +
                             $"`{GuildModel.Guild.Moderation.msettings.warnLimitAction.Ban}`\n\n" +
                             "Command Use:\n" +
                             "`SetWarnLimitAction <Type>`");
        }

        [Command("SetModLogChannel")]
        [Summary("Admin SetModLogChannel")]
        [Remarks("set the moderator logging channel")]
        public async Task SetModChannel()
        {
            Context.Server.ModerationSetup.Settings.ModLogChannel = Context.Channel.Id;
            Context.Server.Save();
            await ReplyAsync($"Mod Logs will now be sent in {Context.Channel.Name}");
        }

        [Command("HackBan")]
        [Summary("HackBan <user ID>")]
        [Remarks("Ban a user by their user ID even if they are not in the server")]
        public async Task HackBan(ulong UserID)
        {
            Context.Server.ModerationSetup.Bans.Add(new GuildModel.Guild.Moderation.ban
            {
                userID = UserID,
                modID = Context.User.Id,
                modname = Context.User.Username,
                reason = "HackBan",
                username = $"[{UserID}]"
            });
            try
            {
                await Context.Guild.AddBanAsync(UserID, 1, "HackBan");
                await ReplyAsync("User has been Banned and messages from the last 24 hours have been cleared");
                Context.Server.Save();
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = $"User with ID [{UserID}] has been banned",
                    Description = $"User: [{UserID}]\n" +
                                  $"UserID: {UserID}\n" +
                                  $"Mod: {Context.User.Username}#{Context.User.Discriminator}\n" +
                                  $"Mod ID: {Context.User.Id}\n\n" +
                                  "Reason:\n" +
                                  "HackBan"
                });
            }
            catch
            {
                await ReplyAsync("User is unable to be Banned. ");
            }
        }

        [Command("ClearWarns")]
        [Summary("ClearWarns <@user>")]
        [Remarks("Clear all warnings for the specified user")]
        public async Task DelWarn(IUser User)
        {
            if (Context.Server.ModerationSetup.Warns.Any(x => x.userID == User.Id))
            {
                var warnstring = string.Join("\n", Context.Server.ModerationSetup.Warns.Where(x => x.userID == User.Id).Select(x => $"Mod: {x.modname} [{x.modID}]\nReason: {x.reason}\n"));
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = "The Following warnings have been cleared!",
                    Description = $"**User: {User.Username} [{User.Id}]**\n" +
                                  $"{warnstring}"
                });

                Context.Server.ModerationSetup.Warns = Context.Server.ModerationSetup.Warns.Where(x => x.userID != User.Id).ToList();
                Context.Server.Save();
            }
            else
            {
                await ReplyAsync("No warns found for this user.");
            }
        }

        [Command("ClearKicks")]
        [Summary("ClearKicks <@user>")]
        [Remarks("Clear all kick logs for the specified user")]
        public async Task DelKick(IUser User)
        {
            if (Context.Server.ModerationSetup.Kicks.Any(x => x.userID == User.Id))
            {
                var warnstring = string.Join("\n", Context.Server.ModerationSetup.Kicks.Where(x => x.userID == User.Id).Select(x => $"Mod: {x.modname} [{x.modID}]\nReason: {x.reason}\n"));
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = "The Following Kicks have been cleared!",
                    Description = $"**User: {User.Username} [{User.Id}]**\n" +
                                  $"{warnstring}"
                });

                Context.Server.ModerationSetup.Kicks = Context.Server.ModerationSetup.Kicks.Where(x => x.userID != User.Id).ToList();
                Context.Server.Save();
            }
            else
            {
                await ReplyAsync("No Kicks found for this user.");
            }
        }

        [Command("ClearBans")]
        [Summary("ClearBans <@user>")]
        [Remarks("Clear all ban logs for the specified user")]
        public async Task delBan(IUser User)
        {
            if (Context.Server.ModerationSetup.Bans.Any(x => x.userID == User.Id))
            {
                var warnstring = string.Join("\n", Context.Server.ModerationSetup.Bans.Where(x => x.userID == User.Id).Select(x => $"Mod: {x.modname} [{x.modID}]\nReason: {x.reason}\n"));
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = "The Following Bans have been cleared!",
                    Description = $"**User: {User.Username} [{User.Id}]**\n" +
                                  $"{warnstring}\n\n" +
                                  "NOTE: This does not unban the user."
                });

                Context.Server.ModerationSetup.Bans = Context.Server.ModerationSetup.Bans.Where(x => x.userID != User.Id).ToList();
                Context.Server.Save();
            }
            else
            {
                await ReplyAsync("No Bans found for this user.");
            }
        }


        [Command("HideModule")]
        [Summary("Admin HideModule <modulename>")]
        [Remarks("Disable a module from being used by users")]
        public async Task HideModule([Remainder] string modulename = null)
        {
            if (_service.Modules.Any(x => string.Equals(x.Name, modulename, StringComparison.CurrentCultureIgnoreCase)))
            {
                Context.Server.Settings.DisabledParts.BlacklistedModules.Add(modulename.ToLower());
                Context.Server.Save();
                await ReplyAsync(
                    $"Commands from {modulename} will no longer be accessible or visible to regular users");
            }
            else
            {
                await ReplyAsync("No module found with this name.");
            }
        }

        [Command("HideCommand")]
        [Summary("Admin HideCommand <commandname>")]
        [Remarks("Disable a command from being used by users")]
        public async Task HideCommand([Remainder] string cmdname = null)
        {
            if (_service.Commands.Any(x => string.Equals(x.Name, cmdname, StringComparison.CurrentCultureIgnoreCase)))
            {
                Context.Server.Settings.DisabledParts.BlacklistedCommands.Add(cmdname.ToLower());
                Context.Server.Save();
                await ReplyAsync($"{cmdname} will no longer be accessible or visible to regular users");
            }
            else
            {
                await ReplyAsync("No command found with this name.");
            }
        }

        [Command("UnHideModule")]
        [Summary("Admin UnHideModule <modulename>")]
        [Remarks("Re-Enable a module to be used by users")]
        public async Task UnHideModule([Remainder] string modulename = null)
        {
            if (_service.Modules.Any(x => string.Equals(x.Name, modulename, StringComparison.CurrentCultureIgnoreCase)))
            {
                Context.Server.Settings.DisabledParts.BlacklistedModules.Remove(modulename.ToLower());
                Context.Server.Save();
                await ReplyAsync($"Commands from {modulename} are now accessible to non-admins again");
            }
            else
            {
                await ReplyAsync("No module found with this name.");
            }
        }

        [Command("UnHideCommand")]
        [Summary("Admin UnHideCommand <commandname>")]
        [Remarks("Re-Enable a command to be used by users")]
        public async Task UnHideCommand([Remainder] string cmdname = null)
        {
            if (_service.Commands.Any(x => string.Equals(x.Name, cmdname, StringComparison.CurrentCultureIgnoreCase)))
            {
                Context.Server.Settings.DisabledParts.BlacklistedCommands.Remove(cmdname.ToLower());
                Context.Server.Save();
                await ReplyAsync($"{cmdname} is now accessible to non-admins again");
            }
            else
            {
                await ReplyAsync("No command found with this name.");
            }
        }

        [Command("HiddenCommands")]
        [Alias("HiddenModules")]
        [Summary("Admin HiddenCommands")]
        [Remarks("list all hidden commands and modules")]
        public async Task HiddenCMDs([Remainder] string cmdname = null)
        {
            var embed = new EmbedBuilder();
            var jsonObj = Context.Server;
            if (jsonObj.Settings.DisabledParts.BlacklistedModules.Any())
                embed.AddField("Blacklisted Modules",
                    $"{string.Join("\n", jsonObj.Settings.DisabledParts.BlacklistedModules)}");

            if (jsonObj.Settings.DisabledParts.BlacklistedCommands.Any())
            {
                var desc = "";
                foreach (var cmd in jsonObj.Settings.DisabledParts.BlacklistedCommands)
                {
                    var cmdcummary = _service.Commands.FirstOrDefault(x =>
                                             string.Equals(x.Name, cmd, StringComparison.CurrentCultureIgnoreCase))
                                         ?.Summary ?? cmd;
                    desc += $"{cmdcummary}\n";
                }

                embed.AddField("Blacklisted Commands",
                    $"{desc}");
            }

            await ReplyAsync("", false, embed.Build());
        }

        [Command("ServerSetup")]
        [Summary("ServerSetup")]
        [Remarks("View the PassiveBOT Server Config")]
        public async Task ServerSetup()
        {
            var Guild = Context.Server;
            var pages = new List<PaginatedMessage.Page>
            {
                new PaginatedMessage.Page
                {
                    dynamictitle = $"Guild Info",
                    description = $"ID: {Guild.GuildID}\n" +
                                  $"Prefix: {Guild.Settings.Prefix ?? Config.Load().DefaultPrefix}\n"
                },/*
                new PaginatedMessage.Page
                {
                    dynamictitle = "Welcome and Goodbye",
                    description = $"__**Welcome**__\n" +
                                  $"Event: {Guild.WelcomeEvent}\n" +
                                  $"Message: {Guild.WelcomeMessage ?? "N/A"}\n" +
                                  $"Channel: {Context.Guild.GetChannel(Guild.WelcomeChannel)?.Name ?? "N/A"}\n\n" +
                                  $"__**Goodbye**__\n" +
                                  $"Event: {Guild.GoodbyeEvent}\n" +
                                  $"Message: {Guild.GoodbyeMessage ?? "N/A"}\n" +
                                  $"Channel: {Context.Guild.GetChannel(Guild.GoodByeChannel)?.Name ?? "N/A"}\n"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = $"Partner Program",
                    description = $"Signed Up: {Guild.PartnerSetup.IsPartner}\n" +
                                  $"Banned: {Guild.PartnerSetup.banned}\n" +
                                  $"Channel: {Context.Guild.GetChannel(Guild.PartnerSetup.PartherChannel)?.Name ?? "N/A"}\n" +
                                  $"Image URL: {Guild.PartnerSetup.ImageUrl ?? "N/A"}\n" +
                                  $"Show User Count: {Guild.PartnerSetup.showusercount}\n" +
                                  $"Message: \n" +
                                  $"{Guild.PartnerSetup.Message ?? "N/A"}"
                },*/
                new PaginatedMessage.Page
                {
                    dynamictitle = $"AntiSpam 1.Prevention",
                    description = $"NoSpam: {Guild.Antispam.Antispam.NoSpam}\n" +
                                  $"Remove IPs: {Guild.Antispam.Privacy.RemoveIPs}\n" +
                                  $"Remove Invites: {Guild.Antispam.Advertising.Invite}\n" +
                                  $"Remove Invites Message:\n" +
                                  $"{Guild.Antispam.Advertising.NoInviteMessage ?? "N/A"}\n\n" +
                                  $"Remove @Everyone and @Here: {Guild.Antispam.Mention.MentionAll}\n" +
                                  $"Remove @Everyone and @Here Message:\n" +
                                  $"{Guild.Antispam.Mention.MentionAllMessage}\n\n" +
                                  $"Remove @everyone and @here exempt:\n" +
                                  $"Remove Messages with 5+ Mentions: {Guild.Antispam.Mention.RemoveMassMention}\n"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = $"AntiSpam 2.Blacklist",
                    description = $"Using Blacklist: {Guild.Antispam.Blacklist.BlacklistWordSet.Any()}\n" +
                                  $"Default Blacklist Message: {Guild.Antispam.Blacklist.DefaultBlacklistMessage ?? "N/A"}\n" +
                                  $"Blacklisted Words:\n" +
                                  $"Use the `{Config.Load().DefaultPrefix}blacklist` message to show this\n"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = $"AntiSpam 3.Toxicity",
                    description = $"NoToxicity: {Guild.Antispam.Toxicity.UsePerspective}\n" +
                                  $"Threshhold: {Guild.Antispam.Toxicity.ToxicityThreshHold}"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = $"AntiSpam 4.Exempt",
                    description =
                        $"{(Guild.Antispam.IgnoreRoles.Any() ? string.Join("\n", Guild.Antispam.IgnoreRoles.Where(x => Context.Guild.GetRole(x.RoleID) != null).Select(x => $"__{Context.Guild.GetRole(x.RoleID).Name}__\nBypass Antispam: {x.AntiSpam}\nBypass Blacklist: {x.Blacklist}\nBypass Mention: {x.Mention}\nBypass Invite: {x.Advertising}\nBypass Filtering: {x.Privacy}\n")) : "N/A")}"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = "Kicks Warns and Bans",
                    description = $"Kicks: {(Guild.ModerationSetup.Kicks.Any() ? Guild.ModerationSetup.Kicks.Count.ToString() : "N/A")}\n" +
                                  $"Warns: {(Guild.ModerationSetup.Warns.Any() ? Guild.ModerationSetup.Warns.Count.ToString() : "N/A")}\n" +
                                  $"Bans: {(Guild.ModerationSetup.Bans.Any() ? Guild.ModerationSetup.Bans.Count.ToString() : "N/A")}\n"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = "Event & Error Logging",
                    description = $"Event Logging: {Guild.EventLogger.LogEvents}\n" +
                                  $"Event Channel: {Context.Socket.Guild.GetChannel(Guild.EventLogger.EventChannel)?.Name ?? "N/A"}"
                },/*
                new PaginatedMessage.Page
                {
                    dynamictitle = "Tagging",
                    description = $"Using Tags: {Guild.Dict.Any()}\n" +
                                  $"Tag Names: \n{(Guild.Dict.Any() ? string.Join("\n", Guild.Dict.Select(x => x.Tagname)) : "N/A")}\n"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = "AutoMessage",
                    description = $"Using Automessage: {Guild.AutoMessage.Any()}\n" +
                                  $"Auto Message Channels:\n" +
                                  $"{(Guild.AutoMessage.Any() ? string.Join("\n", Guild.AutoMessage.Select(x => Context.Guild.GetChannel(x.channelID)?.Name).Where(x => x != null)) : "N/A")}"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = "Levelling",
                    description = $"Enabled: {Guild.Levels.LevellingEnabled}\n" +
                                  $"Level Messages: {Guild.Levels.UseLevelMessages}\n" +
                                  $"Use Level Log Channel: {Guild.Levels.UseLevelChannel}\n" +
                                  $"Level Log Channel: {Context.Guild.GetChannel(Guild.Levels.LevellingChannel)?.Name ?? "N/A"}\n" +
                                  $"Increment Rewards: {Guild.Levels.IncrementLevelRewards}\n" +
                                  $"Levels: \n" +
                                  $"{(Guild.Levels.LevelRoles.Any() ? string.Join("\n", Guild.Levels.LevelRoles.Select(x => $"{Context.Guild.GetRole(x.RoleID)?.Mention ?? "Removed Role"} Level Requirement: {x.LevelToEnter}")) : "N/A")}"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = "Gambling",
                    description = $"Guild Wealth: {Guild.Gambling.Users.Sum(x => x.coins)}\n" +
                                  $"Currency Name: {Guild.Gambling.settings.CurrencyName}\n" +
                                  $"Store Items: {Guild.Gambling.Store.ShowItems.Count}\n" +
                                  $"Enabled: {Guild.Gambling.enabled}"
                },*/
                new PaginatedMessage.Page
                {
                    dynamictitle = "Moderators",
                    description =
                        $"Mod Roles: {(Guild.ModerationSetup.ModeratorRoles.Any() ? string.Join("\n", Guild.ModerationSetup.ModeratorRoles.Where(mr => Context.Guild.Roles.Any(x => x.Id == mr)).Select(mr => Context.Guild.GetRole(mr)?.Name)) : "N/A")}\n" +
                        $"Moderator Logging: {Guild.ModerationSetup.Settings.ModLogChannel != 0}\n" +
                        $"Moderator Log Channel: {Context.Socket.Guild.GetChannel(Guild.ModerationSetup.Settings.ModLogChannel)?.Name ?? "N/A"}"
                },
                new PaginatedMessage.Page
                {
                    dynamictitle = "Administrators",
                    description =
                        $"Admin Roles: {(Guild.ModerationSetup.AdminRoles.Any() ? string.Join("\n", Guild.ModerationSetup.AdminRoles.Where(ar => Context.Guild.Roles.Any(x => x.Id == ar)).Select(ar => Context.Guild.GetRole(ar)?.Name)) : "N/A")}"
                }
            };

            await PagedReplyAsync(new PaginatedMessage
            {
                Pages = pages
            });
        }
    }
}