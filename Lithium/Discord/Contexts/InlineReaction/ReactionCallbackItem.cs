using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.Interactive
{
    public class ReactionCallbackItem
    {
        public ReactionCallbackItem(IEmote reaction, Func<SocketCommandContext, Task> callback)
        {
            Reaction = reaction;
            Callback = callback;
        }

        public IEmote Reaction { get; }
        public Func<SocketCommandContext, Task> Callback { get; }
    }
}