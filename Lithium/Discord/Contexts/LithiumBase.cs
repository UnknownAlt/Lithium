using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Lithium.Handlers;
using Lithium.Models;
using Lithium.Services;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Lithium.Discord.Contexts
{
    public class Base : ModuleBase<LithiumBase>
    {
        public async Task<IUserMessage> ReplyAsync(string Message, Embed Embed = null)
        {
            await Context.Channel.TriggerTypingAsync();
            return await base.ReplyAsync(Message, false, Embed, null);
        }

        public async Task<IUserMessage> ReplyAndDeleteAsync(string Message, TimeSpan? Timeout = null)
        {
            Timeout = Timeout ?? TimeSpan.FromSeconds(5);
            var Msg = await ReplyAsync(Message).ConfigureAwait(false);
            _ = Task.Delay(Timeout.Value).ContinueWith(_ => Msg.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
            return Msg;
        }

        void SaveDocuments()
        {

            Context.Server.Save(Context.Server);
            bool Check = !Context.Session.Advanced.HasChanges;

            if (Check == false) Logger.LogInfo($"Failed to save document.");
        }

        public Task<IUserMessage> SendEmbedAsync(EmbedBuilder embed)
        {
            return Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }

    public class LithiumBase : ICommandContext
    {
        public IUser User { get; }
        public IGuild Guild { get; }
        public GuildModel.Guild Server { get; }
        public IDiscordClient Client { get; }
        public IUserMessage Message { get; }
        public IMessageChannel Channel { get; }
        public IDocumentSession Session { get; }

        public LithiumBase(IDiscordClient ClientParam, IUserMessage MessageParam, IServiceProvider ServiceProvider)
        {
            Client = ClientParam;
            Message = MessageParam;
            User = MessageParam.Author;
            Channel = MessageParam.Channel;
            Guild = (MessageParam.Channel as IGuildChannel).Guild;
            Server = ServiceProvider.GetRequiredService<DatabaseHandler>().GetGuild(Guild.Id);
            Session = ServiceProvider.GetRequiredService<IDocumentStore>().OpenSession();
        }

    }
}
