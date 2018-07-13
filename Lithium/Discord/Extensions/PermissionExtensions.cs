namespace Lithium.Discord.Extensions
{
    using System;
    using System.Linq;

    using global::Discord;
    using global::Discord.WebSocket;

    using Lithium.Models;

    public static class PermissionExtensions
    {
        /// <summary>
        /// Attempts to cast the current SocketUser to a SocketGuildUser
        /// </summary>
        /// <param name="currentUser">
        /// The current user.
        /// </param>
        /// <returns>
        /// The <see cref="SocketGuildUser"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Throws if the user is unable to be cast
        /// </exception>
        public static SocketGuildUser CastToSocketGuildUser(this IUser currentUser)
        {
            if (currentUser is SocketGuildUser user)
            {
                return user;
            }

            throw new InvalidOperationException("User is unable to be cast to a SocketGuildUser");
        }

        public static bool IsAdminOrHigher(this SocketGuildUser currentUser, GuildModel.Moderation moderationSettings, DiscordSocketClient client)
        {
            if (currentUser.Roles.Any(r => moderationSettings.AdminRoles.Contains(r.Id)) || currentUser.GuildPermissions.Administrator)
            {
                return true;
            }

            if (currentUser.Id == currentUser.Guild.OwnerId)
            {
                return true;
            }

            if (currentUser.Id == client.GetApplicationInfoAsync().Result.Owner.Id)
            {
                return true;
            }

            return false;
        }

        public static bool IsModeratorOrHigher(this SocketGuildUser currentUser, GuildModel.Moderation moderationSettings, DiscordSocketClient client)
        {
            if (currentUser.Roles.Any(r => moderationSettings.ModeratorRoles.Contains(r.Id) || moderationSettings.AdminRoles.Contains(r.Id)))
            {
                return true;
            }

            if (currentUser.Id == currentUser.Guild.OwnerId)
            {
                return true;
            }

            if (currentUser.Id == client.GetApplicationInfoAsync().Result.Owner.Id)
            {
                return true;
            }

            return false;
        }

        public static bool IsHigherRankedThan(this SocketUser currentUser, SocketUser compareUser, SocketGuild guild)
        {
            var currentGuildUser = guild.GetUser(currentUser.Id);
            var compareGuildUser = guild.GetUser(compareUser.Id);
            if (currentUser == null || compareUser == null || currentGuildUser == null || compareGuildUser == null)
            {
                throw new NullReferenceException("Specified users cannot be null and must be a member of the target guild");
            }

            return IsHigherRankedThan(currentGuildUser, compareGuildUser);
        }

        public static bool IsHigherRankedThan(this SocketGuildUser currentUser, SocketGuildUser compareUser)
        {
            if (currentUser == null || compareUser == null)
            {
                throw new NullReferenceException("Specified users cannot be null.");
            }

            if (currentUser.Guild.Id != compareUser.Guild.Id)
            {
                throw new InvalidOperationException("SocketGuildUsers must be referencing the same guild");
            }

            if (currentUser.Roles.Max(x => x.Position) > compareUser.Roles.Max(x => x.Position))
            {
                return true;
            }

            return false;
        }

        public static bool IsHigherThan(this IRole currentRole, IRole compare)
        {
            if (currentRole.Position > compare.Position)
            {
                return true;
            }

            return false;
        }
    }
}
