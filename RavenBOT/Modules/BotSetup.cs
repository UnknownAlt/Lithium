namespace RavenBOT.Modules
{
    using System.Threading.Tasks;

    using Discord.Commands;

    using Lithium.Discord.Services;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Core.Configuration.BotConfig;

    public class BotSetup : Base
    {
        private Perspective.Api Perspective { get; }

        public BotSetup(Perspective.Api api)
        {
            Perspective = api;
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

            Perspective.Initialize(token);

            await SimpleEmbedAsync("Toxicity token set.");
        }
    }
}
