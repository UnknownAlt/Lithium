using System.Threading.Tasks;
using Discord.Commands;

namespace Lithium.Discord.Contexts.Criteria
{
    public interface ICriterion<T>
    {
        Task<bool> JudgeAsync(SocketCommandContext sourceContext, T parameter);
    }
}