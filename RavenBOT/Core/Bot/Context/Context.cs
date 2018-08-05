namespace RavenBOT.Core.Bot.Context
{
    using System;

    using Discord.Commands;
    using Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;
    
    using Passive.Services.DatabaseService;

    /// <summary>
    /// The context.
    /// </summary>
    public class Context : ShardedCommandContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        /// <param name="client">
        /// The client param.
        /// </param>
        /// <param name="message">
        /// The message param.
        /// </param>
        /// <param name="serviceProvider">
        /// The service provider.
        /// </param>
        public Context(DiscordShardedClient client, SocketUserMessage message, IServiceProvider serviceProvider) : base(client, message)
        {
            DBService = serviceProvider.GetRequiredService<DatabaseService>();
        }

        /// <summary>
        /// Gets the database service.
        /// </summary>
        public DatabaseService DBService { get; }
    }
}