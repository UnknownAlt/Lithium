using System;
using System.Collections.Generic;
using System.Text;

namespace Lithium.Modules
{
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

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

            if (level == DefaultPermissionLevel.Administrators)
            {
                if (!Context.User.CastToSocketGuildUser().IsAdminOrHigher(Context.Server.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                {
                    throw new Exception("Only administrator or higher can set commands to this permission level");
                }
            }

            if (level == DefaultPermissionLevel.Moderators)
            {
                if (!Context.User.CastToSocketGuildUser().IsModeratorOrHigher(Context.Server.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                {
                    throw new Exception("Only moderators or higher can set commands to this permission level");
                }
            }

            var search = CommandService.Modules.FirstOrDefault(m => (string.IsNullOrWhiteSpace(m.Aliases.FirstOrDefault()) ? m.Name : m.Aliases.FirstOrDefault()).Equals(moduleName, StringComparison.OrdinalIgnoreCase));
            if (search == null)
            {
                throw new Exception("Module not found");
            }

            Context.Server.CustomAccess.CustomizedPermission.Add(new GuildModel.CommandAccess.CustomPermission
                                                                     {
                                                                         IsCommand = false,
                                                                         Name = string.IsNullOrWhiteSpace(search.Aliases.FirstOrDefault()) ? search.Name : search.Aliases.First(),
                                                                         Setting = level
                                                                     });

            Context.Server.Save();

            return SimpleEmbedAsync("Added Overwrite");
        }

        [Command("AddCommand")]
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

            if (level == DefaultPermissionLevel.Administrators)
            {
                if (!Context.User.CastToSocketGuildUser().IsAdminOrHigher(Context.Server.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
                {
                    throw new Exception("Only administrator or higher can set commands to this permission level");
                }
            }

            if (level == DefaultPermissionLevel.Moderators)
            {
                if (!Context.User.CastToSocketGuildUser().IsModeratorOrHigher(Context.Server.ModerationSetup, Context.Client.GetShardFor(Context.Guild)))
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
            

            Context.Server.CustomAccess.CustomizedPermission.Add(new GuildModel.CommandAccess.CustomPermission
                                                                     {
                                                                         IsCommand = true,
                                                                         Name = string.IsNullOrWhiteSpace(commandResult.Command.Aliases.FirstOrDefault()) ? commandResult.Command.Name : commandResult.Command.Aliases.FirstOrDefault(),
                                                                         Setting = level
                                                                     });

            Context.Server.Save();

            return SimpleEmbedAsync("Added Overwrite");
        }

        [Command("Remove")]
        public Task RemovePermissionsAsync([Remainder]string commandOrModuleName)
        {
            var match = Context.Server.CustomAccess.CustomizedPermission.FirstOrDefault(p => p.Name.Equals(commandOrModuleName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                Context.Server.CustomAccess.CustomizedPermission.Remove(match);
                Context.Server.Save();
                return SimpleEmbedAsync("Success, removed.");

            }

            return SimpleEmbedAsync("Invalid command name");
        }

        [Command("View")]
        public Task ViewCustomAsync()
        {
            if (!Context.Server.CustomAccess.CustomizedPermission.Any())
            {
                return SimpleEmbedAsync("There are no custom permissions set yet.");
            }

            var pages = new List<PaginatedMessage.Page>();
            foreach (var group in Context.Server.CustomAccess.CustomizedPermission.SplitList(10))
            {
                pages.Add(new PaginatedMessage.Page
                              {
                                  Description = string.Join("\n", group.Select(o => $"{o.Name} - {o.Setting}"))
                              });
            }

            return PagedReplyAsync(new PaginatedMessage { Pages = pages }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }

        [Command("Clear")]
        public Task ClearCustomAsync()
        {
            if (!Context.Server.CustomAccess.CustomizedPermission.Any())
            {
                return SimpleEmbedAsync("There are no custom permissions set.");
            }

            Context.Server.CustomAccess.CustomizedPermission = new List<GuildModel.CommandAccess.CustomPermission>();
            Context.Server.Save();
            return SimpleEmbedAsync("Permissions cleared");
        }
    }
}
