namespace RavenBOT.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Addons.Interactive;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Extensions;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Group("Permissions"), Alias("Perms")]
    public class CustomPermission : Base
    {
        private CommandService CommandService { get; }

        public CustomPermission(CommandService service)
        {
            CommandService = service;
        }

        [Command("AddModule")]
        [Summary("Add a new permission overwrite for the specified module")]
        
        public Task AddModulePermissionsAsync(DefaultPermissionLevel level, [Remainder]string moduleName)
        {
            if (level == DefaultPermissionLevel.BotOwner)
            {
                throw new Exception("Only the bot owner can set permissions at this level");
            }

            if (level == DefaultPermissionLevel.ServerOwner)
            {
                if (Context.User.Id != Context.Guild.OwnerId)
                {
                    throw new Exception("Only the server owner can set command permissions at this level");
                }
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (level == DefaultPermissionLevel.Administrators)
                        {
                            if (!Context.User.CastToSocketGuildUser().IsAdminOrHigher(g.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                            {
                                throw new Exception("Only administrator or higher can set commands to this permission level");
                            }
                        }

                        if (level == DefaultPermissionLevel.Moderators)
                        {
                            if (!Context.User.CastToSocketGuildUser().IsModeratorOrHigher(g.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                            {
                                throw new Exception("Only moderators or higher can set commands to this permission level");
                            }
                        }

                        var search = CommandService.Modules.FirstOrDefault(m => (string.IsNullOrWhiteSpace(m.Aliases.FirstOrDefault()) ? m.Name : m.Aliases.FirstOrDefault()).Equals(moduleName, StringComparison.OrdinalIgnoreCase));
                        if (search == null)
                        {
                            throw new Exception("Module not found");
                        }

                        // TODO Filter out duplicates
                        g.CustomAccess.CustomizedPermission.Add(new GuildService.GuildModel.CommandAccess.CustomPermission { IsCommand = false, Name = string.IsNullOrWhiteSpace(search.Aliases.FirstOrDefault()) ? search.Name : search.Aliases.First(), Setting = level });

                        return SimpleEmbedAsync("Added Overwrite");
                    });
        }

        [Command("AddCommand")]
        [Summary("Add a new permissions overwrite for the specified command")]
        public Task AddCommandPermissionsAsync(DefaultPermissionLevel level, [Remainder]string commandName)
        {
            if (level == DefaultPermissionLevel.BotOwner)
            {
                throw new Exception("Only the bot owner can set permissions at this level");
            }

            if (level == DefaultPermissionLevel.ServerOwner)
            {
                if (Context.User.Id != Context.Guild.OwnerId)
                {
                    throw new Exception("Only the server owner can set command permissions at this level");
                }
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (level == DefaultPermissionLevel.Administrators)
                        {
                            if (!Context.User.CastToSocketGuildUser().IsAdminOrHigher(g.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                            {
                                throw new Exception("Only administrator or higher can set commands to this permission level");
                            }
                        }

                        if (level == DefaultPermissionLevel.Moderators)
                        {
                            if (!Context.User.CastToSocketGuildUser().IsModeratorOrHigher(g.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                            {
                                throw new Exception("Only moderators or higher can set commands to this permission level");
                            }
                        }

                        var search = CommandService.Search(commandName);
                        if (!search.IsSuccess)
                        {
                            throw new Exception("Command not found");
                        }

                        var commandResult = search.Commands.First();

                        g.CustomAccess.CustomizedPermission.Add(new GuildService.GuildModel.CommandAccess.CustomPermission { IsCommand = true, Name = string.IsNullOrWhiteSpace(commandResult.Command.Aliases.FirstOrDefault()) ? commandResult.Command.Name : commandResult.Command.Aliases.FirstOrDefault(), Setting = level });

                        return SimpleEmbedAsync("Added Overwrite");
                    });
        }

        [Command("Remove")]
        [Summary("Remove a permission overwrite for the specified module or command")]
        public Task RemovePermissionsAsync([Remainder]string commandOrModuleName)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        var match = g.CustomAccess.CustomizedPermission.FirstOrDefault(p => p.Name.Equals(commandOrModuleName, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            g.CustomAccess.CustomizedPermission.Remove(match);
                            return SimpleEmbedAsync("Success, removed.");
                        }

                        return SimpleEmbedAsync("Invalid command name");
                    });
        }

        [Command("View")]
        [Summary("View all custom permission overwrites")]
        public async Task ViewCustomAsync()
        {
            var g = await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}");
            if (!g.CustomAccess.CustomizedPermission.Any())
            {
                await SimpleEmbedAsync("There are no custom permissions set yet.");
                return;
            }

            var pages = new List<PaginatedMessage.Page>();
            foreach (var group in g.CustomAccess.CustomizedPermission.SplitList(10))
            {
                pages.Add(new PaginatedMessage.Page
                              {
                                  Description = string.Join("\n", group.Select(o => $"{o.Name} - {o.Setting}"))
                              });
            }

            await PagedReplyAsync(new PaginatedMessage { Pages = pages }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }

        [Command("Clear")]
        [Summary("Clear all permission overwrites")]
        public Task ClearCustomAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (!g.CustomAccess.CustomizedPermission.Any())
                        {
                            return SimpleEmbedAsync("There are no custom permissions set.");
                        }

                        g.CustomAccess.CustomizedPermission = new List<GuildService.GuildModel.CommandAccess.CustomPermission>();
                        return SimpleEmbedAsync("Permissions cleared");
                    });
        }
    }
}
