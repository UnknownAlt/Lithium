namespace RavenBOT.Modules
{
    using System.Threading.Tasks;

    using Discord.Addons.PrefixService;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;

    [RequireOwner]
    public class BotOwner : Base
    {
        private readonly PrefixService prefix;

        public BotOwner(PrefixService prefixService)
        {
            prefix = prefixService;
        }

        [Command("DefaultPrefix")]
        public Task SetDefaultPrefixAsync(string newPrefix)
        {
            prefix.SetDefaultPrefix(newPrefix);
            return ReplyAsync("Prefix Set.");
        }
    }
}
