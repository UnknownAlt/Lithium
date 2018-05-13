using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Preconditions;

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