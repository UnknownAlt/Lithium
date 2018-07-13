namespace Lithium.Discord.Preconditions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Extensions;
    using Lithium.Handlers;
    using Lithium.Models;

    using Microsoft.Extensions.DependencyInjection;

    public enum DefaultPermissionLevel
    {
        AllUsers,
        Moderators,
        Administrators,
        ServerOwner,
        BotOwner
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CustomPermissions : PreconditionAttribute
    {
        private DefaultPermissionLevel defaultPermissionLevel;

        public CustomPermissions(DefaultPermissionLevel defaultPermission)
        {
            defaultPermissionLevel = defaultPermission;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext iContext, CommandInfo command, IServiceProvider services)
        {
            var context = iContext as SocketCommandContext;
            if (context.Channel is IDMChannel)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a Guild command"));
            }

            var server = services.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, context.Guild.Id.ToString());

            // At this point, all users are registered, not the server owner and not the bot owner
            if (server.CustomAccess.CustomizedPermission.Any())
            {
                var match = server.CustomAccess.CustomizedPermission.FirstOrDefault(x => string.Equals(command.Name, x.Name, StringComparison.CurrentCultureIgnoreCase));
                if (match != null)
                {
                    defaultPermissionLevel = match.Setting;
                }
            }

            if (defaultPermissionLevel == DefaultPermissionLevel.AllUsers)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (defaultPermissionLevel == DefaultPermissionLevel.Moderators)
            {
                if (context.User.CastToSocketGuildUser().IsModeratorOrHigher(server.ModerationSetup, context.Client))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.Administrators)
            {
                if (context.User.CastToSocketGuildUser().IsAdminOrHigher(server.ModerationSetup, context.Client))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.ServerOwner)
            {
                if (context.User.Id == context.Guild.OwnerId
                    || context.Client.GetApplicationInfoAsync().Result.Owner.Id == context.User.Id)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.BotOwner)
            {
                if (context.Client.GetApplicationInfoAsync().Result.Owner.Id == context.User.Id)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }

            return Task.FromResult(PreconditionResult.FromError($"You do not have the access level of {defaultPermissionLevel}, which is required to run this command"));
        }
    }
}