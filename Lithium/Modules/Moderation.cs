using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Contexts.Paginator;
using Lithium.Discord.Extensions;
using Lithium.Discord.Preconditions;
using Lithium.Models;
using Raven.Client.Documents.Smuggler;
using Serilog.Parsing;

namespace Lithium.Modules
{
    [RequireRole.RequireModerator]
    [Group("Mod")]
    public class Moderation : Base
    {

        [Command("Warn")]
        [Summary("Warn <@user> <reason>")]
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
        [Summary("Kick <@user> <reason>")]
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
        [Summary("Ban <@user> <reason>")]
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
                await ReplyAsync("This user has admin or user ban permissions, therefore I cannot perform this action on them");
                return;
            }

            Context.Server.ModerationSetup.Bans.Add(new GuildModel.Guild.Moderation.ban
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

        [Command("SoftBan")]
        [Summary("SoftBan <@user> <hours> <reason>")]
        [Remarks("Ban the specified user for the specified amount of hours")]
        public async Task SoftBanUser(IGuildUser user, int hours, [Remainder] string reason = null)
        {
            if (Permissions.CheckHeirachy(user, Context.Client))
            {
                await ReplyAsync("This user has higher permissions than me. I cannot perform this action on them");
                return;
            }

            if (user.GuildPermissions.Administrator || user.GuildPermissions.BanMembers)
            {
                await ReplyAsync("This user has admin or user ban permissions, therefore I cannot perform this action on them");
                return;
            }

            Context.Server.ModerationSetup.Bans.Add(new GuildModel.Guild.Moderation.ban
            {
                userID = user.Id,
                modID = Context.User.Id,
                modname = Context.User.Username,
                reason = reason,
                username = user.Username,
                ExpiryDate = DateTime.UtcNow + TimeSpan.FromHours(hours),
                Expires = true
            });
            try
            {
                await Context.Guild.AddBanAsync(user, 1, reason);
                await ReplyAsync("User has been Soft Banned and messages from the last 24 hours have been cleared");
                Context.Server.Save();
                await SendEmbedAsync(new EmbedBuilder
                {
                    Title = $"{user.Username} has been soft banned",
                    Description = $"User: {user.Username}#{user.Discriminator}\n" +
                                  $"UserID: {user.Id}\n" +
                                  $"Mod: {Context.User.Username}#{Context.User.Discriminator}\n" +
                                  $"Mod ID: {Context.User.Id}\n" +
                                  $"Expires After: {hours} hours\n\n" +
                                  "Reason:\n" +
                                  $"{reason ?? "N/A"}"
                });
            }
            catch
            {
                await ReplyAsync("User is unable to be Banned. ");
            }
        }


        [Command("Warns")]
        [Summary("Warns")]
        [Remarks("List all logged server warnings")]
        public async Task Warns()
        {
            var pages = new List<PaginatedMessage.Page>();
            var desc = "";
            foreach (var warngroup in Context.Server.ModerationSetup.Warns.GroupBy(x => x.userID).ToList())
            {
                var first = warngroup.FirstOrDefault();
                var user = await Context.Client.GetUserAsync(first.userID);

                var dstr = $"**{user.Username ?? first.username} `[{first.userID}]`**\n";
                foreach (var warn in warngroup)
                {
                    dstr += $"Warned By: {(await Context.Client.GetUserAsync(first.modID)).Username ?? warn.modname} `[{warn.modID}]`\n" +
                            $"Reason: {warn.reason}\n";
                }
                dstr += $"-Count: {warngroup.Count()}\n";
                desc += dstr;
                if (desc.Length > 800)
                {
                    if (desc.Length > 1024)
                    {
                        desc = desc.Substring(0, 1023);
                    }
                    pages.Add(new PaginatedMessage.Page
                    {
                        description = desc
                    });
                    desc = "";
                }
            }
            pages.Add(new PaginatedMessage.Page
            {
                description = desc
            });
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Title = "Warnings",
                Color = Color.DarkPurple
            };
            await PagedReplyAsync(pager);
        }

        [Command("Kicks")]
        [Summary("Kicks")]
        [Remarks("List all logged server Kicks")]
        public async Task Kicks()
        {
            var pages = new List<PaginatedMessage.Page>();
            var desc = "";
            foreach (var kickgroup in Context.Server.ModerationSetup.Kicks.GroupBy(x => x.userID).ToList())
            {
                var first = kickgroup.FirstOrDefault();
                var user = await Context.Client.GetUserAsync(first.userID);

                var dstr = $"**{user.Username ?? first.username} `[{first.userID}]`**\n";
                foreach (var kick in kickgroup)
                {
                    dstr += $"Kicked By: {(await Context.Client.GetUserAsync(first.modID)).Username ?? kick.modname} `[{kick.modID}]`\n" +
                            $"Reason: {kick.reason}\n";
                }
                dstr += $"-Count: {kickgroup.Count()}\n";
                desc += dstr;
                if (desc.Length > 800)
                {
                    if (desc.Length > 1024)
                    {
                        desc = desc.Substring(0, 1023);
                    }
                    pages.Add(new PaginatedMessage.Page
                    {
                        description = desc
                    });
                    desc = "";
                }
            }
            pages.Add(new PaginatedMessage.Page
            {
                description = desc
            });
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Title = "Kicks",
                Color = Color.DarkMagenta
            };
            await PagedReplyAsync(pager);
        }

        [Command("Bans")]
        [Summary("Bans")]
        [Remarks("List all logged server Bans")]
        public async Task Bans()
        {
            var pages = new List<PaginatedMessage.Page>();
            var desc = "";
            foreach (var bangroup in Context.Server.ModerationSetup.Bans.GroupBy(x => x.userID).ToList())
            {
                var first = bangroup.FirstOrDefault();
                var user = await Context.Client.GetUserAsync(first.userID);

                var dstr = $"**{user.Username ?? first.username} `[{first.userID}]`**\n";
                foreach (var ban in bangroup)
                {
                    dstr += $"Banned By: {(await Context.Client.GetUserAsync(first.modID)).Username ?? ban.modname} `[{ban.modID}]`\n" +
                            $"SoftBan: {(ban.Expires ? $"{(ban.ExpiryDate - DateTime.UtcNow).TotalMinutes} Minutes Remaining" : "Permanent Ban")}" +
                            $"Reason: {ban.reason}\n";
                }
                desc += dstr;
                if (desc.Length > 800)
                {
                    if (desc.Length > 1024)
                    {
                        desc = desc.Substring(0, 1023);
                    }
                    pages.Add(new PaginatedMessage.Page
                    {
                        description = desc
                    });
                    desc = "";
                }
            }
            pages.Add(new PaginatedMessage.Page
            {
                description = desc
            });
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Title = "Bans",
                Color = Color.DarkRed
            };
            await PagedReplyAsync(pager);
        }
    }
}