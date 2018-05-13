using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts.Criteria;

namespace Lithium.Discord.Contexts.Paginator
{
    internal class EnsureReactionFromSourceUserCriterion : ICriterion<SocketReaction>
    {
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter)
        {
            var ok = parameter.UserId == sourceContext.User.Id;
            return Task.FromResult(ok);
        }
    }
}