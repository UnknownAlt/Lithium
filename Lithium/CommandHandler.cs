using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Lithium
{
    public class CommandHandler
    {
        public static int Messages;
        public static int Commands;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        public IServiceProvider Provider;

        public CommandHandler(IServiceProvider provider)
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
            Log.Information(
                $"Invite: https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=2146958591");
        }

        private static async Task _client_JoinedGuild(SocketGuild guild)
        {
            var embed = new EmbedBuilder();
            embed.AddField($"{guild.CurrentUser.Username}",
                $"Hi there, I am {guild.CurrentUser.Username}. Type `{Config.Load().Prefix}help` to see a list of my commands");
            embed.WithColor(Color.Blue);
            embed.AddField("Bot Base By PassiveModding", $"Support Server: {Config.Load().HomeInvite} \n" +
                                                         "Patreon: https://www.patreon.com/passivebot");
            try
            {
                await guild.DefaultChannel.SendMessageAsync("", false, embed.Build());
            }
            catch
            {
                foreach (var channel in guild.Channels)
                    try
                    {
                        await ((ITextChannel) channel).SendMessageAsync("", false, embed.Build());
                        break;
                    }
                    catch
                    {
                        //
                    }
            }
        }


        public async Task DoCommand(SocketMessage parameterMessage)
        {
            Messages++;
            if (!(parameterMessage is SocketUserMessage message)) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message);


            //Do not react to commands initiated by a bot
            if (context.User.IsBot)
                return;

            //Ensure that commands are only executed if thet start with the bot's prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                  message.HasStringPrefix(Config.Load().Prefix, ref argPos))) return;


            var result = await _commands.ExecuteAsync(context, argPos, Provider);

            var commandsuccess = result.IsSuccess;


            if (!commandsuccess)
            {
                var embed = new EmbedBuilder();

                foreach (var module in _commands.Modules)
                foreach (var command in module.Commands)
                    if (context.Message.Content.ToLower()
                        .StartsWith($"{Config.Load().Prefix}{command.Name} ".ToLower()))
                    {
                        embed.AddField("COMMAND INFO", $"Name: {command.Name}\n" +
                                                       $"Summary: {command.Summary}\n" +
                                                       $"Info: {command.Remarks}");
                        break;
                    }

                embed.AddField($"ERROR {result.Error.ToString().ToUpper()}", $"Command: {context.Message}\n" +
                                                                             $"Error: {result.ErrorReason}");


                embed.Color = Color.Red;
                await context.Channel.SendMessageAsync("", false, embed.Build());
                Logger.LogError($"{message.Content} || {message.Author}");
            }
            else
            {
                Logger.LogInfo($"{message.Content} || {message.Author}");
                Commands++;
            }
        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}