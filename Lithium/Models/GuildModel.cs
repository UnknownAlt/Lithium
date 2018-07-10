namespace Lithium.Models
{
    using System;
    using System.Collections.Generic;

    using global::Discord;

    using Lithium.Handlers;

    /// <summary>
    /// The guild model.
    /// </summary>
    public class GuildModel
    {
        public ulong ID { get; set; }

        public Moderation ModerationSetup { get; set; } = new Moderation();

        public CommandAccess CustomAccess { get; set; } = new CommandAccess();

        public AntiSpamSetup AntiSpam { get; set; } = new AntiSpamSetup();

        public EventConfig EventLogger { get; set; } = new EventConfig();

        public Ticketing Tickets { get; set; } = new Ticketing();

        /// <summary>
        /// Saves the GuildModel
        /// </summary>
        public void Save()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                session.Store(this, ID.ToString());
                session.SaveChanges();
            }
        }

        public void ModAction(IGuildUser user, IGuildUser moderator, string reason, Moderation.ModEvent.AutoReason autoModReason, Moderation.ModEvent.EventType modAction, Moderation.ModEvent.Trigger trigger, TimeSpan? expires)
        {
            if (modAction == Moderation.ModEvent.EventType.warn)
            {
                // TODO Warn in chat then auto-delete if applicable
            }
            else if (modAction == Moderation.ModEvent.EventType.Kick)
            {
                // TODO Kick from server and log the event
            }
            else if (modAction == Moderation.ModEvent.EventType.mute)
            {
                // TODO Check all server channels for mute perms, then set if applicable and give user role
            }
            else if (modAction == Moderation.ModEvent.EventType.ban)
            {
                // TODO Ban user from server, log in mod channel and give small message in chat
            }

            var modEvent = new Moderation.ModEvent
                               {
                                   Action = modAction,
                                   ExpiryDate = DateTime.UtcNow + expires,
                                   ModName = moderator.Username,
                                   ModId = moderator.Id,
                                   UserId = user.Id,
                                   UserName = user.Username,
                                   AutoModReason = autoModReason,
                                   ProvidedReason = reason,
                                   ReasonTrigger = trigger
                               };
            ModerationSetup.ModActions.Add(user.Id, modEvent);

            Save();
        }

        public class EventConfig
        {
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

        public class Moderation
        {
            public List<ulong> ModeratorRoles { get; set; } = new List<ulong>();

            public List<ulong> AdminRoles { get; set; } = new List<ulong>();

            public Dictionary<ulong, ModEvent> ModActions { get; set; } = new Dictionary<ulong, ModEvent>();

            public ModerationSettings Settings { get; set; } = new ModerationSettings();

            public class ModerationSettings
            {
                public enum WarnLimitAction
                {
                    NoAction,
                    Kick,
                    Ban,
                    Mute
                }

                // Hide Actions after x seconds (outside of mod log)
                public TimeSpan? AutoHideDelay { get; set; } = TimeSpan.FromSeconds(5);

                public TimeSpan? WarnExpiryTime { get; set; } = TimeSpan.FromDays(7);

                public TimeSpan? AutoMuteExpiry { get; set; } = TimeSpan.FromMinutes(30);

                public TimeSpan? AutoBanExpiry { get; set; } = null;

                public ulong ModLogChannel { get; set; } = 0;

                public ulong MutedRoleId { get; set; } = 0;

                public Dictionary<int, AutoAction> AutoTasks { get; set; } = new Dictionary<int, AutoAction>();

                public class AutoAction
                {
                    public WarnLimitAction LimitAction { get; set; } = WarnLimitAction.NoAction;

                    // Warnings before doing a specific action.
                    public int WarnLimit { get; set; } = int.MaxValue;

                    public TimeSpan? AutoActionExpiry { get; set; } = null;
                }
            }

            public class ModEvent
            {
                public enum EventType
                {
                    Kick,
                    warn,
                    ban,
                    mute
                }

                public enum AutoReason
                {
                    blacklist,
                    messageSpam,
                    discordInvites,
                    ipAddresses,
                    massMention,
                    mentionAll,
                    toxicity,
                    none
                }

                public AutoReason AutoModReason { get; set; } = AutoReason.none;

                public class Trigger
                {
                    public string Message { get; set; }

                    public ulong ChannelId { get; set; }
                }

                public EventType Action { get; set; }

                public Trigger ReasonTrigger { get; set; }

                public ulong UserId { get; set; }

                public string UserName { get; set; }

                public string ProvidedReason { get; set; } = "N/A";

                public string ModName { get; set; }

                public ulong ModId { get; set; }

                public DateTime? ExpiryDate { get; set; } = DateTime.MaxValue;

                public bool ExpiredOrRemoved { get; set; } = false;
            }
        }

        public class CommandAccess
        {
            public List<CustomPermission> CustomizedPermission { get; set; } = new List<CustomPermission>();

            public class CustomPermission
            {
                public enum AccessType
                {
                    ServerOwner,
                    Admin,
                    Moderator,
                    All
                }

                public bool IsCommand { get; set; } = true;

                public string Name { get; set; }

                public AccessType Setting { get; set; } = AccessType.Admin;
            }
        }

        public class Ticketing
        {
            public TicketSettings Settings { get; set; } = new TicketSettings();

            public List<Ticket> Tickets { get; set; } = new List<Ticket>();

            public class TicketSettings
            {
                public ulong TicketChannelId { get; set; } = 0;

                public bool UsingTickets { get; set; } = false;
            }

            public class Ticket
            {
                // The message ID of the original ticket
                public ulong TicketMessageId { get; set; }

                // Ticket ID
                public int TicketId { get; set; }

                // Info about current ticket
                public string Message { get; set; }

                public ulong CreatorID { get; set; }

                // Solved Status
                public bool Solved { get; set; } = false;

                public string SolvedMessage { get; set; } = null;

                // List of user IDs for upVotes and downVotes
                public List<ulong> UpVotes { get; set; } = new List<ulong>();

                public List<ulong> DownVotes { get; set; } = new List<ulong>();

                public List<Comment> Comments { get; set; } = new List<Comment>();

                public class Comment
                {
                    public int CommentId { get; set; }

                    public ulong CreatorId { get; set; }

                    public string Message { get; set; }

                    public List<ulong> UpVotes { get; set; } = new List<ulong>();

                    public List<ulong> DownVotes { get; set; } = new List<ulong>();
                }
            }
        }

        public class AntiSpamSetup
        {
            public BlacklistSettings Blacklist { get; set; } = new BlacklistSettings();

            public AntiSpamSettings AntiSpam { get; set; } = new AntiSpamSettings();

            public AdvertisingSettings Advertising { get; set; } = new AdvertisingSettings();

            public MentionSettings Mention { get; set; } = new MentionSettings();

            public PrivacySettings Privacy { get; set; } = new PrivacySettings();

            public ToxicitySettings Toxicity { get; set; } = new ToxicitySettings();

            // public List<IgnoreRole> IgnoreRoles { get; set; } = new List<IgnoreRole>();

            // RoleId, Ignore Settings
            public Dictionary<ulong, IgnoreRole> IgnoreList { get; set; } = new Dictionary<ulong, IgnoreRole>();

            public class ToxicitySettings
            {
                public bool WarnOnDetection { get; set; } = false;

                public bool UsePerspective { get; set; } = false;

                public int ToxicityThreshold { get; set; } = 90;
            }
            
            public class AntiSpamSettings
            {
                // remove repetitive messages and messages posted in quick succession
                public bool NoSpam { get; set; } = false;

                // words to skip while using anti-spam
                public List<string> WhiteList { get; set; } = new List<string>();

                // Toggle whether or not to use anti-spam on bot commands
                public bool IgnoreCommandMessages { get; set; } = true;

                public bool WarnOnDetection { get; set; } = false;

                public AntiSpamRate RateLimiting { get; set; } = new AntiSpamRate();

                public class AntiSpamRate
                {
                    public int MessageCount { get; set; } = 5;

                    public TimeSpan TimePeriod { get; set; } = TimeSpan.FromSeconds(5);

                    public TimeSpan BlockPeriod { get; set; } = TimeSpan.FromSeconds(5);
                }
            }

            public class BlacklistSettings
            {
                // the blacklist word groupings
                public List<BlacklistWords> BlacklistWordSet { get; set; } = new List<BlacklistWords>();

                public string DefaultBlacklistMessage { get; set; } = "";

                public bool WarnOnDetection { get; set; } = false;

                public class BlacklistWords
                {
                    // Words for the specified blacklist message
                    public List<string> WordList { get; set; } = new List<string>();

                    // Custom response for certain words.
                    public string BlacklistResponse { get; set; } = null;

                    // Group word list by a group Id to make grouping easier
                    public int GroupId { get; set; }
                }
            }

            public class AdvertisingSettings
            {
                public string Response { get; set; } = null;

                // blacklist for discord invites
                public bool Invite { get; set; } = false;

                public bool WarnOnDetection { get; set; } = false;
            }

            public class MentionSettings
            {
                public string MentionAllResponse { get; set; } = null;

                // blacklist for @everyone and @here 
                public bool MentionAll { get; set; } = false;

                public string MassMentionResponse { get; set; } = null;

                // Remove messages with too many user or role mentions
                public bool RemoveMassMention { get; set; } = false;

                public int MassMentionLimit { get; set; } = 5;

                public bool WarnOnDetection { get; set; } = false;
            }

            public class PrivacySettings
            {
                // remove all ip addresses posted in the format x.x.x.x
                public bool RemoveIPs { get; set; } = false;

                public string Response { get; set; } = null;

                public bool WarnOnDetection { get; set; } = false;
            }

            public class IgnoreRole
            {
                // false = filter
                // true = bypass filter
                public bool AntiSpam { get; set; } = false;

                public bool Blacklist { get; set; } = false;

                public bool Advertising { get; set; } = false;

                public bool Mention { get; set; } = false;

                public bool Privacy { get; set; } = false;

                public bool Toxicity { get; set; } = false;
            }
        }
    }
}