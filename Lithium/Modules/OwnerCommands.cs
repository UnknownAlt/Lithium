using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Contexts.Paginator;
using Lithium.Models;

namespace Lithium.Modules
{
    [RequireOwner]
    public class OwnerCommands : Base
    {
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
                                     $"Unable to set the game");
                }
            }
        }


        [Command("Pages")]
        [Summary("Pages")]
        [Remarks("Paginator")]
        public async Task Pages()
        {
            var pages = new List<PaginatedMessage.Page>
            {
                new PaginatedMessage.Page
                {
                    description = "1"
                },
                new PaginatedMessage.Page
                {
                    description = "2"
                }
            };
            var gager = new PaginatedMessage
            {
                Title = "Pages",
                Pages = pages
            };
            await PagedReplyAsync(gager);
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
        }
    }
}