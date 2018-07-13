namespace Lithium.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.WebSocket;

    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Handlers;

    /// <summary>
    /// The guild model.
    /// </summary>
    public class GuildModel
    {
        public GuildModel(ulong id)
        {
            ID = id;
        }

        public ulong ID { get; }

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

        public async Task CheckWarnLimitsAsync(SocketGuildUser user, SocketGuildUser client, SocketTextChannel channel, Moderation.ModEvent modEvent)
        {
            // Ensure that only warnings that apply to the user and aren't expired are counted
            var currentWarns = ModerationSetup.ModActions.Where(m => m.UserId == user.Id && !m.ExpiredOrRemoved && m.Action == Moderation.ModEvent.EventType.Warn).ToList();

            var warnCount = currentWarns.Count;

            // Check to see if the user's warnings are greater 
            ModerationSetup.Settings.AutoTasks.TryGetValue(warnCount, out var autoAction);

            if (autoAction == null)
            {
                var matchTasks = ModerationSetup.Settings.AutoTasks.Where(a => a.Key < warnCount).ToList();
                if (matchTasks.Any())
                {
                    var maxTask = matchTasks.Max(t => t.Key);
                    autoAction = matchTasks.FirstOrDefault(m => m.Key == maxTask).Value;
                }
                else
                {
                    return;
                }
            }

            if (autoAction != null)
            {
                switch (autoAction.LimitAction)
                {
                    case Moderation.ModerationSettings.WarnLimitAction.NoAction:
                        break;
                    case Moderation.ModerationSettings.WarnLimitAction.Kick:
                        await ModActionAsync(user, client, channel, null, modEvent.AutoModReason, Moderation.ModEvent.EventType.Kick, modEvent.ReasonTrigger, autoAction.AutoActionExpiry);
                        break;
                    case Moderation.ModerationSettings.WarnLimitAction.Mute:
                        await ModActionAsync(user, client, channel, null, modEvent.AutoModReason, Moderation.ModEvent.EventType.Mute, modEvent.ReasonTrigger, autoAction.AutoActionExpiry);
                        break;
                    case Moderation.ModerationSettings.WarnLimitAction.Ban:
                        await ModActionAsync(user, client, channel, null, modEvent.AutoModReason, Moderation.ModEvent.EventType.Ban, modEvent.ReasonTrigger, autoAction.AutoActionExpiry);
                        break;
                }
            }
        }

        public Task ModLogAsync(SocketGuild guild, EmbedBuilder embed)
        {
            if (guild.GetTextChannel(ModerationSetup.Settings.ModLogChannel) is SocketTextChannel modChannel)
            {
                return modChannel.SendMessageAsync("", false, embed.Build());
            }

            return Task.CompletedTask;
        }

        public async Task ModActionAsync(SocketGuildUser user, SocketGuildUser moderator, SocketTextChannel channel, string reason, Moderation.ModEvent.AutoReason autoModReason, Moderation.ModEvent.EventType modAction, Moderation.ModEvent.Trigger trigger, TimeSpan? expires)
        {
            if (user.Id == moderator.Id)
            {
                throw new Exception("User can not be the same as the moderator");
            }

            if (expires == null && autoModReason != Moderation.ModEvent.AutoReason.none)
            {
                if (ModerationSetup.Settings.WarnExpiryTime != null && modAction == Moderation.ModEvent.EventType.Warn)
                {
                    expires = ModerationSetup.Settings.WarnExpiryTime;
                }
                else if (modAction == Moderation.ModEvent.EventType.Ban && ModerationSetup.Settings.AutoBanExpiry != null)
                {
                    expires = ModerationSetup.Settings.AutoBanExpiry;
                }
                else if (modAction == Moderation.ModEvent.EventType.Mute && ModerationSetup.Settings.AutoMuteExpiry != null)
                {
                    expires = ModerationSetup.Settings.AutoMuteExpiry;
                }
            }

            var modEvent = new Moderation.ModEvent
            {
                Action = modAction,
                ExpiryDate = expires == null ? null : DateTime.UtcNow + expires,
                ModName = moderator.ToString(),
                ModId = moderator.Id,
                UserId = user.Id,
                UserName = user.ToString(),
                AutoModReason = autoModReason,
                ProvidedReason = reason,
                ReasonTrigger = trigger,
                ActionId = ModerationSetup.ModActions.Count
            };
            ModerationSetup.ModActions.Add(modEvent);
            Save();

            var embed = new EmbedBuilder
            {
                Fields = new List<EmbedFieldBuilder>
                                             {
                                                 new EmbedFieldBuilder
                                                     {
                                                         Name =
                                                             $"{user} was {modAction.GetDescription()} [#{modEvent.ActionId}]",
                                                         Value =
                                                             $"**Mod:** {moderator.Mention}\n" +
                                                             $"**Expires:** {(modEvent.ExpiryDate.HasValue ? $"{modEvent.ExpiryDate.Value.ToLongDateString()} {modEvent.ExpiryDate.Value.ToLongTimeString()}\n" : "Never\n")}" +
                                                             (modEvent.AutoModReason == Moderation.ModEvent.AutoReason.none ? $"**Reason:** {reason ?? "N/A"}\n" : $"**Auto-Reason:** {modEvent.AutoModReason}\n")
                                                     }
                                             }
            }.WithCurrentTimestamp();

            var request = new RequestOptions
            {
                AuditLogReason =
                                                         $"Mod: {moderator.Username} [{moderator.Id}]\n"
                                                         + $"Action: {modAction}\n"
                                                         + (modEvent.AutoModReason == Moderation.ModEvent.AutoReason.none ? $"**Reason:** {reason ?? "N/A"}\n" : $"**Auto-Reason:** {modEvent.AutoModReason}\n")
                                                         + $"Trigger: {trigger?.Message}\n"
            };
            bool success = true;

            if (modAction == Moderation.ModEvent.EventType.Warn)
            {
                embed.Color = Color.DarkTeal;
            }
            else if (modAction == Moderation.ModEvent.EventType.Kick)
            {
                embed.Color = Color.DarkRed;
                try
                {
                    await user.KickAsync(reason, request);
                }
                catch (Exception e)
                {
                    LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    await channel.SendMessageAsync(e.ToString());
                    success = false;
                }
            }
            else if (modAction == Moderation.ModEvent.EventType.Mute)
            {
                embed.Color = Color.DarkPurple;
                IRole muteRole = user.Guild.GetRole(ModerationSetup.Settings.MutedRoleId);
                if (muteRole == null)
                {
                    muteRole = user.Guild.Roles.FirstOrDefault(r => r.Name.Equals("Muted", StringComparison.OrdinalIgnoreCase));

                    if (muteRole == null)
                    {
                        muteRole = await user.Guild.CreateRoleAsync("Muted", GuildPermissions.None);
                        await channel.SendMessageAsync("", false, new EmbedBuilder
                        {
                            Description = $"Muted Role was not configured, I have auto-generated one for you. {muteRole.Mention} has been auto-set as the server's mute role"
                        }.Build());
                        ModerationSetup.Settings.MutedRoleId = muteRole.Id;
                    }
                    else
                    {
                        await channel.SendMessageAsync("", false, new EmbedBuilder
                        {
                            Description = $"Muted Role was not configured, but one was found. {muteRole.Mention} has been auto-set as the server's mute role"
                        }.Build());
                        ModerationSetup.Settings.MutedRoleId = muteRole.Id;
                    }

                    Save();
                }

                foreach (var guildChannel in user.Guild.Channels)
                {
                    try
                    {
                        var _ = Task.Run(() => guildChannel.AddPermissionOverwriteAsync(muteRole, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny, attachFiles: PermValue.Deny, connect: PermValue.Deny, speak: PermValue.Deny, mentionEveryone: PermValue.Deny)));
                    }
                    catch (Exception e)
                    {
                        LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }

                try
                {
                    await user.AddRoleAsync(muteRole, request);
                }
                catch (Exception e)
                {
                    LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    await channel.SendMessageAsync(e.ToString());
                    success = false;
                }
            }
            else if (modAction == Moderation.ModEvent.EventType.Ban)
            {
                embed.Color = Color.DarkOrange;

                try
                {
                    await user.Guild.AddBanAsync(user, ModerationSetup.Settings.PruneDays, reason, request);
                }
                catch (Exception e)
                {
                    LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    await channel.SendMessageAsync(e.ToString());
                    success = false;
                }
            }

            var m = await channel.SendMessageAsync("", false, embed.Build());
            if (ModerationSetup.Settings.AutoHideDelay.HasValue)
            {
                var deleteAction = Task.Delay(ModerationSetup.Settings.AutoHideDelay.Value)
                    .ContinueWith(_ => m.DeleteAsync().ConfigureAwait(false));
            }

            if (user.Guild.GetTextChannel(ModerationSetup.Settings.ModLogChannel) is SocketTextChannel modChannel)
            {
                if (modEvent.AutoModReason != Moderation.ModEvent.AutoReason.none)
                {
                    if (trigger != null)
                    {
                        embed.AddField("Trigger", $"In {user.Guild.GetTextChannel(trigger.ChannelId)?.Mention}\n**Message:**\n{trigger.Message}");
                    }
                }

                if (!success)
                {
                    embed.AddField("FATAL ERROR", "Unable to mod action for user");
                }

                await ModLogAsync(user.Guild, embed);
            }

            if (modAction == Moderation.ModEvent.EventType.Warn)
            {
                await CheckWarnLimitsAsync(user, channel.Guild.CurrentUser, channel, modEvent);
            }
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

            public List<ModEvent> ModActions { get; set; } = new List<ModEvent>();

            public ModerationSettings Settings { get; set; } = new ModerationSettings();

            public class ModerationSettings
            {
                public enum WarnLimitAction
                {
                    NoAction,
                    [Description("Kicked")]
                    Kick,
                    [Description("Banned")]
                    Ban,
                    [Description("Muted")]
                    Mute
                }

                // Hide Actions after x seconds (outside of mod log)
                public TimeSpan? AutoHideDelay { get; set; } = TimeSpan.FromSeconds(5);

                public TimeSpan? WarnExpiryTime { get; set; } = TimeSpan.FromDays(7);

                public TimeSpan? AutoMuteExpiry { get; set; } = TimeSpan.FromMinutes(30);

                public TimeSpan? AutoBanExpiry { get; set; } = null;

                public int PruneDays { get; set; } = 1;

                public ulong ModLogChannel { get; set; } = 0;

                public ulong MutedRoleId { get; set; } = 0;

                /// <summary>
                /// Gets or sets a dictionary containing actions that wll be taken once a user reaches a certain amount of warnings
                /// </summary>
                public Dictionary<int, AutoAction> AutoTasks { get; set; } = new Dictionary<int, AutoAction>();

                public class AutoAction
                {
                    public WarnLimitAction LimitAction { get; set; } = WarnLimitAction.NoAction;

                    // Warnings before doing a specific action.
                    public int WarnLimit { get; set; } = int.MaxValue;

                    public string Response { get; set; } = "Warn Limit Exceeded";

                    public TimeSpan? AutoActionExpiry { get; set; } = null;
                }
            }

            public class ModEvent
            {
                public enum EventType
                {
                    [Description("Kicked")]
                    Kick,
                    [Description("Warned")]
                    Warn,
                    [Description("Banned")]
                    Ban,
                    [Description("Muted")]
                    Mute
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
                    exceededWarnLimit,
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

                public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

                public int ActionId { get; set; }
            }
        }

        public class CommandAccess
        {
            public List<CustomPermission> CustomizedPermission { get; set; } = new List<CustomPermission>();

            public class CustomPermission
            {
                public bool IsCommand { get; set; } = true;

                public string Name { get; set; }

                public DefaultPermissionLevel Setting { get; set; }
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