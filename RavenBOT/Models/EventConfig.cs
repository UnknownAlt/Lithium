namespace RavenBOT.Models
{
    using System.Threading.Tasks;

    using Passive.Services.DatabaseService;

    public class EventServer
    {
        private static DatabaseService dbService;

        public EventServer(DatabaseService service)
        {
            dbService = service;
        }

        public async Task<EventConfig> LoadAsync(ulong guildId)
        {
            return await dbService.LoadAsync<EventConfig>($"{guildId}-Events") ?? new EventConfig
                                                                                {
                                                                                    GuildId = guildId
                                                                                };
        }

        public class EventConfig
        {
            public Task SaveAsync()
            {
                return dbService.UpdateOrStoreAsync($"{GuildId}-Events", this);
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
}
