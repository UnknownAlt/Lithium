using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Handlers;
using Lithium.Models;
using Lithium.Services;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Lithium.Discord.Contexts
{
    public abstract class Base : ModuleBase<LithiumContext>
    {
        //Reply in the server. This is a shortcut for context.channel.sendmessageasync
        public async Task<IUserMessage> ReplyAsync(string Message, Embed Embed = null)
        {
            await Context.Channel.TriggerTypingAsync();
            return await base.ReplyAsync(Message, false, Embed, null);
        }

        //Reply in the server and then delete after the provided delay.
        public async Task<IUserMessage> ReplyAndDeleteAsync(string Message, TimeSpan? Timeout = null)
        {
            Timeout = Timeout ?? TimeSpan.FromSeconds(5);
            var Msg = await ReplyAsync(Message).ConfigureAwait(false);
            _ = Task.Delay(Timeout.Value).ContinueWith(_ => Msg.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
            return Msg;
        }

        //Shorthand for  replying with just an embed
        public async Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed)
        {
            return await base.ReplyAsync("", false, embed.Build());
        }
    }

    public class LithiumContext : ICommandContext
    {
        public LithiumContext(IDiscordClient ClientParam, IUserMessage MessageParam, IServiceProvider ServiceProvider)
        {
            Client = ClientParam;
            Message = MessageParam;
            User = MessageParam.Author;
            Channel = MessageParam.Channel;
            Guild = (MessageParam.Channel as IGuildChannel).Guild;

            //These are our custom additions to the context, giving access to the server object and all server objects through Context.
            Server = DatabaseHandler.GetGuild(Guild.Id);
            Session = ServiceProvider.GetRequiredService<IDocumentStore>().OpenSession();
        }

        public GuildModel.Guild Server { get; }
        public IDocumentSession Session { get; }
        public IUser User { get; }
        public IGuild Guild { get; }
        public IDiscordClient Client { get; }
        public IUserMessage Message { get; }
        public IMessageChannel Channel { get; }
    }
}