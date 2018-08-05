namespace RavenBOT.Core.Bot.Handlers.Events
{
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Extensions;

    /// <summary>
    /// The event handler.
    /// </summary>
    public partial class EventHandler
    {
        internal async Task MessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage Message) || message.Author.IsBot || message.Author.IsWebhook)
            {
                return;
            }

            int argPos = 0;

            var context = new Context(Client, Message, Provider);
            if (!(Message.HasStringPrefix(PrefixService.GetPrefix(context.Guild?.Id ?? 0), ref argPos) || Message.HasMentionPrefix(Client.CurrentUser, ref argPos)))
            {
                await AutoMod.RunChecksAsync(context);
                return;
            }

            var result = await CommandService.ExecuteAsync(context, argPos, Provider);

            if (!result.IsSuccess)
            {
                // Log error.
                if (result.Error == CommandError.Exception || result.Error == CommandError.Unsuccessful)
                {
                    LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Error);
                }
                else
                {
                    LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Warning);
                }

                await context.Channel.SendMessageAsync("", false, result.GenerateErrorEmbed(CommandService, context, argPos).Build());
            }
            else
            {
                // Log Command.
                LogHandler.LogMessage(context);
            }
        }

        internal async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> messageCacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            // Do something later
        }
    }
}
