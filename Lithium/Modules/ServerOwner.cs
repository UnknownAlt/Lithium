namespace Lithium.Modules
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.ServerOwner)]
    public class ServerOwner : Base
    {
        [Command("SetModLog")]
        public Task SetModLogAsync()
        {
            Context.Server.ModerationSetup.Settings.ModLogChannel = Context.Channel.Id;
            Context.Server.Save();
            return SimpleEmbedAsync($"Moderation Log messages will be sent to {Context.Channel.Name}");
        }

        [Command("SetMute")]
        public Task SetMuteAsync(IRole mute)
        {
            Context.Server.ModerationSetup.Settings.MutedRoleId = mute.Id;
            Context.Server.Save();
            return SimpleEmbedAsync($"Users will be given {mute.Mention} when muted.");
        }

        [Command("AddMod")]
        [Remarks("Add a new moderator role")]
        public async Task AddModRoleAsync(IRole modRole = null)
        {
            if (modRole == null)
            {
                await SimpleEmbedAsync("Please provide a role to add");
                return;
            }

            if (!Context.Server.ModerationSetup.ModeratorRoles.Contains(modRole.Id))
            {
                Context.Server.ModerationSetup.ModeratorRoles.Add(modRole.Id);
                Context.Server.Save();
            }

            await SimpleEmbedAsync("Moderator Role added.");
        }

        [Command("DelMod")]
        [Remarks("Delete a moderator role")]
        public async Task DelModRoleAsync(IRole modRole = null)
        {
            if (modRole == null)
            {
                await SimpleEmbedAsync("Please provide a role to add");
                return;
            }

            if (Context.Server.ModerationSetup.ModeratorRoles.Contains(modRole.Id))
            {
                Context.Server.ModerationSetup.ModeratorRoles.Remove(modRole.Id);
                Context.Server.Save();
                await SimpleEmbedAsync("Moderator Role Removed.");
            }
            else
            {
                await SimpleEmbedAsync("That is not a moderator role.");
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
                await SimpleEmbedAsync("Moderator Role Removed.");
            }
            else
            {
                await SimpleEmbedAsync("That is not a moderator role.");
            }
        }

        [Command("AddAdmin")]
        [Remarks("Add a new administrator role")]
        public async Task AddAdminRoleAsync(IRole adminRole = null)
        {
            if (adminRole == null)
            {
                await SimpleEmbedAsync("Please provide a role to add");
                return;
            }

            if (!Context.Server.ModerationSetup.AdminRoles.Contains(adminRole.Id))
            {
                Context.Server.ModerationSetup.AdminRoles.Add(adminRole.Id);
                Context.Server.Save();
            }

            await SimpleEmbedAsync("Admin Role added.");
        }

        [Command("DelAdmin")]
        [Remarks("Delete an admin role")]
        public async Task DelAdminAsync(IRole adminRole = null)
        {
            if (adminRole == null)
            {
                await SimpleEmbedAsync("Please provide a role to add");
                return;
            }

            if (Context.Server.ModerationSetup.AdminRoles.Contains(adminRole.Id))
            {
                Context.Server.ModerationSetup.AdminRoles.Remove(adminRole.Id);
                Context.Server.Save();
                await SimpleEmbedAsync("Admin Role Removed.");
            }
            else
            {
                await SimpleEmbedAsync("That is not a admin role.");
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
                await SimpleEmbedAsync("Admin Role Removed.");
            }
            else
            {
                await SimpleEmbedAsync("That is not a admin role.");
            }
        }

        [Command("WipeModEvents")]
        [Summary("DELETES ALL Mod Events")]
        [Remarks("Note: Only use this if you want to completely remove the logs")]
        public Task WipeEventsAsync()
        {
            Context.Server.ModerationSetup.ModActions = new List<GuildModel.Moderation.ModEvent>();
            Context.Server.Save();
            return SimpleEmbedAsync("Success, all mod events deleted");
        }
    }
}
