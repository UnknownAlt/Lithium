namespace Lithium.Discord.Extensions
{
    using System;

    using global::Discord.WebSocket;

    public static class GuildExtensions
    {
        public static SocketTextChannel CastToSocketTextChannel(this ISocketMessageChannel channel)
        {
            if (channel is SocketTextChannel socketTextChannel)
            {
                return socketTextChannel;
            }

            throw new InvalidCastException("The channel cannot be cast to a SocketTextChannel");
        }
    }
}