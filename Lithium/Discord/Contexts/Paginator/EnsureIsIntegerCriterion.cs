using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts.Criteria;

namespace Lithium.Discord.Contexts.Paginator
{
    internal class EnsureIsIntegerCriterion : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        {
            var ok = int.TryParse(parameter.Content, out _);
            return Task.FromResult(ok);
        }
    }
}