using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Models;

namespace Lithium.Commands
{
    public class Example : ModuleBase
    {
        public readonly CommandService _service;

        public Example(CommandService service)
        {
            _service = service;
        }



        [Command("help")]
        [Summary("help")]
        [Remarks("all help commands")]
        public async Task HelpAsync([Remainder] string modulearg = null)
        {
            var embed = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Title = $"{Context.Client.CurrentUser.Username} Commands"
            };
            var prefix = Config.Load().DefaultPrefix;
            if (modulearg == null) //ShortHelp
            {
                var modules = _service.Modules.Where(x => x.Commands.Count > 0).Select(x => new simplemoduleobj
                {
                    moduledesc = string.Join("\n",
                        x.Commands.Select(c => $"`{prefix}{c.Summary ?? c.Name}` {(c.Remarks == null ? "" : $"- {c.Remarks}")}")),
                    modulename = x.Name
                });
                foreach (var module in modules)
                {
                    embed.AddField(module.modulename, module.moduledesc);
                }

                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                var modules = _service.Modules.Where(x => x.Commands.Count > 0).Where(x => string.Equals(x.Name, modulearg, StringComparison.CurrentCultureIgnoreCase)).Select(x => new simplemoduleobj
                {
                    moduledesc = string.Join("\n",
                        x.Commands.Select(c => $"`{prefix}{c.Summary ?? c.Name}` {(c.Remarks == null ? "" : $"- {c.Remarks}")}")),
                    modulename = x.Name
                });
                foreach (var module in modules)
                {
                    embed.AddField(module.modulename, module.moduledesc);
                }

                await ReplyAsync("", false, embed.Build());
            }
        }

        public class simplemoduleobj
        {
            public string modulename { get; set; }
            public string moduledesc { get; set; }
        }

    }
}
