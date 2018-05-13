using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;

namespace Lithium.Discord.Extensions
{
    public class Permissions
    {
        /// <summary>
        /// Returns true if the specified user has a higher position (rank) in the guild than the bot
        /// </summary>
        /// <param name="targetuser"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool CheckHeirachy(IGuildUser targetuser, IDiscordClient client)
        {
            var guild = targetuser.Guild as SocketGuild;
            var userrole = guild.Roles.OrderByDescending(x => x.Position).FirstOrDefault(x => targetuser.RoleIds.Contains(x.Id));
            if (userrole == null)
            {
                return false;
            }

            var gclient = guild.Users.First(x => x.Id == client.CurrentUser.Id);
            //Here we could also check if the bot has roles, however it should always have a role as upon joining the bot user will have a managed permissions role
            return userrole.Position > gclient.Roles.Max(x => x.Position);
        }
    }
}
