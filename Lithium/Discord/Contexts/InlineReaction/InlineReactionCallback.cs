﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts;
using Lithium.Discord.Contexts.Callbacks;
using Lithium.Discord.Contexts.Criteria;

namespace Discord.Addons.Interactive
{
    public class InlineReactionCallback : IReactionCallback
    {
        private readonly ReactionCallbackData data;

        private readonly InteractiveService interactive;

        public InlineReactionCallback(
            InteractiveService interactive,
            SocketCommandContext context,
            ReactionCallbackData data,
            ICriterion<SocketReaction> criterion = null)
        {
            this.interactive = interactive;
            Context = context;
            this.data = data;
            Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
            Timeout = data.Timeout ?? TimeSpan.FromSeconds(30);
        }

        public IUserMessage Message { get; private set; }
        public RunMode RunMode => RunMode.Sync;

        public ICriterion<SocketReaction> Criterion { get; }

        public TimeSpan? Timeout { get; }

        public SocketCommandContext Context { get; }

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var reactionCallbackItem = data.Callbacks.FirstOrDefault(t => t.Reaction.Equals(reaction.Emote));
            if (reactionCallbackItem == null)
                return false;

            await reactionCallbackItem.Callback(Context);
            return true;
        }

        public async Task DisplayAsync()
        {
            var message = await Context.Channel.SendMessageAsync(data.Text, embed: data.Embed).ConfigureAwait(false);
            Message = message;
            interactive.AddReactionCallback(message, this);

            _ = Task.Run(async () =>
            {
                foreach (var item in data.Callbacks)
                    await message.AddReactionAsync(item.Reaction);
            });

            if (Timeout.HasValue)
            {
                _ = Task.Delay(Timeout.Value)
                    .ContinueWith(_ => interactive.RemoveReactionCallback(message));
            }
        }
    }
}