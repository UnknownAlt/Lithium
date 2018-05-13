using System.Reflection.Metadata;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Preconditions;
using Lithium.Handlers;
using Lithium.Models;

namespace Lithium.Modules.ServerOwner
{
    [RequireOwner.ServerOwner]
    public class Serverowner : Base
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

        [Command("addadmin")]
        [Summary("addadmin <@role>")]
        [Remarks("Add a new administrator role")]
        public async Task AddAdminRole(IRole AdminRole = null)
        {
            if (AdminRole == null)
            {
                await ReplyAsync("Please provide a role to add");
                return;
            }

            if (!Context.Server.ModerationSetup.AdminRoles.Contains(AdminRole.Id))
            {
                Context.Server.ModerationSetup.AdminRoles.Add(AdminRole.Id);
                Context.Server.Save();
            }

            await ReplyAsync("Admin Role added.");
        }

        [Command("reset")]
        [Summary("reset <confirm>")]
        [Remarks("reset the entire guild's config")]
        public async Task AddAdminRole(string confirmcode = null)
        {
            if (confirmcode != "287f3njg3o5")
            {
                await ReplyAsync("Please use this command again using the confirm code: `287f3njg3o5` to rset the server config. NOTE: This is permanent and cannot be reversed");
                return;
            }

            DatabaseHandler.RemoveGuild(Context.Guild.Id);
            DatabaseHandler.AddGuild(Context.Guild.Id);

            await ReplyAsync("Config Reset.");
        }
    }
}