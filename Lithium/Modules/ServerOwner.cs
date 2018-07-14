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
        private ConfigModel ConfigModel { get; }

        public ServerOwner(ConfigModel model)
        {
            ConfigModel = model;
        }

        [Command("SetModLog")]
        [Summary("Set the current channel as the mod log channel")]
        public Task SetModLogAsync()
        {
            Context.Server.ModerationSetup.Settings.ModLogChannel = Context.Channel.Id;
            Context.Server.Save();
            return SimpleEmbedAsync($"Moderation Log messages will be sent to {Context.Channel.Name}");
        }

        [Command("SetMute")]
        [Summary("Set the server's mute role")]
        public Task SetMuteAsync(IRole mute)
        {
            Context.Server.ModerationSetup.Settings.MutedRoleId = mute.Id;
            Context.Server.Save();
            return SimpleEmbedAsync($"Users will be given {mute.Mention} when muted.");
        }

        [Command("AddMod")]
        [Summary("Add a new moderator role")]
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
        [Summary("Delete a moderator role")]
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
        [Summary("Delete a moderator role by ID")]
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
        [Summary("Add a new administrator role")]
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
        [Summary("Delete an admin role")]
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
        [Summary("Delete an admin role by ID")]
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
        [Remarks("Only use this if you want to completely remove the logs")]
        public Task WipeEventsAsync()
        {
            Context.Server.ModerationSetup.ModActions = new List<GuildModel.Moderation.ModEvent>();
            Context.Server.Save();
            return SimpleEmbedAsync("Success, all mod events deleted");
        }

        [Command("SetPrefix")]
        [Summary("Set the current guild's Prefix")]
        public Task SetPrefixAsync([Remainder]string prefix = null)
        {
            var dict = PrefixDictionary.Load(ConfigModel.Prefix);
            dict.PrefixList.Remove(Context.Guild.Id);

            if (prefix != null)
            {
                dict.PrefixList.Add(Context.Guild.Id, prefix);
                dict.Save();
                return SimpleEmbedAsync($"Prefix is now `{prefix}`");
            }

            dict.Save();
            return SimpleEmbedAsync($"Prefix is now `{dict.DefaultPrefix}`");
        }
    }
}
