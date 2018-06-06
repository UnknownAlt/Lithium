using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using DiscordBotsList.Api;
using Lithium.Discord.Contexts;
using Lithium.Models;

namespace Lithium.Modules.BotOwner
{
    [RequireOwner]
    public class OwnerCommands : Base
    {
        [Command("UpdateServers")]
        [Summary("UpdateServers")]
        [Remarks("Update the Bot's Server Count on DiscordBots.org")]
        public async Task UpdateCount()
        {
            var token = Config.Load().DBLToken;
            if (token == null)
            {
                return;
            }

            var DblApi = new AuthDiscordBotListApi(Context.Client.CurrentUser.Id, Config.Load().DBLToken);
            var me = await DblApi.GetMeAsync();
            await me.UpdateStatsAsync(Context.Socket.Client.Guilds.Count);
        }

        [Command("SetGame")]
        [Summary("SetGame <game>")]
        [Remarks("Set the bot's Current Game.")]
        public async Task Setgame([Remainder] string game = null)
        {
            if (game == null)
            {
                await ReplyAsync("Please specify a game");
            }
            else
            {
                try
                {
                    await Context.Socket.Client.SetGameAsync(game);
                    await ReplyAsync($"{Context.Client.CurrentUser.Username}'s game has been set to:\n" +
                                     $"{game}");
                }
                catch (Exception e)
                {
                    await ReplyAsync($"{e.Message}\n" +
                                     "Unable to set the game");
                }
            }
        }

        [Command("Stats")]
        [Summary("Stats")]
        [Remarks("Display Bot Statistics")]
        public async Task BotStats()
        {
            var embed = new EmbedBuilder();

            var heap = Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.InvariantCulture);
            var uptime = (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

            embed.AddField($"{Context.Client.CurrentUser.Username} Statistics",
                $"Servers: {Context.Socket.Client.Guilds.Count}\n" +
                $"Users: {Context.Socket.Client.Guilds.Select(x => x.Users.Count).Sum()}\n" +
                $"Unique Users: {Context.Socket.Client.Guilds.SelectMany(x => x.Users.Select(y => y.Id)).Distinct().Count()}\n" +
                $"Server Channels: {Context.Socket.Client.Guilds.Select(x => x.Channels.Count).Sum()}\n" +
                $"DM Channels: {Context.Socket.Client.DMChannels.Count}\n\n" +
                $"Uptime: {uptime}\n" +
                $"Heap Size: {heap}\n" +
                $"Discord Version: {DiscordConfig.Version}");

            await ReplyAsync("", false, embed.Build());
        }

        // InlineReactionReplyAsync will send a message and adds reactions on it.
        // Once an user adds a reaction, the callback is fired.
        // If callback was successfull next callback is not handled (message is unsubscribed).
        // Unsuccessful callback is a reaction that did not have a callback.
        [Command("reaction")]
        public async Task Test_ReactionReply()
        {
            await InlineReactionReplyAsync(new ReactionCallbackData("text")
                .WithCallback(new Emoji("👍"), c => c.Channel.SendMessageAsync("You've replied with 👍"))
                .WithCallback(new Emoji("👎"), c => c.Channel.SendMessageAsync("You've replied with 👎"))
            );
        }

        [Command("embedreaction")]
        public async Task Test_EmedReactionReply()
        {
            var one = new Emoji("1⃣");
            var two = new Emoji("2⃣");

            var embed = new EmbedBuilder()
                .WithTitle("Choose one")
                .AddInlineField(one.Name, "Beer")
                .AddInlineField(two.Name, "Drink")
                .Build();

            await InlineReactionReplyAsync(new ReactionCallbackData("text", embed)
                .WithCallback(one, c => c.Channel.SendMessageAsync("Here you go :beer:"))
                .WithCallback(two, c => c.Channel.SendMessageAsync("Here you go :tropical_drink:"))
            );
        }

        [Group("Tokens")]
        public class Tokens : Base
        {
            [Command("Perspective")]
            [Summary("Perspective <token>")]
            [Remarks("Set the toxicity token")]
            public async Task SetPerspectiveToken([Remainder] string token = null)
            {
                var cfg = Config.Load();
                cfg.ToxicityToken = token;
                cfg.Save();

                await ReplyAsync("Success Token Set (or reset if nothing was supplied)\n" +
                                 "NOTE, This requires a bot restart");
            }

            [Command("DBL")]
            [Summary("DBL <token>")]
            [Remarks("Set the DiscordBotsList token")]
            public async Task SetDBL([Remainder] string token = null)
            {
                var cfg = Config.Load();
                cfg.DBLToken = token;
                cfg.Save();

                await ReplyAsync("Success Token Set (or reset if nothing was supplied)");
            }
        }
    }
}