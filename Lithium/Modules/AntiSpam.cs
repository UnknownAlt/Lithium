namespace Lithium.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    public class AutoMod : Base
    {
        [Command("NoSpam")]
        [Summary("NoSpam")]
        [Remarks("Toggle Auto Removal of spam messages")]
        public Task NoSpamAsync()
        {
            Context.Server.AntiSpam.AntiSpam.NoSpam = !Context.Server.AntiSpam.AntiSpam.NoSpam;
            Context.Server.Save();
            return ReplyAsync($"NoSpam: {Context.Server.AntiSpam.AntiSpam.NoSpam}");
        }

        [Command("NoSpamCount")]
        [Summary("NoSpamCount")]
        [Remarks("Set amount of messages required before noSpam detects it")]
        public Task NoSpamCountAsync(int messages = 3)
        {
            Context.Server.AntiSpam.AntiSpam.RateLimiting.MessageCount = messages;
            Context.Server.Save();
            return ReplyAsync($"if more than {messages} messages are sent within {Context.Server.AntiSpam.AntiSpam.RateLimiting.TimePeriod.TotalSeconds} seconds, they will be counted as spam and not be able to send messages for another {Context.Server.AntiSpam.AntiSpam.RateLimiting.BlockPeriod.TotalSeconds} seconds");
        }

        [Command("NoSpamTimeout")]
        [Summary("NoSpamTimeout")]
        [Remarks("Set amount of seconds per message-limit to detect spam")]
        public Task NoSpamTimeAsync(int seconds = 5)
        {
            if (seconds > 30 || seconds < 2)
            {
                throw new Exception("Seconds must be less than 30 and greater than 2");
            }

            Context.Server.AntiSpam.AntiSpam.RateLimiting.TimePeriod = TimeSpan.FromSeconds(seconds);
            Context.Server.Save();
            return ReplyAsync($"if more than {Context.Server.AntiSpam.AntiSpam.RateLimiting.MessageCount} messages are sent within {seconds} seconds, they will be counted as spam and not be able to send messages for another {Context.Server.AntiSpam.AntiSpam.RateLimiting.BlockPeriod.TotalSeconds} seconds");
        }

        [Command("NoSpamBlock")]
        [Summary("NoSpamBlock")]
        [Remarks("Set amount of time users must wait before sending messages again")]
        public Task NoSpamBlockAsync(int seconds = 5)
        {
            if (seconds > 30 || seconds < 2)
            {
                throw new Exception("Seconds must be less than 30 and greater than 2");
            }

            Context.Server.AntiSpam.AntiSpam.RateLimiting.BlockPeriod = TimeSpan.FromSeconds(seconds);
            Context.Server.Save();
            return ReplyAsync($"if more than {Context.Server.AntiSpam.AntiSpam.RateLimiting.MessageCount} messages are sent within {seconds} seconds, they will be counted as spam and not be able to send messages for another {seconds} seconds");
        }

        [Command("NoInvite")]
        [Summary("NoInvite")]
        [Remarks("Disable the posting of discord invite links in the server")]
        public Task NoInviteAsync()
        {
            Context.Server.AntiSpam.Advertising.Invite = !Context.Server.AntiSpam.Advertising.Invite;
            Context.Server.Save();

            return ReplyAsync($"NoInvite: {Context.Server.AntiSpam.Advertising.Invite}");
        }

        [Command("NoInviteMessage")]
        [Summary("NoInviteMessage <message>")]
        [Remarks("set the no invites message")]
        public Task NoInviteAsync([Remainder] string message = null)
        {
            Context.Server.AntiSpam.Advertising.Response = message;
            Context.Server.Save();

            return ReplyAsync("The No Invites message is now:\n" +
                              $"{Context.Server.AntiSpam.Advertising.Response ?? "Default"}");
        }

        [Command("NoMentionAll")]
        [Summary("NoMentionAll")]
        [Remarks("Disable the use of @everyone and @here for users")]
        public Task NoMentionAllAsync()
        {
            Context.Server.AntiSpam.Mention.MentionAll = !Context.Server.AntiSpam.Mention.MentionAll;
            Context.Server.Save();
            return ReplyAsync($"NoMentionAll: {Context.Server.AntiSpam.Mention.MentionAll}");
        }

        [Command("NoMentionMessage")]
        [Summary("NoMentionMessage")]
        [Remarks("Set the No Mention Message response")]
        public Task NoMentionAllAsync([Remainder] string message = null)
        {
            Context.Server.AntiSpam.Mention.MentionAllResponse = message;
            Context.Server.Save();
            return ReplyAsync($"No Mention Message: {message ?? "N/A"}");
        }

        [Command("NoMassMention")]
        [Summary("NoMassMention")]
        [Remarks("Toggle auto-deletion of messages with 5+ role or user mentions")]
        public Task NoMassMentionAsync()
        {
            Context.Server.AntiSpam.Mention.RemoveMassMention = !Context.Server.AntiSpam.Mention.RemoveMassMention;
            Context.Server.Save();
            return ReplyAsync($"NoMassMention: {Context.Server.AntiSpam.Mention.RemoveMassMention}");
        }

        [Command("NoIPs")]
        [Summary("NoIps")]
        [Remarks("Toggle auto-deletion of messages containing valid IP addresses")]
        public Task NoIPsAsync()
        {
            Context.Server.AntiSpam.Privacy.RemoveIPs = !Context.Server.AntiSpam.Privacy.RemoveIPs;
            Context.Server.Save();
            return ReplyAsync($"No IP Addresses: {Context.Server.AntiSpam.Privacy.RemoveIPs}");
        }

        [Command("NoToxicity")]
        [Summary("NoToxicity <threshold>")]
        [Remarks("Toggle auto-deletion of messages that are too toxic")]
        public Task NoToxicityAsync(int threshold = 999)
        {
            if (threshold == 999 || threshold < 50 || threshold > 99)
            {
                throw new Exception("Pick a threshold between 50 and 99");
            }

            Context.Server.AntiSpam.Toxicity.ToxicityThreshold = threshold;
            Context.Server.AntiSpam.Toxicity.UsePerspective = !Context.Server.AntiSpam.Toxicity.UsePerspective;
            Context.Server.Save();
            return ReplyAsync($"Remove Toxic Messages: {Context.Server.AntiSpam.Toxicity.UsePerspective}\n" +
                              $"Threshold: {threshold}");
        }

        [Command("HideAutoWarnDelay")]
        [Summary("HideAutoWarnDelay <seconds>")]
        [Remarks("Toggle the deletion of warn messages in channel after x sec.")]
        public Task HideAWarnDelAsync(int seconds = 5)
        {
            TimeSpan? time = TimeSpan.FromSeconds(seconds);
            if (seconds == 0)
            {
                time = null;
            }

            Context.Server.ModerationSetup.Settings.AutoHideDelay = time;
            Context.Server.Save();
            return ReplyAsync($"Auto Delete Warning Message responses after {seconds} seconds: {time.HasValue}");
        }

        [Command("ignore")]
        [Summary("ignore <selection> <@role>")]
        [Remarks("choose a role to ignore when using AntiSpam commands")]
        public Task IgnoreRoleAsync(string selection, IRole role = null)
        {
            if (role == null)
            {
                return IgnoreRoleAsync();
            }

            var split = selection.Split(',');
            Context.Server.AntiSpam.IgnoreList.TryGetValue(role.Id, out var ignore);
            var addRole = false;
            if (ignore == null)
            {
                ignore = new GuildModel.AntiSpamSetup.IgnoreRole();
                addRole = true;
            }

            if (int.TryParse(split[0], out var i))
            {
                if (i == 0)
                {
                    Context.Server.AntiSpam.IgnoreList.Remove(role.Id);
                    Context.Server.Save();
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
                    Context.Server.AntiSpam.IgnoreList.Add(role.Id, ignore);
                }

                Context.Server.Save();

                return SimpleEmbedAsync(
                    $"{role.Mention}\n" + "__Ignore AntiSpam Detections__\n" + $"Bypass AntiSpam: {ignore.AntiSpam}\n"
                    + $"Bypass Blacklist: {ignore.Blacklist}\n"
                    + $"Bypass Mention Everyone and 5+ Role Mentions: {ignore.Mention}\n"
                    + $"Bypass Invite Link Removal: {ignore.Advertising}\n" + $"Bypass IP Removal: {ignore.Privacy}\n"
                    + $"Bypass Toxicity Check: {ignore.Toxicity}");
        }

        [Command("ignore")]
        [Summary("ignore")]
        [Remarks("ignore role setup information")]
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
        [Summary("IgnoreList")]
        [Remarks("list ignore-roles and their setup")]
        public Task IgnoreListAsync()
        {
            var list = Context.Server.AntiSpam.IgnoreList.Select(x => $"{Context.Guild.GetRole(x.Key)?.Mention ?? "N/A"} Invites: {x.Value.Advertising} AntiSpam: {x.Value.AntiSpam} Blacklist: {x.Value.Blacklist} Mention: {x.Value.Mention} IP: {x.Value.Privacy} Toxicity: {x.Value.Toxicity}");
            return ReplyAsync(new EmbedBuilder
                                  {
                                      Description = string.Join("\n", list)
                                  });
        }

        [Command("WarnSpammers")]
        [Alias("AutoWarn")]
        [Summary("WarnSpammers <type>")]
        [Remarks("Toggle Auto-Warning of people detected by any of the Anti Spam methods")]
        public async Task WarnSpammersAsync([Remainder] string selection)
        {
            var split = selection.Split(',');

            if (int.TryParse(split[0], out var i))
            {
                if (i == 0)
                {
                    Context.Server.AntiSpam.AntiSpam.WarnOnDetection = false;
                    Context.Server.AntiSpam.Blacklist.WarnOnDetection = false;
                    Context.Server.AntiSpam.Advertising.WarnOnDetection = false;
                    Context.Server.AntiSpam.Mention.WarnOnDetection = false;
                    Context.Server.AntiSpam.Toxicity.WarnOnDetection = false;
                    Context.Server.AntiSpam.Privacy.WarnOnDetection = false;
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
                                await SimpleEmbedAsync($"Invalid Input {s}\n" +
                                                 "only 1-6 are accepted.");
                                return;
                            }

                            switch (i1)
                            {
                                case 1:
                                    Context.Server.AntiSpam.AntiSpam.WarnOnDetection = true;
                                    break;
                                case 2:
                                    Context.Server.AntiSpam.Blacklist.WarnOnDetection = true;
                                    break;
                                case 3:
                                    Context.Server.AntiSpam.Mention.WarnOnDetection = true;
                                    break;
                                case 4:
                                    Context.Server.AntiSpam.Advertising.WarnOnDetection = true;
                                    break;
                                case 5:
                                    Context.Server.AntiSpam.Privacy.WarnOnDetection = true;
                                    break;
                                case 6:
                                    Context.Server.AntiSpam.Toxicity.WarnOnDetection = true;
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

                var embed = new EmbedBuilder
                {
                    Description = "__AutoMod Detections__\n" +
                                  $"Warn on Anti Spam: {Context.Server.AntiSpam.AntiSpam.WarnOnDetection}\n" +
                                  $"Warn on Blacklist: {Context.Server.AntiSpam.Blacklist.WarnOnDetection}\n" +
                                  $"Warn on Mention Everyone and 5+ Role Mentions: {Context.Server.AntiSpam.Mention.WarnOnDetection}\n" +
                                  $"Warn on Invite Link Removal: {Context.Server.AntiSpam.Advertising.WarnOnDetection}\n" +
                                  $"Warn on IP Removal: {Context.Server.AntiSpam.Privacy.WarnOnDetection}\n" +
                                  $"Warn on Toxicity Check: {Context.Server.AntiSpam.Toxicity.WarnOnDetection}"
                };
                await ReplyAsync(embed.Build());
                Context.Server.Save();
            }
            else
            {
                await SimpleEmbedAsync("Input Error!");
            }
        }

        [Command("WarnSpammers")]
        [Alias("AutoWarn")]
        [Summary("WarnSpammers")]
        [Remarks("Warn Spammers Setup Info")]
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
                                                     $"`WarnSpammers 0` - resets the ignore config and will stop all auto-warn actions"
                                             }.Build());
        }

        [Command("AutoMuteExpiry")]
        [Remarks("set the amount of minutes it takes for an auto mute to expire")]
        public Task WarnExpiryTimeAsync(int minutes = 0)
        {
            TimeSpan? time = TimeSpan.FromMinutes(minutes);
            if (minutes == 0)
            {
                time = null;
            }

            Context.Server.ModerationSetup.Settings.AutoMuteExpiry = time;
            Context.Server.Save();
            return ReplyAsync($"Success! After {minutes} minutes, auto-mutes will automatically expire");
        }

        [Command("AutoBanExpiry")]
        [Summary("Admin AutoBanExpiry <Hours>")]
        [Remarks("set the amount of hours it takes for an auto ban to expire")]
        public Task BanExpiryTimeAsync(int hours = 0)
        {
            TimeSpan? time = TimeSpan.FromHours(hours);
            if (hours == 0)
            {
                time = null;
            }

            Context.Server.ModerationSetup.Settings.AutoBanExpiry = time;
            Context.Server.Save();
            return ReplyAsync($"Success! After {hours} hours, auto-bans will automatically expire");
        }

        [Command("AddWhiteList")]
        [Summary("AddWhiteList <message>")]
        [Remarks("Skip AntiSpam on messages containing the given message")]
        public async Task SkipAntiSpamAsync([Remainder] string message = null)
        {
            if (message == null)
            {
                await SimpleEmbedAsync("Please provide a message that will be skipped.");
                return;
            }

            if (Context.Server.AntiSpam.AntiSpam.WhiteList.Any(x =>
                string.Equals(x, message, StringComparison.CurrentCultureIgnoreCase)))
            {
                await SimpleEmbedAsync($"`{message}` is already included in the WhiteList");
                return;
            }

            Context.Server.AntiSpam.AntiSpam.WhiteList.Add(message);

            Context.Server.Save();
            await SimpleEmbedAsync("Complete.");
        }

        [Command("RemoveWhiteList")]
        [Remarks("Remove a message from anti spam skipper")]
        public async Task RemSkipAntiSpamAsync([Remainder] string message = null)
        {
            if (message == null)
            {
                await SimpleEmbedAsync("Please provide a message that will be removed.");
                return;
            }

            if (!Context.Server.AntiSpam.AntiSpam.WhiteList.Any(x =>
                string.Equals(x, message, StringComparison.CurrentCultureIgnoreCase)))
            {
                await SimpleEmbedAsync($"`{message}` is not on the WhiteList");
                return;
            }

            Context.Server.AntiSpam.AntiSpam.WhiteList.Remove(message);

            Context.Server.Save();
            await SimpleEmbedAsync("Complete.");
        }

        [Command("ClearWhiteList")]
        [Remarks("Clear the SkipAntiSpam List")]
        public Task ClearAntiSpamAsync()
        {
            Context.Server.AntiSpam.AntiSpam.WhiteList = new List<string>();
            Context.Server.Save();
            return ReplyAsync("Complete.");
        }

        [Command("ShowWhiteList")]
        [Remarks("List of messages anti-spam will skip")]
        public Task SkipAntiSpamAsync()
        {
            var embed = new EmbedBuilder { Description = string.Join("\n", Context.Server.AntiSpam.AntiSpam.WhiteList) };
            return ReplyAsync(embed.Build());
        }
    }
}