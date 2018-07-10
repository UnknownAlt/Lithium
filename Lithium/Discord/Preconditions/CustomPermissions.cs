namespace Lithium.Discord.Preconditions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Handlers;
    using Lithium.Models;

    using Microsoft.Extensions.DependencyInjection;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CustomPermissions : PreconditionAttribute
    {
        private readonly bool defaultAdmin;
        private readonly bool defaultMod;

        public CustomPermissions(bool defaultAdministrator = false, bool defaultModerator = false)
        {
            defaultAdmin = defaultAdministrator;
            defaultMod = defaultModerator;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel is IDMChannel)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a Guild command"));
            }

            if (context.Client.GetApplicationInfoAsync().Result.Owner.Id == context.User.Id || context.Guild.OwnerId == context.User.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            var server = services.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, context.Guild.Id.ToString());

            var gUser = context.User as IGuildUser;

            // At this point, all users are registered, not the server owner and not the bot owner
            if (server.CustomAccess.CustomizedPermission.Any())
            {
                var match = server.CustomAccess.CustomizedPermission.FirstOrDefault(x => string.Equals(command.Name, x.Name, StringComparison.CurrentCultureIgnoreCase));
                if (match != null)
                {
                    return CheckPermsAsync(match.Setting, gUser, server);
                }
            }

            if (!defaultMod && !defaultAdmin)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if ((gUser.RoleIds.Any(x => server.ModerationSetup.ModeratorRoles.Contains(x)) || gUser.RoleIds.Any(x => server.ModerationSetup.AdminRoles.Contains(x))) && defaultMod)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (gUser.RoleIds.Any(x => server.ModerationSetup.AdminRoles.Contains(x)) && defaultAdmin)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError($"This command is {(defaultMod ? "Moderator+" : string.Empty)}{(defaultAdmin ? "Admin+" : string.Empty)} Only"));
        }

        public Task<PreconditionResult> CheckPermsAsync(GuildModel.CommandAccess.CustomPermission.AccessType setting, IGuildUser gUser, GuildModel server)
        {
            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.All)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.Moderator && gUser.RoleIds.Any(x => server.ModerationSetup.ModeratorRoles.Contains(x)))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.Moderator && gUser.RoleIds.Any(x => server.ModerationSetup.AdminRoles.Contains(x)))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.Moderator)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a server Moderator only command"));
            }

            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.Admin && gUser.RoleIds.Any(x => server.ModerationSetup.AdminRoles.Contains(x)))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.Admin)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a server Admin only command"));
            }

            if (setting == GuildModel.CommandAccess.CustomPermission.AccessType.ServerOwner)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a server Owner only command"));
            }

            return Task.FromResult(PreconditionResult.FromError("Unknown error in permissions"));
        }
    }
}