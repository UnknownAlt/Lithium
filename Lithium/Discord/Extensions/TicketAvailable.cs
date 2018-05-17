using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;
using Lithium.Models;

namespace Lithium.Discord.Extensions
{
    public class TicketAvailable
    {
        /// <summary>
        /// This checks whether or not a user is allowed to use the bot's ticketing system.
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="User"></param>
        /// <returns></returns>
        public bool CanCreate(GuildModel.Guild.ticketing.tsettings Settings, IGuildUser User)
        {
            if (Settings.allowAnyUserToCreate)
            {
                return true;
            }

            if (User.RoleIds.Any(x => Settings.AllowedCreationRoles.Contains(x)))
            {
                return true;
            }

            return false;
        }
    }
}
