namespace Lithium.Discord.Context
{
    using System;

    using global::Discord.Commands;

    using global::Discord.WebSocket;

    using Lithium.Discord.Extensions;
    using Lithium.Handlers;
    using Lithium.Models;

    using Microsoft.Extensions.DependencyInjection;

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
            // These are our custom additions to the context, giving access to the server object and all server objects through Context.
            var handler = serviceProvider.GetRequiredService<DatabaseHandler>();
            Server = handler.Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, Guild.Id);
            Prefix = PrefixDictionary.Load(handler.Execute<ConfigModel>(DatabaseHandler.Operation.LOAD, null, "Config").Prefix).GuildPrefix(message.Author.CastToSocketGuildUser().Guild.Id);
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        public GuildModel Server { get; }

        public string Prefix { get; }
    }
}