using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;

namespace Lithium.Modules
{
    public class Moderation : Base
    {
        [Command("LoadCFG")]
        public async Task Setgame()
        {
            await ReplyAsync($"{Context.Server.GuildID}");
        }
    }
}
