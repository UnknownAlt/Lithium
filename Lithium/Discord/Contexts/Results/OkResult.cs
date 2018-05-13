using Discord.Commands;

namespace Lithium.Discord.Contexts.Results
{
    public class OkResult : RuntimeResult
    {
        public OkResult(string reason = null) : base(null, reason)
        {
        }
    }
}