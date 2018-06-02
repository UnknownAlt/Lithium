using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Lithium.Handlers;

namespace Lithium.Models
{
    public class GuildModel
    {
        public class Guild
        {
            public ulong GuildID { get; set; }
            public Moderation ModerationSetup { get; set; } = new Moderation();
            public settings Settings { get; set; } = new settings();
            public autochannels AutoMessage { get; set; } = new autochannels();
            public antispams Antispam { get; set; } = new antispams();
            public Eventlogger EventLogger { get; set; } = new Eventlogger();
            public ticketing Tickets { get; set; } = new ticketing();
            public tags Tags { get; set; } = new tags();

            public void Save()
            {
                using (var Session = DatabaseHandler.Store.OpenSession(DatabaseHandler.DBName))
                {
                    Session.Store(this, GuildID.ToString());
                    Session.SaveChanges();
                }
            }

            public async Task TicketLog(EmbedBuilder embed, IGuild guild)
            {
                if (!Tickets.Settings.useticketing) return;
                if (Tickets.Settings.ticketchannelid != 0)
                {
                    if (await guild.GetChannelAsync(Tickets.Settings.ticketchannelid) is IMessageChannel channel)
                    {
                        try
                        {
                            await channel.SendMessageAsync("", false, embed.Build());
                        }
                        catch
                        {
                            //
                        }
                    }
                }
            }

            public async Task ModLog(EmbedBuilder embed, IGuild guild)
            {
                if (ModerationSetup.Settings.ModLogChannel != 0)
                {
                    if (await guild.GetChannelAsync(ModerationSetup.Settings.ModLogChannel) is IMessageChannel channel)
                    {
                        try
                        {
                            await channel.SendMessageAsync("", false, embed.Build());
                        }
                        catch
                        {
                            //
                        }
                    }
                }
            }

            public async Task EventLog(EmbedBuilder embed, IGuild guild)
            {
                if (EventLogger.EventChannel != 0 && EventLogger.LogEvents)
                {
                    if (await guild.GetChannelAsync(EventLogger.EventChannel) is IMessageChannel channel)
                    {
                        try
                        {
                            await channel.SendMessageAsync("", false, embed.Build());
                        }
                        catch
                        {
                            //
                        }
                    }
                }
            }


            public async Task AddWarn(string reason, IGuildUser User, IUser mod, IMessageChannel channel, string message = null)
            {
                ModerationSetup.Warns.Add(new Moderation.warn
                {
                    modID = mod.Id,
                    modname = mod.Username,
                    reason = reason,
                    userID = User.Id,
                    username = User.Username
                });

                var embed = new EmbedBuilder
                {
                    Title = $"{User.Username} has been Warned {(ModerationSetup.Settings.WarnLimitAction != Moderation.msettings.warnLimitAction.NoAction ? $"[{ModerationSetup.Warns.Count(x => x.userID == User.Id)}/{ModerationSetup.Settings.warnlimit}]" : "")}",
                    Color = Color.DarkPurple
                }
                .AddField("User", $"{User.Username}#{User.Discriminator} ({User.Mention})\n" +
                                  $"`[{User.Id}]`", true)
                .AddField("Moderator", $"{mod.Username}#{mod.Discriminator}", true)
                .AddField("Reason", $"{reason ?? "N/A"}", true)
                .AddField("Context", $"Channel: {channel.Name}\n" +
                                     $"Time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}", true);

                var replymsg = await channel.SendMessageAsync("", false, embed.Build());
                if (message != null)
                {
                    if (message.Length > 1024)
                    {
                        message = message.Substring(0, 1020) + "...";
                    }
                    embed.AddField("Message", $"{message}");
                }
                await ModLog(embed, User.Guild);
                if (ModerationSetup.Warns.Count(x => x.userID == User.Id) > ModerationSetup.Settings.warnlimit && ModerationSetup.Settings.WarnLimitAction != Moderation.msettings.warnLimitAction.NoAction)
                {
                    var embedmsg = new EmbedBuilder();
                    if (ModerationSetup.Settings.WarnLimitAction == Moderation.msettings.warnLimitAction.Ban)
                    {
                        ModerationSetup.Bans.Add(new Moderation.ban
                        {
                            modID = mod.Id,
                            modname = mod.Username,
                            reason = reason,
                            userID = User.Id,
                            username = User.Username,
                            Expires = false
                        });
                        await User.Guild.AddBanAsync(User, 1, "AutoBan, Warnlimit Exceeded by user!");
                        embedmsg.Title = $"{User.Username} has been Auto banned";
                        embedmsg.Description = $"User: {User.Username}#{User.Discriminator}\n" +
                                               $"UserID: {User.Id}\n" +
                                               $"Mod: {mod.Username}#{mod.Discriminator}\n" +
                                               $"Mod ID: {mod.Id}\n" +
                                               "Reason:\n" +
                                               "AutoBan, Warnlimit Exceeded by user!";
                        embedmsg.Color = Color.DarkRed;
                    }
                    else
                    {
                        ModerationSetup.Kicks.Add(new Moderation.kick
                        {
                            modID = mod.Id,
                            modname = mod.Username,
                            reason = reason,
                            userID = User.Id,
                            username = User.Username
                        });
                        await User.KickAsync("AutoKick, WarnLimit Exceeded by user!");
                        embedmsg.Title = $"{User.Username} has been Auto Kicked";
                        embedmsg.Description = $"User: {User.Username}#{User.Discriminator}\n" +
                                               $"UserID: {User.Id}\n" +
                                               $"Mod: {mod.Username}#{mod.Discriminator}\n" +
                                               $"Mod ID: {mod.Id}\n" +
                                               "Reason:\n" +
                                               "Auto Kick, Warnlimit Exceeded by user!";
                        embedmsg.Color = Color.DarkMagenta;
                    }

                    await channel.SendMessageAsync("", false, embedmsg.Build());
                    if (message != null)
                    {
                        if (message.Length > 1024)
                        {
                            message = message.Substring(0, 1020) + "...";
                        }
                        embedmsg.AddField("Message", $"{message}");
                    }
                    await ModLog(embedmsg, User.Guild);
                }

                if (ModerationSetup.Settings.hidewarnafterdelay)
                {
                    _ = Task.Delay(TimeSpan.FromSeconds(5))
                        .ContinueWith(_ => replymsg.DeleteAsync().ConfigureAwait(false))
                        .ConfigureAwait(false);
                }
            }

            public class Eventlogger
            {
                public bool LogEvents { get; set; } = false;
                public ulong EventChannel { get; set; } = 0;
                public ELSettings Settings { get; set; } = new ELSettings();

                public class ELSettings
                {
                    public bool guildmemberupdated { get; set; } = false;
                    public bool guilduserjoined { get; set; } = true;
                    public bool guilduserleft { get; set; } = true;
                    public bool guilduserbanned { get; set; } = true;
                    public bool guilduserunbanned { get; set; } = true;
                    public bool messageupdated { get; set; } = false;
                    public bool messagedeleted { get; set; } = false;
                    public bool channelcreated { get; set; } = true;
                    public bool channeldeleted { get; set; } = true;
                    public bool channelupdated { get; set; } = false;
                }
            }

            public class Moderation
            {
                public List<ulong> ModeratorRoles { get; set; } = new List<ulong>();
                public List<ulong> AdminRoles { get; set; } = new List<ulong>();
                public List<kick> Kicks { get; set; } = new List<kick>();
                public List<warn> Warns { get; set; } = new List<warn>();
                public List<ban> Bans { get; set; } = new List<ban>();
                public msettings Settings { get; set; } = new msettings();
                public muted Mutes { get; set; } = new muted();

                public class msettings
                {
                    public enum warnLimitAction
                    {
                        NoAction,
                        Kick,
                        Ban
                    }

                    //Hide Warnings after 5 seconds (outside of mod log)
                    public bool hidewarnafterdelay { get; set; } = true;

                    //Warnings before doing a specific action.
                    public int warnlimit { get; set; } = int.MaxValue;
                    public warnLimitAction WarnLimitAction { get; set; } = warnLimitAction.NoAction;

                    public ulong ModLogChannel { get; set; } = 0;
                }

                public class muted
                {
                    public ulong mutedrole { get; set; } = 0;
                    public List<muteduser> MutedUsers { get; set; } = new List<muteduser>();

                    public class muteduser
                    {
                        public ulong userid { get; set; }
                        public bool expires { get; set; } = false;
                        public DateTime expiry { get; set; } = DateTime.UtcNow;
                    }
                }

                public class kick
                {
                    public ulong userID { get; set; }
                    public string username { get; set; }
                    public string reason { get; set; } = "N/A";

                    public string modname { get; set; }
                    public ulong modID { get; set; }
                }

                public class warn
                {
                    public ulong userID { get; set; }
                    public string username { get; set; }
                    public string reason { get; set; } = "N/A";

                    public string modname { get; set; }
                    public ulong modID { get; set; }
                }

                public class ban
                {
                    public ulong userID { get; set; }
                    public string username { get; set; }
                    public string reason { get; set; } = "N/A";

                    public string modname { get; set; }
                    public ulong modID { get; set; }

                    public bool Expires { get; set; }
                    public DateTime ExpiryDate { get; set; } = DateTime.MaxValue;
                }
            }

            public class settings
            {
                public string Prefix { get; set; } = Config.Load().DefaultPrefix;
                public visibilityconfig DisabledParts { get; set; } = new visibilityconfig();

                public class visibilityconfig
                {
                    public List<string> BlacklistedModules { get; set; } = new List<string>();
                    public List<string> BlacklistedCommands { get; set; } = new List<string>();
                }
            }

            public class autochannels
            {
                public List<autochannel> AutoChannels { get; set; } = new List<autochannel>();

                public class autochannel
                {
                    public acsettings Settings { get; set; } = new acsettings();

                    public ulong channelID { get; set; }

                    public string title { get; set; } = "AutoMessage";
                    public string automessage { get; set; } = "Lithium";
                    public string ImgURL { get; set; } = null;

                    public class acsettings
                    {
                        public bool enabled { get; set; } = false;
                        public int msgcount { get; set; } = 0;
                        public int sendlimit { get; set; } = 50;
                    }
                }
            }

            public class ticketing
            {
                public tsettings Settings { get; set; } = new tsettings();

                public List<ticket> tickets { get; set; } = new List<ticket>();

                public class tsettings
                {
                    public ulong ticketchannelid { get; set; } = 0;
                    public bool useticketing { get; set; } = false;
                    public bool allowAnyUserToCreate { get; set; } = true;
                    public List<ulong> AllowedCreationRoles { get; set; } = new List<ulong>();
                }

                public class ticket
                {
                    public int id { get; set; }
                    public bool solved { get; set; } = false;
                    public string message { get; set; }
                    public ulong InitUser { get; set; }
                    public string solvedmessage { get; set; } = null;

                    public List<ulong> Up { get; set; } = new List<ulong>();
                    public List<ulong> Down { get; set; } = new List<ulong>();


                    public List<comment> comments { get; set; } = new List<comment>();

                    public class comment
                    {
                        public int id { get; set; }
                        public ulong UserID { get; set; }
                        public string Comment { get; set; }


                        public List<ulong> Up { get; set; } = new List<ulong>();
                        public List<ulong> Down { get; set; } = new List<ulong>();
                    }
                }
            }

            public class tags
            {
                public List<tag> Tags = new List<tag>();
                public tsettings Settings { get; set; } = new tsettings();

                public class tsettings
                {
                    public bool AllowAllUsersToCreate { get; set; } = false;
                }

                public class tag
                {
                    public string name { get; set; }
                    public string content { get; set; }
                    public string imgURL { get; set; } = null;
                    public int uses { get; set; } = 0;
                    public ulong ownerID { get; set; }
                }
            }

            public class antispams
            {
                public blacklist Blacklist { get; set; } = new blacklist();
                public antispam Antispam { get; set; } = new antispam();
                public advertising Advertising { get; set; } = new advertising();
                public mention Mention { get; set; } = new mention();
                public privacy Privacy { get; set; } = new privacy();
                public toxicity Toxicity { get; set; } = new toxicity();

                public List<IgnoreRole> IgnoreRoles { get; set; } = new List<IgnoreRole>();

                public class toxicity
                {
                    public bool WarnOnDetection { get; set; } = false;
                    public bool UsePerspective { get; set; } = false;
                    public int ToxicityThreshHold { get; set; } = 90;
                }


                public class antispam
                {
                    //remove repetitive messages and messages posted in quick succession
                    public bool NoSpam { get; set; } = false;

                    //words to skip while using antispam
                    public List<string> AntiSpamSkip { get; set; } = new List<string>();

                    //Toggle wether or not to use antispam on bot commands
                    public bool IgnoreCommandMessages { get; set; } = true;

                    public bool antiraid { get; set; } = false;
                    public bool WarnOnDetection { get; set; } = false;
                }

                public class blacklist
                {
                    //the blacklist word groupings
                    public List<BlacklistWords> BlacklistWordSet { get; set; } = new List<BlacklistWords>();
                    public string DefaultBlacklistMessage { get; set; } = "";

                    public bool WarnOnDetection { get; set; } = false;

                    public class BlacklistWords
                    {
                        //Words for the specified blacklist message
                        public List<string> WordList { get; set; } = new List<string>();

                        //Custom response for certain words.
                        public string BlacklistResponse { get; set; } = null;
                    }
                }

                public class advertising
                {
                    public string NoInviteMessage { get; set; } = null;

                    //blacklist for discord invites
                    public bool Invite { get; set; } = false;
                    public bool WarnOnDetection { get; set; } = false;
                }

                public class mention
                {
                    public string MentionAllMessage { get; set; } = null;

                    //blacklist for @everyone and @here 
                    public bool MentionAll { get; set; } = false;

                    //Remove 5+ mentions of roles or users
                    public bool RemoveMassMention { get; set; } = false;
                    public bool WarnOnDetection { get; set; } = false;
                }

                public class privacy
                {
                    //remove all ip addresses posted in the format x.x.x.x
                    public bool RemoveIPs { get; set; } = false;
                    public bool WarnOnDetection { get; set; } = false;
                }

                public class IgnoreRole
                {
                    public ulong RoleID { get; set; }

                    //false = filter
                    //true = bypass filter
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
}