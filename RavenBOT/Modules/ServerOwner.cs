namespace RavenBOT.Modules
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Addons.PrefixService;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.ServerOwner)]
    public class ServerOwner : Base
    {
        private PrefixService PrefixService { get; }

        public ServerOwner(PrefixService prefix)
        {
            PrefixService = prefix;
        }

        [Command("SetModLog")]
        [Summary("Set the current channel as the mod log channel")]
        public Task SetModLogAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.ModerationSetup.Settings.ModLogChannel = Context.Channel.Id;
                        return SimpleEmbedAsync($"Moderation Log messages will be sent to {Context.Channel.Name}");
                    });
        }

        [Command("SetMute")]
        [Summary("Set the server's mute role")]
        public Task SetMuteAsync(IRole mute)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.ModerationSetup.Settings.MutedRoleId = mute.Id;
                        return SimpleEmbedAsync($"Users will be given {mute.Mention} when muted.");
                    });
        }

        [Command("AddMod")]
        [Summary("Add a new moderator role")]
        public Task AddModRoleAsync(IRole modRole = null)
        {
            if (modRole == null)
            {
                return SimpleEmbedAsync("Please provide a role to add");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}", g =>
                    {
                        if (!g.ModerationSetup.ModeratorRoles.Contains(modRole.Id))
                        {
                            g.ModerationSetup.ModeratorRoles.Add(modRole.Id);
                        }

                        return SimpleEmbedAsync("Moderator Role added.");
                    });
        }

        [Command("DelMod")]
        [Summary("Delete a moderator role")]
        public Task DelModRoleAsync(IRole modRole = null)
        {
            if (modRole == null)
            {
                return SimpleEmbedAsync("Please provide a role to add");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (g.ModerationSetup.ModeratorRoles.Contains(modRole.Id))
                        {
                            g.ModerationSetup.ModeratorRoles.Remove(modRole.Id);
                            return SimpleEmbedAsync("Moderator Role Removed.");
                        }

                        return SimpleEmbedAsync("That is not a moderator role.");
                    });
        }

        [Command("DelMod")]
        [Summary("Delete a moderator role by ID")]
        public Task DelModRoleAsync(ulong modRole)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (g.ModerationSetup.ModeratorRoles.Contains(modRole))
                        {
                            g.ModerationSetup.ModeratorRoles.Remove(modRole);
                            return SimpleEmbedAsync("Moderator Role Removed.");
                        }

                        return SimpleEmbedAsync("That is not a moderator role.");
                    });
        }

        [Command("AddAdmin")]
        [Summary("Add a new administrator role")]
        public Task AddAdminRoleAsync(IRole adminRole = null)
        {
            if (adminRole == null)
            {
                return SimpleEmbedAsync("Please provide a role to add");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (!g.ModerationSetup.AdminRoles.Contains(adminRole.Id))
                        {
                            g.ModerationSetup.AdminRoles.Add(adminRole.Id);
                        }

                        return SimpleEmbedAsync("Admin Role added.");
                    });
        }

        [Command("DelAdmin")]
        [Summary("Delete an admin role")]
        public Task DelAdminAsync(IRole adminRole = null)
        {
            if (adminRole == null)
            {
                return SimpleEmbedAsync("Please provide a role to add");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (g.ModerationSetup.AdminRoles.Contains(adminRole.Id))
                        {
                            g.ModerationSetup.AdminRoles.Remove(adminRole.Id);
                            return SimpleEmbedAsync("Admin Role Removed.");
                        }
                        else
                        {
                            return SimpleEmbedAsync("That is not a admin role.");
                        }
                    });
        }
        
        [Command("DelAdmin")]
        [Summary("Delete an admin role by ID")]
        public Task DelAdminAsync(ulong adminRole)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (g.ModerationSetup.AdminRoles.Contains(adminRole))
                        {
                            g.ModerationSetup.AdminRoles.Remove(adminRole);
                            return SimpleEmbedAsync("Admin Role Removed.");
                        }
                        else
                        {
                            return SimpleEmbedAsync("That is not a admin role.");
                        }
                    });
        }

        [Command("WipeModEvents")]
        [Summary("DELETES ALL Mod Events")]
        [Remarks("Only use this if you want to completely remove the logs")]
        public Task WipeEventsAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.ModerationSetup.ModActions = new List<GuildService.GuildModel.Moderation.ModEvent>();
                        return SimpleEmbedAsync("Success, all mod events deleted");
                    });
        }

        [Command("SetPrefix")]
        [Summary("Set the current guild's Prefix")]
        public Task SetPrefixAsync([Remainder]string prefix = null)
        {
            PrefixService.SetPrefix(Context.Guild.Id, prefix);
            return SimpleEmbedAsync($"Prefix set to {PrefixService.GetPrefix(Context.Guild.Id)}");
        }
    }
}
