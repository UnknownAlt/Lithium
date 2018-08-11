namespace RavenBOT.Modules
{
    using System.Threading.Tasks;

    using Discord.Addons.PrefixService;
    using Discord.Commands;

    using Lithium.Discord.Services;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Core.Configuration.BotConfig;

    [RequireOwner]
    public class BotOwner : Base
    {
        private readonly PrefixService prefix;

        private readonly Perspective.Api perspective;

        public BotOwner(PrefixService prefixService, Perspective.Api api)
        {
            prefix = prefixService;
            perspective = api;
        }

        [Command("SetToxicityToken")]
        public async Task SetToxicityTokenAsync(string token)
        {
            await Context.DBService.ModifyAsync<Config>(
                "Config",
                c =>
                    {
                        c.PerspectiveToken = token;
                        return Task.CompletedTask;
                    });

            perspective.Initialize(token);

            await SimpleEmbedAsync("Toxicity token set.");
        }

        [Command("DefaultPrefix")]
        public Task SetDefaultPrefixAsync(string newPrefix)
        {
            prefix.SetDefaultPrefix(newPrefix);
            return ReplyAsync("Prefix Set.");
        }
    }
}
