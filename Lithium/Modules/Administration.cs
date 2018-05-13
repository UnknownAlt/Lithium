using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Preconditions;

namespace Lithium.Modules
{
    [RequireRole.RequireAdmin]
    [Group("admin")]
    public class Administration : Base
    {
        [Command("addmod")]
        [Summary("addmod <@role>")]
        [Remarks("Add a new moderator role")]
        public async Task AddModRole(IRole ModRole = null)
        {
            if (ModRole == null)
            {
                await ReplyAsync("Please provide a role to add");
                return;
            }

            if (!Context.Server.ModerationSetup.ModeratorRoles.Contains(ModRole.Id))
            {
                Context.Server.ModerationSetup.ModeratorRoles.Add(ModRole.Id);
                Context.Server.Save();
            }

            await ReplyAsync("Moderator Role added.");
        }
    }
}