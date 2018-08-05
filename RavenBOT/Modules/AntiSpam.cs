namespace RavenBOT.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class AutoMod : Base
    {
        [Command("NoSpam")]
        [Summary("Toggle Auto Removal of spam messages")]
        public Task NoSpamAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.AntiSpam.NoSpam = !g.AntiSpam.AntiSpam.NoSpam;
                        return ReplyAsync($"NoSpam: {g.AntiSpam.AntiSpam.NoSpam}");
                    });
        }

        [Command("NoSpamCount")]
        [Summary("Set amount of messages required before noSpam detects it")]
        public Task NoSpamCountAsync(int messages = 3)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.AntiSpam.RateLimiting.MessageCount = messages;
                        return ReplyAsync($"if more than {messages} messages are sent within {g.AntiSpam.AntiSpam.RateLimiting.TimePeriod.TotalSeconds} seconds, they will be counted as spam and not be able to send messages for another {g.AntiSpam.AntiSpam.RateLimiting.BlockPeriod.TotalSeconds} seconds");
                    });
        }

        [Command("NoSpamTimeout")]
        [Summary("Set amount of seconds per message-limit to detect spam")]
        public Task NoSpamTimeAsync(int seconds = 5)
        {
            if (seconds > 30 || seconds < 2)
            {
                throw new Exception("Seconds must be less than 30 and greater than 2");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.AntiSpam.RateLimiting.TimePeriod = TimeSpan.FromSeconds(seconds);
                        return ReplyAsync($"if more than {g.AntiSpam.AntiSpam.RateLimiting.MessageCount} messages are sent within {seconds} seconds, they will be counted as spam and not be able to send messages for another {g.AntiSpam.AntiSpam.RateLimiting.BlockPeriod.TotalSeconds} seconds");
                    });
        }

        [Command("NoSpamBlock")]
        [Summary("Set amount of time users must wait before sending messages again")]
        public Task NoSpamBlockAsync(int seconds = 5)
        {
            if (seconds > 30 || seconds < 2)
            {
                throw new Exception("Seconds must be less than 30 and greater than 2");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.AntiSpam.RateLimiting.BlockPeriod = TimeSpan.FromSeconds(seconds);
                        return ReplyAsync($"if more than {g.AntiSpam.AntiSpam.RateLimiting.MessageCount} messages are sent within {g.AntiSpam.AntiSpam.RateLimiting.TimePeriod} seconds, they will be counted as spam and not be able to send messages for another {seconds} seconds");
                    });
        }

        [Command("NoInvite")]
        [Summary("Disable the posting of discord invite links in the server")]
        public Task NoInviteAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.Advertising.Invite = !g.AntiSpam.Advertising.Invite;
                        return ReplyAsync($"NoInvite: {g.AntiSpam.Advertising.Invite}");
                    });
        }

        [Command("NoInviteMessage")]
        [Summary("set the no invites message")]
        public Task NoInviteAsync([Remainder] string message = null)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.Advertising.Response = message;
                        return ReplyAsync("The No Invites message is now:\n" +
                                          $"{g.AntiSpam.Advertising.Response ?? "Default"}");
                    });
        }

        [Command("NoMentionAll")]
        [Summary("Disable the use of @everyone and @here for users")]
        public Task NoMentionAllAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.Mention.MentionAll = !g.AntiSpam.Mention.MentionAll;
                        return ReplyAsync($"NoMentionAll: {g.AntiSpam.Mention.MentionAll}");
                    });
        }

        [Command("NoMentionMessage")]
        [Summary("Set the No Mention Message response")]
        public Task NoMentionAllAsync([Remainder] string message = null)
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.Mention.MentionAllResponse = message;
                        return ReplyAsync($"No Mention Message: {message ?? "N/A"}");
                    });
        }

        [Command("NoMassMention")]
        [Summary("Toggle auto-deletion of messages with 5+ role or user mentions")]
        public Task NoMassMentionAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.Mention.RemoveMassMention = !g.AntiSpam.Mention.RemoveMassMention;

                        return ReplyAsync($"NoMassMention: {g.AntiSpam.Mention.RemoveMassMention}");
                    });
        }

        [Command("NoIPs")]
        [Summary("Toggle auto-deletion of messages containing valid IP addresses")]
        public Task NoIPsAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                    g =>
                        {
                            g.AntiSpam.Privacy.RemoveIPs = !g.AntiSpam.Privacy.RemoveIPs;

                            return ReplyAsync($"No IP Addresses: {g.AntiSpam.Privacy.RemoveIPs}");
                        });
        }

        [Command("NoToxicity")]
        [Summary("Toggle auto-deletion of messages that are too toxic")]
        public Task NoToxicityAsync(int threshold = 999)
        {
            if (threshold == 999 || threshold < 50 || threshold > 99)
            {
                throw new Exception("Pick a threshold between 50 and 99");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                    g =>
                        {
                            g.AntiSpam.Toxicity.ToxicityThreshold = threshold;
                            g.AntiSpam.Toxicity.UsePerspective = !g.AntiSpam.Toxicity.UsePerspective;
                            return ReplyAsync($"Remove Toxic Messages: {g.AntiSpam.Toxicity.UsePerspective}\n" +
                                              $"Threshold: {threshold}");
                        });
        }

        [Command("HideAutoWarnDelay")]
        [Summary("Toggle the deletion of warn messages in channel after x sec.")]
        public Task HideAWarnDelAsync(int seconds = 5)
        {
            TimeSpan? time = TimeSpan.FromSeconds(seconds);
            if (seconds == 0)
            {
                time = null;
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                    g =>
                        {
                            g.ModerationSetup.Settings.AutoHideDelay = time;
                            return ReplyAsync($"Auto Delete Warning Message responses after {seconds} seconds: {time.HasValue}");
                        });
        }

        [Command("ignore")]
        [Summary("choose a role to ignore when using AntiSpam commands")]
        public Task IgnoreRoleAsync(string selection, IRole role = null)
        {
            if (role == null)
            {
                return IgnoreRoleAsync();
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}",
                    g =>
                        {
                            var split = selection.Split(',');
                            g.AntiSpam.IgnoreList.TryGetValue(role.Id, out var ignore);
                            var addRole = false;
                            if (ignore == null)
                            {
                                ignore = new GuildService.GuildModel.AntiSpamSetup.IgnoreRole();
                                addRole = true;
                            }

                            if (int.TryParse(split[0], out var i))
                            {
                                if (i == 0)
                                {
                                    g.AntiSpam.IgnoreList.Remove(role.Id);
                                    return SimpleEmbedAsync("Success, Role has been removed form the ignore list");
                                }
                            }
                            else
                            {
                                return SimpleEmbedAsync("Input Error!");
                            }

                            foreach (var s in split)
                            {
                                if (int.TryParse(s, out var i1))
                                {
                                    if (i1 < 1 || i1 > 6)
                                    {
                                        return SimpleEmbedAsync($"Invalid Input {s}\n" + "only 1-6 are accepted.");
                                    }

                                    switch (i1)
                                    {
                                        case 1:
                                            ignore.AntiSpam = true;
                                            break;
                                        case 2:
                                            ignore.Blacklist = true;
                                            break;
                                        case 3:
                                            ignore.Mention = true;
                                            break;
                                        case 4:
                                            ignore.Advertising = true;
                                            break;
                                        case 5:
                                            ignore.Privacy = true;
                                            break;
                                        case 6:
                                            ignore.Toxicity = true;
                                            break;
                                    }
                                }
                                else
                                {
                                    return SimpleEmbedAsync($"Invalid Input {s}");
                                }
                            }

                            if (addRole)
                            {
                                g.AntiSpam.IgnoreList.Add(role.Id, ignore);
                            }

                            return SimpleEmbedAsync(
                                $"{role.Mention}\n" + "__Ignore AntiSpam Detections__\n" + $"Bypass AntiSpam: {ignore.AntiSpam}\n"
                                + $"Bypass Blacklist: {ignore.Blacklist}\n"
                                + $"Bypass Mention Everyone and 5+ Role Mentions: {ignore.Mention}\n"
                                + $"Bypass Invite Link Removal: {ignore.Advertising}\n" + $"Bypass IP Removal: {ignore.Privacy}\n"
                                + $"Bypass Toxicity Check: {ignore.Toxicity}");
                        });
        }

        [Command("ignore")]
        [Summary("ignore role setup information")]
        public Task IgnoreRoleAsync()
        {
            return ReplyAsync("", false, new EmbedBuilder
            {
                Description =
                                                     "You can select roles to ignore from all spam type checks in this module using the ignore command.\n" +
                                                     "__Key__\n" +
                                                     "`1` - AntiSpam\n" +
                                                     "`2` - Blacklist\n" +
                                                     "`3` - Mention\n" +
                                                     "`4` - Invite\n" +
                                                     "`5` - IP Addresses\n" +
                                                     "`6` - Toxicity\n\n" +
                                                     "__usage__\n" +
                                                     "`ignore 1 @role` - this allows the role to spam without being limited/removed\n" +
                                                     "You can use commas to use multiple Settings on the same role.\n" +
                                                     "`ignore 1,2,3 @role` - this allows the role to spam, use blacklisted words and bypass mention filtering without being removed\n" +
                                                     "`ignore 0 @role` - resets the ignore config and will add all limits back to the role"
            }.Build());
        }

        [Command("IgnoreList")]
        [Summary("list ignore-roles and their setup")]
        public async Task IgnoreListAsync()
        {
            var g = await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}");
            var list = g.AntiSpam.IgnoreList.Select(x => $"{Context.Guild.GetRole(x.Key)?.Mention ?? "N/A"} Invites: {x.Value.Advertising} AntiSpam: {x.Value.AntiSpam} Blacklist: {x.Value.Blacklist} Mention: {x.Value.Mention} IP: {x.Value.Privacy} Toxicity: {x.Value.Toxicity}");
            await ReplyAsync(new EmbedBuilder
            {
                Description = string.Join("\n", list)
            });
        }

        [Command("WarnSpammers")]
        [Alias("AutoWarn")]
        [Summary("Toggle Auto-Warning of people detected by any of the Anti Spam methods")]
        public async Task WarnSpammersAsync([Remainder] string selection)
        {
            var split = selection.Split(',');

            if (int.TryParse(split[0], out var i))
            {
                await Context.DBService.ModifyAsync<GuildService.GuildModel>(
                    $"{Context.Guild.Id}",
                    async g =>
                        {
                            if (i == 0)
                            {
                                g.AntiSpam.AntiSpam.WarnOnDetection = false;
                                g.AntiSpam.Blacklist.WarnOnDetection = false;
                                g.AntiSpam.Advertising.WarnOnDetection = false;
                                g.AntiSpam.Mention.WarnOnDetection = false;
                                g.AntiSpam.Toxicity.WarnOnDetection = false;
                                g.AntiSpam.Privacy.WarnOnDetection = false;
                                await SimpleEmbedAsync("Success, All have been reset.");
                            }
                            else
                            {
                                foreach (var s in split)
                                {
                                    if (int.TryParse(s, out var i1))
                                    {
                                        if (i1 < 1 || i1 > 6)
                                        {
                                            await SimpleEmbedAsync($"Invalid Input {s}\n" + "only 1-6 are accepted.");
                                            return;
                                        }

                                        switch (i1)
                                        {
                                            case 1:
                                                g.AntiSpam.AntiSpam.WarnOnDetection = true;
                                                break;
                                            case 2:
                                                g.AntiSpam.Blacklist.WarnOnDetection = true;
                                                break;
                                            case 3:
                                                g.AntiSpam.Mention.WarnOnDetection = true;
                                                break;
                                            case 4:
                                                g.AntiSpam.Advertising.WarnOnDetection = true;
                                                break;
                                            case 5:
                                                g.AntiSpam.Privacy.WarnOnDetection = true;
                                                break;
                                            case 6:
                                                g.AntiSpam.Toxicity.WarnOnDetection = true;
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        await SimpleEmbedAsync($"Invalid Input {s}");
                                        return;
                                    }
                                }
                            }

                            var embed = new EmbedBuilder { Description = "__AutoMod Detections__\n" + $"Warn on Anti Spam: {g.AntiSpam.AntiSpam.WarnOnDetection}\n" + $"Warn on Blacklist: {g.AntiSpam.Blacklist.WarnOnDetection}\n" + $"Warn on Mention Everyone and 5+ Role Mentions: {g.AntiSpam.Mention.WarnOnDetection}\n" + $"Warn on Invite Link Removal: {g.AntiSpam.Advertising.WarnOnDetection}\n" + $"Warn on IP Removal: {g.AntiSpam.Privacy.WarnOnDetection}\n" + $"Warn on Toxicity Check: {g.AntiSpam.Toxicity.WarnOnDetection}" };
                            await ReplyAsync(embed.Build());
                        });
            }
            else
            {
                await SimpleEmbedAsync("Input Error!");
            }
        }

        [Command("WarnSpammers")]
        [Alias("AutoWarn")]
        [Summary("Warn Spammers Setup Info")]
        public Task WarnSpammersAsync()
        {
            return ReplyAsync("", false, new EmbedBuilder
            {
                Description =
                                                     "You can select roles to warn from all spam type checks in this module using the WarnSpammers command.\n" +
                                                     "__Key__\n" +
                                                     "`1` - AntiSpam\n" +
                                                     "`2` - Blacklist\n" +
                                                     "`3` - Mention\n" +
                                                     "`4` - Invite\n" +
                                                     "`5` - IP Addresses\n" +
                                                     "`6` - Toxicity\n\n" +
                                                     "__usage__\n" +
                                                     "`WarnSpammers 1` - this will warn users if they spam\n." +
                                                     "`WarnSpammers 1,2,3` - This will warn users on spamming, blacklist and mentioning multiple roles\n" +
                                                     "`WarnSpammers 0` - resets the ignore config and will stop all auto-warn actions"
            }.Build());
        }

        [Command("AddWhiteList")]
        [Summary("Skip AntiSpam on messages containing the given message")]
        public Task SkipAntiSpamAsync([Remainder] string message = null)
        {
            if (message == null)
            {
                return SimpleEmbedAsync("Please provide a message that will be skipped.");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}", g =>
                {
                    if (g.AntiSpam.AntiSpam.WhiteList.Any(x => string.Equals(x, message, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        return SimpleEmbedAsync($"`{message}` is already included in the WhiteList");
                    }

                    g.AntiSpam.AntiSpam.WhiteList.Add(message);
                    return SimpleEmbedAsync("Complete");
                });
        }

        [Command("RemoveWhiteList")]
        [Summary("Remove a message from anti spam skipper")]
        public Task RemSkipAntiSpamAsync([Remainder] string message = null)
        {
            if (message == null)
            {
                return SimpleEmbedAsync("Please provide a message that will be removed.");
            }

            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        if (!g.AntiSpam.AntiSpam.WhiteList.Any(x => string.Equals(x, message, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            return SimpleEmbedAsync($"`{message}` is not on the WhiteList");
                        }

                        g.AntiSpam.AntiSpam.WhiteList.Remove(message);

                        return SimpleEmbedAsync("Complete.");
                    });
        }

        [Command("ClearWhiteList")]
        [Summary("Clear the SkipAntiSpam List")]
        public Task ClearAntiSpamAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.AntiSpam.WhiteList = new List<string>();
                        return ReplyAsync("Complete.");
                    });
        }

        [Command("ShowWhiteList")]
        [Summary("List of messages anti-spam will skip")]
        public async Task SkipAntiSpamAsync()
        {
            var g = await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}");
            var embed = new EmbedBuilder { Description = string.Join("\n", g.AntiSpam.AntiSpam.WhiteList) };
            await ReplyAsync(embed.Build());
        }
    }
}