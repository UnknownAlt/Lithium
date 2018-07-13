using System;
using System.Collections.Generic;
using System.Text;

namespace Lithium.Models
{
    using Lithium.Handlers;

    public class EventConfig
    {
        public void Save()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                session.Store(this, $"{GuildId}-Events");
                session.SaveChanges();
            }
        }

        public static EventConfig Load(ulong guildId)
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                var list = session.Load<EventConfig>($"{guildId}-Events") ?? new EventConfig
                                                                                 {
                                                                                     GuildId = guildId
                                                                                 };
                session.Dispose();
                return list;
            }
        }

        public ulong GuildId { get; set; }

        public bool LogEvents { get; set; } = false;

        public ulong EventChannel { get; set; } = 0;

        public EventSettings Settings { get; set; } = new EventSettings();

        public class EventSettings
        {
            public bool GuildMemberUpdated { get; set; } = false;

            public bool GuildUserJoined { get; set; } = true;

            public bool GuildUserLeft { get; set; } = true;

            public bool GuildUserBanned { get; set; } = true;

            public bool GuildUserUnBanned { get; set; } = true;

            public bool MessageUpdated { get; set; } = false;

            public bool MessageDeleted { get; set; } = false;

            public bool ChannelCreated { get; set; } = true;

            public bool ChannelDeleted { get; set; } = true;

            public bool ChannelUpdated { get; set; } = false;
        }
    }

}
