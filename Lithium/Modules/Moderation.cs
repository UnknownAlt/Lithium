using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Preconditions;
using Lithium.Models;

namespace Lithium.Modules
{
    [RequireRole.RequireModerator]
    [Group("Mod")]
    public class Moderation : Base
    {
        [Command("Warn")]
        [Summary("Warn <@user>")]
        [Remarks("Warn the specified user")]
        public async Task WarnUser(IUser user, [Remainder] string reason = null)
        {
            Context.Server.ModerationSetup.Warns.Add(new GuildModel.Guild.Moderation.warn
            {
                userID = user.Id,
                modID = Context.User.Id,
                modname = Context.User.Username,
                reason = reason,
                username = user.Username
            });
            await ReplyAsync("User has been warned");
            Context.Server.Save();
        }
    }
}