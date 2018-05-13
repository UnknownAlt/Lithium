using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Handlers;

namespace Lithium.Discord.Preconditions
{
    public class RequireRole
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class RequireModerator : PreconditionAttribute
        {
            private readonly bool _allowAdministrator;
            private readonly bool _allowAdministratorRole;

            public RequireModerator(bool AllowAdminPermission = true, bool allowAdministratorRole = true)
            {
                _allowAdministrator = AllowAdminPermission;
                _allowAdministratorRole = allowAdministratorRole;
            }

            public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
                IServiceProvider services)
            {
                if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromError("User is not a Moderator or an Admin and Command is only accessible within a guild"));

                var own = context.Client.GetApplicationInfoAsync();
                if (own.Result.Owner.Id == context.User.Id)
                    return Task.FromResult(PreconditionResult.FromSuccess());

                var guser = (IGuildUser) context.User;
                var guild = DatabaseHandler.GetGuild(context.Guild.Id);
                if (_allowAdministrator && guser.GuildPermissions.Administrator)
                    return Task.FromResult(PreconditionResult.FromSuccess());

                if (_allowAdministratorRole && guild.ModerationSetup.AdminRoles.Any(x => guser.RoleIds.Contains(x)))
                    return Task.FromResult(PreconditionResult.FromSuccess());

                if (guild.ModerationSetup.ModeratorRoles.Any(x => guser.RoleIds.Contains(x)))
                    return Task.FromResult(PreconditionResult.FromSuccess());

                return Task.FromResult(PreconditionResult.FromError("User is Not A Moderator or an Admin!"));
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class RequireAdmin : PreconditionAttribute
        {
            private readonly bool _allowAdministrator;

            public RequireAdmin(bool AllowAdminPermission = true)
            {
                _allowAdministrator = AllowAdminPermission;
            }

            public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
                IServiceProvider services)
            {
                if (context.Channel is IDMChannel) return Task.FromResult(PreconditionResult.FromError("User is not an Admin and Command is only accessible within a guild"));

                var own = context.Client.GetApplicationInfoAsync();
                if (own.Result.Owner.Id == context.User.Id)
                    return Task.FromResult(PreconditionResult.FromSuccess());

                var guser = (IGuildUser) context.User;
                var guild = DatabaseHandler.GetGuild(context.Guild.Id);
                if (_allowAdministrator && guser.GuildPermissions.Administrator)
                    return Task.FromResult(PreconditionResult.FromSuccess());


                if (guild.ModerationSetup.AdminRoles.Any(x => guser.RoleIds.Contains(x)))
                    return Task.FromResult(PreconditionResult.FromSuccess());

                return Task.FromResult(PreconditionResult.FromError("User does not have an Administrator role!"));
            }
        }
    }
}