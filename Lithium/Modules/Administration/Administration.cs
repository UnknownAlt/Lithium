using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
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


            await ReplyAsync($"Channels Modified for Mute Role:\n" +
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
            await ReplyAsync($"Types:\n" +
                             $"`{GuildModel.Guild.Moderation.msettings.warnLimitAction.NoAction}`\n" +
                             $"`{GuildModel.Guild.Moderation.msettings.warnLimitAction.Kick}`\n" +
                             $"`{GuildModel.Guild.Moderation.msettings.warnLimitAction.Ban}`\n\n" +
                             $"Command Use:\n" +
                             $"`SetWarnLimitAction <Type>`");
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
                                  $"NOTE: This does not unban the user."
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
                await ReplyAsync($"No module found with this name.");
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
                await ReplyAsync($"No command found with this name.");
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
                await ReplyAsync($"No module found with this name.");
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
                await ReplyAsync($"No command found with this name.");
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
    }
}