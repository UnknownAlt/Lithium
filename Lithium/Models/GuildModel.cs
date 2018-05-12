using System;
using System.Collections.Generic;
using System.Text;

namespace Lithium.Models
{
    public class GuildModel
    {
        public List<Guild> Guilds { get; set; }= new List<Guild>();
        public class Guild
        {
            public ulong GuildID { get; set; }
            public Moderation ModerationSetup { get; set; } = new Moderation();
            public Settings settings { get; set; } = new Settings();
            public autochannels AutoMessageChannels { get; set; } = new autochannels();
            public antispams Antispam { get; set; } = new antispams();
            public class Moderation
            {
                public List<ulong> ModeratorRoles { get; set; } = new List<ulong>();
                public List<ulong> AdminRoles { get; set; } = new List<ulong>();
                public List<kick> Kicks { get; set; } = new List<kick>();
                public List<warn> Warns { get; set; } = new List<warn>();
                public List<ban> Bans { get; set; } = new List<ban>();
                public class kick
                {
                    public ulong userID { get; set; }
                    public string username { get; set; }
                    public string reason { get; set; }

                    public string modname { get; set; }
                    public ulong modID { get; set; }
                }
                public class warn
                {
                    public ulong userID { get; set; }
                    public string username { get; set; }
                    public string reason { get; set; }

                    public string modname { get; set; }
                    public ulong modID { get; set; }
                }
                public class ban
                {
                    public ulong userID { get; set; }
                    public string username { get; set; }
                    public string reason { get; set; }

                    public string modname { get; set; }
                    public ulong modID { get; set; }

                    public DateTime Expires { get; set; } = DateTime.MaxValue;
                }
            }

            public class Settings
            {
                public string Prefix { get; set; } = Config.Load().DefaultPrefix;
            }

            public class autochannels
            {
                public bool enabled { get; set; } = false;
                public ulong channelID { get; set; }
                public int messages { get; set; } = 0;
                public string automessage { get; set; } = "PassiveBOT";
                public int sendlimit { get; set; } = 50;
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

                    //toggle wether or not to filter diatrics and replace certain numbers with their letter counterparts etc.
                    public bool BlacklistBetterFilter { get; set; } = false;
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
