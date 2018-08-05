namespace RavenBOT.Modules
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Addons.Interactive;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Group("Blacklist")]
    public class Blacklist : Base
    {
        // TODO TEST ALL OF THIS
        [Command]
        [Summary("displays the blacklist")]
        public async Task BAsync()
        {
            var pages = new List<PaginatedMessage.Page>();
            var sb = new StringBuilder();
            var g = await Context.DBService.LoadAsync<GuildService.GuildModel>($"{Context.Guild.Id}");
            foreach (var blacklistWords in g.AntiSpam.Blacklist.BlacklistWordSet)
            {
                if (sb.ToString().Length >= 800)
                {
                    pages.Add(new PaginatedMessage.Page
                    {
                        Description = sb.ToString()
                    });
                    sb.Clear();
                }

                sb.Append("**Word(s)**\n" +
                          $"{string.Join("\n", blacklistWords.WordList)}\n" +
                          "**Response**\n" +
                          $"{blacklistWords.BlacklistResponse ?? g.AntiSpam.Blacklist.DefaultBlacklistMessage}\n\n");
            }

            pages.Add(new PaginatedMessage.Page
            {
                Description = sb.ToString()
            });
            var pager = new PaginatedMessage
            {
                Title = "Blacklisted Messages :stop_button: to remove",
                Pages = pages
            };

            await PagedReplyAsync(pager, new ReactionList
                                              {
                                                  Forward = true,
                                                  Backward = true,
                                                  Trash = true
                                              });
        }

        [Command("FormatHelp")]
        [Summary("help with adding multiple phrases or words to the blacklist at once")]
        public Task FormatHelpAsync()
        {
            return ReplyAsync(new EmbedBuilder
                                  {
                                      Description = "__**Sentences and Multiple Words**__\n" +
                                                    "To add a sentence to the blacklist, use the add command like so:\n" +
                                                    "`blacklist add this_is_a_sentence <response>`" +
                                                    "You may leave the response empty to use the default blacklist message as your response\n" +
                                                    "To add multiple words at once to the blacklist separate words using commas like so:\n" +
                                                    "`blacklist add word1,word2,sentence_1,word3,sentence2 <response>`\n" +
                                                    "Note that this will also work for the blacklist delete command\n\n" +
                                                    "**__Responses__**\n" +
                                                    "You can add custom text into the response by using the following custom tags:\n" +
                                                    "{user} - the user's username\n" +
                                                    "{user.mention} - @ the user\n" +
                                                    "{guild} - the guild's name\n" +
                                                    "{channel} - the current channel name\n" +
                                                    "{channel.mention} - #channel mention"
                                  }.Build());
        }

        [Command("add")]
        [Summary("adds a word to the blacklist, leave response blank to use the default message, use the same response for different blacklisted words to be grouped. Also separate sentences like so: hi_there_person for the keyword")]
        public Task AbAsync(string keyword, [Remainder] string response = null)
        {
            keyword = keyword.Replace("_", " ");
            var keywords = keyword.Split(',').Select(x => x.ToLower()).ToList();
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                async g =>
                    {
                        if (!g.AntiSpam.Blacklist.BlacklistWordSet.Any(x => x.WordList.Contains(keyword)))
                        {
                            var blacklistWords = g.AntiSpam.Blacklist.BlacklistWordSet.FirstOrDefault(x => x.BlacklistResponse == response);
                            if (blacklistWords != null)
                            {
                                blacklistWords.WordList.AddRange(keywords);
                                await Context.Message.DeleteAsync();
                                await SimpleEmbedAsync("Added to the Blacklist");
                            }
                            else
                            {
                                blacklistWords = new GuildService.GuildModel.AntiSpamSetup.BlacklistSettings.BlacklistWords { WordList = keywords, BlacklistResponse = response, GroupId = g.AntiSpam.Blacklist.BlacklistWordSet.Count };
                                g.AntiSpam.Blacklist.BlacklistWordSet.Add(blacklistWords);
                                await Context.Message.DeleteAsync();
                                await SimpleEmbedAsync("Added to the Blacklist");
                            }
                        }
                        else
                        {
                            await Context.Message.DeleteAsync();
                            await SimpleEmbedAsync("Keyword is already in the blacklist");
                        }
                    });
        }

        [Command("del")]
        [Summary("removes a word from the blacklist")]
        public Task DbAsync(string wordToRemove)
        {
            wordToRemove = wordToRemove.Replace("_", " ");
            var keywords = wordToRemove.Split(',').Select(x => x.ToLower()).ToList();
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                async g =>
                    {
                        foreach (var keyword in keywords)
                        {
                            var blacklistWords = g.AntiSpam.Blacklist.BlacklistWordSet.FirstOrDefault(x => x.WordList.Contains(keyword.ToLower()));
                            if (blacklistWords != null)
                            {
                                blacklistWords.WordList.Remove(keyword.ToLower());
                                if (blacklistWords.WordList.Count == 0)
                                {
                                    g.AntiSpam.Blacklist.BlacklistWordSet.Remove(blacklistWords);
                                }

                                await SimpleEmbedAsync($"{keyword} is has been removed from the blacklist");
                            }
                            else
                            {
                                await SimpleEmbedAsync($"{keyword} is not in the blacklist");
                            }
                        }
                    });
        }

        [Command("clear")]
        [Summary("clears the blacklist")]
        public Task ClearAsync()
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>($"{Context.Guild.Id}", g =>
                {
                    g.AntiSpam.Blacklist.BlacklistWordSet = new List<GuildService.GuildModel.AntiSpamSetup.BlacklistSettings.BlacklistWords>();

                    return ReplyAsync("The blacklist has been cleared.");
                });
        }

        [Command("DefaultMessage")]
        [Summary("set the default blacklist message")]
        public Task BlMessageAsync([Remainder] string message = "")
        {
            return Context.DBService.ModifyAsync<GuildService.GuildModel>(
                $"{Context.Guild.Id}",
                g =>
                    {
                        g.AntiSpam.Blacklist.DefaultBlacklistMessage = message;
                        return ReplyAsync("The default blacklist message is now:\n" + $"{g.AntiSpam.Blacklist.DefaultBlacklistMessage}");
                    });
        }
    }
}