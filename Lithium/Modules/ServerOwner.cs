namespace Lithium.Modules
{
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;

    [GuildOwner]
    public class ServerOwner : Base
    {
        // TODO Make these two commands proper
        [Command("SetModLog")]
        public async Task SetModLog()
        {
            Context.Server.ModerationSetup.Settings.ModLogChannel = Context.Channel.Id;
            Context.Server.Save();
        }

        [Command("SetMute")]
        public async Task SetMute(IRole mute)
        {
            Context.Server.ModerationSetup.Settings.MutedRoleId = mute.Id;
            Context.Server.Save();
        }

        [Command("AddMod")]
        [Remarks("Add a new moderator role")]
        public async Task AddModRoleAsync(IRole modRole = null)
        {
            if (modRole == null)
            {
                await ReplyAsync("Please provide a role to add");
                return;
            }

            if (!Context.Server.ModerationSetup.ModeratorRoles.Contains(modRole.Id))
            {
                Context.Server.ModerationSetup.ModeratorRoles.Add(modRole.Id);
                Context.Server.Save();
            }

            await ReplyAsync("Moderator Role added.");
        }

        [Command("DelMod")]
        [Remarks("Delete a moderator role")]
        public async Task DelModRoleAsync(IRole modRole = null)
        {
            if (modRole == null)
            {
                await ReplyAsync("Please provide a role to add");
                return;
            }

            if (Context.Server.ModerationSetup.ModeratorRoles.Contains(modRole.Id))
            {
                Context.Server.ModerationSetup.ModeratorRoles.Remove(modRole.Id);
                Context.Server.Save();
                await ReplyAsync("Moderator Role Removed.");
            }
            else
            {
                await ReplyAsync("That is not a moderator role.");
            }
        }

        [Command("DelMod")]
        [Remarks("Delete a moderator role")]
        public async Task DelModRoleAsync(ulong modRole)
        {
            if (Context.Server.ModerationSetup.ModeratorRoles.Contains(modRole))
            {
                Context.Server.ModerationSetup.ModeratorRoles.Remove(modRole);
                Context.Server.Save();
                await ReplyAsync("Moderator Role Removed.");
            }
            else
            {
                await ReplyAsync("That is not a moderator role.");
            }
        }

        [Command("AddAdmin")]
        [Remarks("Add a new administrator role")]
        public async Task AddAdminRoleAsync(IRole adminRole = null)
        {
            if (adminRole == null)
            {
                await ReplyAsync("Please provide a role to add");
                return;
            }

            if (!Context.Server.ModerationSetup.AdminRoles.Contains(adminRole.Id))
            {
                Context.Server.ModerationSetup.AdminRoles.Add(adminRole.Id);
                Context.Server.Save();
            }

            await ReplyAsync("Admin Role added.");
        }

        [Command("DelAdmin")]
        [Remarks("Delete an admin role")]
        public async Task DelAdminAsync(IRole adminRole = null)
        {
            if (adminRole == null)
            {
                await ReplyAsync("Please provide a role to add");
                return;
            }

            if (Context.Server.ModerationSetup.AdminRoles.Contains(adminRole.Id))
            {
                Context.Server.ModerationSetup.AdminRoles.Remove(adminRole.Id);
                Context.Server.Save();
                await ReplyAsync("Admin Role Removed.");
            }
            else
            {
                await ReplyAsync("That is not a admin role.");
            }
        }
        
        [Command("DelAdmin")]
        [Remarks("Delete an admin role")]
        public async Task DelAdminAsync(ulong adminRole)
        {
            if (Context.Server.ModerationSetup.AdminRoles.Contains(adminRole))
            {
                Context.Server.ModerationSetup.AdminRoles.Remove(adminRole);
                Context.Server.Save();
                await ReplyAsync("Admin Role Removed.");
            }
            else
            {
                await ReplyAsync("That is not a admin role.");
            }
        }
    }
}
