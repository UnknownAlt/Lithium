using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts.Criteria;

namespace Lithium.Discord.Contexts.Callbacks
{
    public interface IReactionCallback
    {
        RunMode RunMode { get; }
        ICriterion<SocketReaction> Criterion { get; }
        TimeSpan? Timeout { get; }
        SocketCommandContext Context { get; }

        Task<bool> HandleCallbackAsync(SocketReaction reaction);
    }
}