using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Models;
using Lithium.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Lithium.Handlers
{
    public class EventHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        public IServiceProvider Provider;

        public EventHandler(IServiceProvider provider)
        {
            Provider = provider;
            _client = Provider.GetService<DiscordSocketClient>();
            _commands = new CommandService();

            _client.MessageReceived += DoCommand;
            _client.JoinedGuild += _client_JoinedGuild;
            _client.Ready += _client_Ready;
        }

        private async Task _client_Ready()
        {
            var application = await _client.GetApplicationInfoAsync();
            Log.Information($"Invite: https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=2146958591");
            DatabaseHandler.CheckDB(_client);
            var dblist = DatabaseHandler.GetFullConfig();
            foreach (var guild in _client.Guilds.Where(g => dblist.All(x => x.GuildID != g.Id)))
            {
                DatabaseHandler.SaveGuild(new GuildModel.Guild
                {
                    GuildID = guild.Id
                });
            }
        }

        private static async Task _client_JoinedGuild(SocketGuild guild)
        {
            //Ensure that we notify new servers how to use the bot by telling them how to get use th ehelp command.
            var embed = new EmbedBuilder
            {
                Title = guild.CurrentUser.Username,
                Description = $"Hi there, I am {guild.CurrentUser.Username}. Type `{Config.Load().DefaultPrefix}help` to see a list of my commands",
                Color = Color.Blue
            };
            await guild.DefaultChannel.SendMessageAsync("", false, embed.Build());

        }


        public async Task DoCommand(SocketMessage parameterMessage)
        {
            if (!(parameterMessage is SocketUserMessage message)) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message);


            //Do not react to commands initiated by a bot
            if (context.User.IsBot)
                return;

            //Ensure that commands are only executed if thet start with the bot's prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                  message.HasStringPrefix(Config.Load().DefaultPrefix, ref argPos))) return;


            var result = await _commands.ExecuteAsync(context, argPos, Provider);

            var commandsuccess = result.IsSuccess;


            if (!commandsuccess)
            {
                var embed = new EmbedBuilder
                {
                    Title = $"ERROR: {result.Error.ToString().ToUpper()}",
                    Description = $"Command: {context.Message}\n" +
                                    $"Error: {result.ErrorReason}"
                };
                await context.Channel.SendMessageAsync("", false, embed.Build());
                Logger.LogError($"{message.Content} || {message.Author}");
            }
            else
            {
                Logger.LogInfo($"{message.Content} || {message.Author}");
            }
        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}