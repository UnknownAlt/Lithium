using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Extensions;
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
        public async Task WarnUser(IGuildUser user, [Remainder] string reason = null)
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

            await SendEmbedAsync(new EmbedBuilder
            {
                Description = $"User: {user.Username}#{user.Discriminator}\n" +
                              $"UserID: {user.Id}\n" +
                              $"Mod: {Context.User.Username}#{Context.User.Discriminator}\n" +
                              $"Mod ID: {Context.User.Id}\n\n" +
                              "Reason:\n" +
                              $"{reason ?? "N/A"}"
            });
        }

        [Command("Kick")]
        [Summary("Kick <@user>")]
        [Remarks("Kick the specified user")]
        public async Task KickUser(IGuildUser user, [Remainder] string reason = null)
        {
            if (Permissions.CheckHeirachy(user, Context.Client))
            {
                await ReplyAsync("This user has higher permissions than me. I cannot perform this action on them");
                return;
            }

            if (user.GuildPermissions.Administrator || user.GuildPermissions.KickMembers)
            {
                await ReplyAsync("This user has admin or user kick permissions, therefore I cannot perform this action on them");
                return;
            }

            Context.Server.ModerationSetup.Kicks.Add(new GuildModel.Guild.Moderation.kick
            {
                userID = user.Id,
                modID = Context.User.Id,
                modname = Context.User.Username,
                reason = reason,
                username = user.Username
            });
            try
            {
                await user.KickAsync(reason);
                await ReplyAsync("User has been Kicked");
                Context.Server.Save();
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = $"{user.Username} has been kicked",
                    Description = $"User: {user.Username}#{user.Discriminator}\n" +
                              $"UserID: {user.Id}\n" +
                              $"Mod: {Context.User.Username}#{Context.User.Discriminator}\n" +
                              $"Mod ID: {Context.User.Id}\n\n" +
                              "Reason:\n" +
                              $"{reason ?? "N/A"}"
                });
            }
            catch
            {
                await ReplyAsync("User is unable to be kicked. ");
            }
        }

        [Command("Ban")]
        [Summary("Ban <@user>")]
        [Remarks("Ban the specified user")]
        public async Task BanUser(IGuildUser user, [Remainder] string reason = null)
        {
            if (Permissions.CheckHeirachy(user, Context.Client))
            {
                await ReplyAsync("This user has higher permissions than me. I cannot perform this action on them");
                return;
            }

            if (user.GuildPermissions.Administrator || user.GuildPermissions.BanMembers)
            {
                await ReplyAsync("This user has admin or user kick permissions, therefore I cannot perform this action on them");
                return;
            }

            Context.Server.ModerationSetup.Kicks.Add(new GuildModel.Guild.Moderation.kick
            {
                userID = user.Id,
                modID = Context.User.Id,
                modname = Context.User.Username,
                reason = reason,
                username = user.Username
            });
            try
            {
                await Context.Guild.AddBanAsync(user, 1, reason);
                await ReplyAsync("User has been Banned and messages from the last 24 hours have been cleared");
                Context.Server.Save();
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = $"{user.Username} has been banned",
                    Description = $"User: {user.Username}#{user.Discriminator}\n" +
                                  $"UserID: {user.Id}\n" +
                                  $"Mod: {Context.User.Username}#{Context.User.Discriminator}\n" +
                                  $"Mod ID: {Context.User.Id}\n\n" +
                                  "Reason:\n" +
                                  $"{reason ?? "N/A"}"
                });
            }
            catch
            {
                await ReplyAsync("User is unable to be Banned. ");
            }
        }
    }
}