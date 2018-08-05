namespace RavenBOT.Extensions
{
    using System;

    using Discord;
    using Discord.WebSocket;

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

        public static SocketGuild CastToSocketGuild(this IGuild guild)
        {
            if (guild is SocketGuild socketGuild)
            {
                return socketGuild;
            }

            throw new InvalidCastException("The guild cannot be cast to a SocketGuild");
        }
    }
}