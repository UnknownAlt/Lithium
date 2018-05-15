using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts.Callbacks;
using Lithium.Discord.Contexts.Criteria;
using Lithium.Discord.Contexts.Paginator;
using Lithium.Handlers;
using Lithium.Models;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Lithium.Discord.Contexts
{
    public abstract class Base : ModuleBase<LithiumContext>
    {
        public InteractiveService Interactive { get; set; }

        //Reply in the server. This is a shortcut for context.channel.sendmessageasync
        public async Task<IUserMessage> ReplyAsync(string Message, Embed Embed = null)
        {
            await Context.Channel.TriggerTypingAsync();
            return await base.ReplyAsync(Message, false, Embed);
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

        public Task<SocketMessage> NextMessageAsync(ICriterion<SocketMessage> criterion, TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(LithiumSocketContext(), criterion, timeout);
        }

        private SocketCommandContext LithiumSocketContext()
        {
            return new SocketCommandContext(Context.Client as DiscordSocketClient, Context.Message as SocketUserMessage);
        }

        public Task<SocketMessage> NextMessageAsync(bool fromSourceUser = true, bool inSourceChannel = true, TimeSpan? timeout = null)
        {
            return Interactive.NextMessageAsync(LithiumSocketContext(), fromSourceUser, inSourceChannel, timeout);
        }

        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, bool fromSourceUser = true, bool showall = false, bool showindex = false)
        {
            var criterion = new Criteria<SocketReaction>();
            if (fromSourceUser)
            {
                criterion.AddCriterion(new EnsureReactionFromSourceUserCriterion());
            }

            return PagedReplyAsync(pager, criterion, showall, showindex);
        }

        public Task<IUserMessage> PagedReplyAsync(PaginatedMessage pager, ICriterion<SocketReaction> criterion, bool showall = false, bool showindex = false)
        {
            return Interactive.SendPaginatedMessageAsync(LithiumSocketContext(), pager, criterion, showall, showindex);
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
            Guild = MessageParam.Channel is IDMChannel ? null : (MessageParam.Channel as IGuildChannel).Guild;

            //
            Socket = new SocketContext
            {
                Guild = Guild as SocketGuild,
                User = User as SocketUser,
                Client = Client as DiscordSocketClient,
                Message = Message as SocketUserMessage,
                Channel = Channel as ISocketMessageChannel
            };

            //These are our custom additions to the context, giving access to the server object and all server objects through Context.
            Server = Channel is IDMChannel ? null : DatabaseHandler.GetGuild(Guild.Id);
            Session = ServiceProvider.GetRequiredService<IDocumentStore>().OpenSession();
        }

        public GuildModel.Guild Server { get; }
        public IDocumentSession Session { get; }
        public SocketContext Socket { get; }
        public IUser User { get; }
        public IGuild Guild { get; }
        public IDiscordClient Client { get; }
        public IUserMessage Message { get; }
        public IMessageChannel Channel { get; }

        public class SocketContext
        {
            public SocketUser User { get; set; }
            public SocketGuild Guild { get; set; }
            public DiscordSocketClient Client { get; set; }
            public SocketUserMessage Message { get; set; }
            public ISocketMessageChannel Channel { get; set; }
        }
    }

    public class InteractiveService : IDisposable
    {
        private readonly Dictionary<ulong, IReactionCallback> _callbacks;
        private readonly TimeSpan _defaultTimeout;

        public InteractiveService(DiscordSocketClient discord, TimeSpan? defaultTimeout = null)
        {
            Discord = discord;
            Discord.ReactionAdded += HandleReactionAsync;

            _callbacks = new Dictionary<ulong, IReactionCallback>();
            _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(15);
        }

        public DiscordSocketClient Discord { get; }

        public void Dispose()
        {
            Discord.ReactionAdded -= HandleReactionAsync;
        }

        public Task<SocketMessage> NextMessageAsync(SocketCommandContext context, bool fromSourceUser = true,
            bool inSourceChannel = true, TimeSpan? timeout = null)
        {
            var criterion = new Criteria<SocketMessage>();
            if (fromSourceUser)
                criterion.AddCriterion(new EnsureSourceUserCriterion());
            if (inSourceChannel)
                criterion.AddCriterion(new EnsureSourceChannelCriterion());
            return NextMessageAsync(context, criterion, timeout);
        }

        public async Task<SocketMessage> NextMessageAsync(SocketCommandContext context,
            ICriterion<SocketMessage> criterion, TimeSpan? timeout = null)
        {
            timeout = timeout ?? _defaultTimeout;

            var eventTrigger = new TaskCompletionSource<SocketMessage>();

            async Task Handler(SocketMessage message)
            {
                var result = await criterion.JudgeAsync(context, message).ConfigureAwait(false);
                if (result)
                    eventTrigger.SetResult(message);
            }

            context.Client.MessageReceived += Handler;

            var trigger = eventTrigger.Task;
            var delay = Task.Delay(timeout.Value);
            var task = await Task.WhenAny(trigger, delay).ConfigureAwait(false);

            context.Client.MessageReceived -= Handler;

            if (task == trigger)
                return await trigger.ConfigureAwait(false);
            return null;
        }

        public async Task<IUserMessage> ReplyAndDeleteAsync(SocketCommandContext context, string content,
            bool isTTS = false, Embed embed = null, TimeSpan? timeout = null, RequestOptions options = null)
        {
            timeout = timeout ?? _defaultTimeout;
            var message = await context.Channel.SendMessageAsync(content, isTTS, embed, options).ConfigureAwait(false);
            _ = Task.Delay(timeout.Value)
                .ContinueWith(_ => message.DeleteAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
            return message;
        }

        public async Task<IUserMessage> SendPaginatedMessageAsync(SocketCommandContext context, PaginatedMessage pager,
            ICriterion<SocketReaction> criterion = null, bool showall = false, bool showindex = false)
        {
            var callback = new PaginatedMessageCallback(this, context, pager, criterion);
            await callback.DisplayAsync(showall, showindex).ConfigureAwait(false);
            return callback.Message;
        }

        public void AddReactionCallback(IMessage message, IReactionCallback callback)
        {
            _callbacks[message.Id] = callback;
        }

        public void RemoveReactionCallback(IMessage message)
        {
            RemoveReactionCallback(message.Id);
        }

        public void RemoveReactionCallback(ulong id)
        {
            _callbacks.Remove(id);
        }

        public void ClearReactionCallbacks()
        {
            _callbacks.Clear();
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.UserId == Discord.CurrentUser.Id) return;
            if (!_callbacks.TryGetValue(message.Id, out var callback)) return;
            if (!await callback.Criterion.JudgeAsync(callback.Context, reaction).ConfigureAwait(false))
                return;
            switch (callback.RunMode)
            {
                case RunMode.Async:
                    _ = Task.Run(async () =>
                    {
                        if (await callback.HandleCallbackAsync(reaction).ConfigureAwait(false))
                            RemoveReactionCallback(message.Id);
                    });
                    break;
                default:
                    if (await callback.HandleCallbackAsync(reaction).ConfigureAwait(false))
                        RemoveReactionCallback(message.Id);
                    break;
            }
        }
    }
}