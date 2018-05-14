using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lithium.Discord.Contexts;
using Lithium.Discord.Contexts.Paginator;

namespace Lithium.Modules
{
    public class Info : Base
    {
        [Command("invite")]
        [Summary("invite")]
        [Remarks("invite the bot")]
        public async Task InviteTask()
        {
            await ReplyAsync($"Invite the bot to your server with the following: https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591");
        }

        private EmbedBuilder SafeEmbed(EmbedBuilder input, string addition, string additiontitle, bool inline = false)
        {
            if (addition == null)
            {
                input.AddField(additiontitle, "NULL", inline);
                return input;
            }

            input.AddField(additiontitle, addition, inline);
            return input;
        }

        [Command("serverinfo")]
        [Summary("serverinfo")]
        [Remarks("Displays information about the current server")]
        public async Task ServerInfo()
        {
            var embed = new EmbedBuilder();
            var s = Context.Socket.Guild;
            embed = SafeEmbed(embed, s.Name, "Server Name", true);
            embed = SafeEmbed(embed, s.Owner.Username, "Owner", true);
            embed = SafeEmbed(embed, s.OwnerId.ToString(), "OwnerID", true);
            embed = SafeEmbed(embed, s.VoiceRegionId, "Voice Region", true);
            embed = SafeEmbed(embed, s.VerificationLevel.ToString(), "Verification Level", true);
            embed = SafeEmbed(embed, s.SplashUrl, "Splash URL", true);
            embed = SafeEmbed(embed, s.CreatedAt.Date.ToShortDateString(), "Creation Date", true);
            embed = SafeEmbed(embed, s.DefaultMessageNotifications.ToString(), "Default Notifications", true);
            embed = SafeEmbed(embed, s.DefaultChannel?.Name, "Default Channel", true);
            embed = SafeEmbed(embed, s.AFKChannel?.Name, "AFK Channel", true);
            embed = SafeEmbed(embed, s.AFKTimeout.ToString(), "AFK Timeout", true);
            embed = SafeEmbed(embed, s.MfaLevel.ToString(), "MFA Status", true);
            embed.AddField("--------------", "**User Counts**");
            embed = SafeEmbed(embed, s.MemberCount.ToString(), ":busts_in_silhouette: Total Members", true);
            embed = SafeEmbed(embed, s.Users.Count(x => x.IsBot).ToString(), ":robot: Total Bots", true);
            embed = SafeEmbed(embed, (s.MemberCount - s.Users.Count(x => x.IsBot)).ToString(),
                ":man_in_tuxedo: Total Users", true);
            embed = SafeEmbed(embed, s.Channels.Count.ToString(), ":newspaper2: Total Channels", true);
            embed = SafeEmbed(embed, $"{s.TextChannels.Count}/{s.VoiceChannels.Count}",
                ":microphone: Text/Voice Channels", true);
            embed = SafeEmbed(embed, s.Roles.Count.ToString(), ":spy: Role Count", true);
            embed.ThumbnailUrl = s.IconUrl;

            await ReplyAsync("", false, embed.Build());
        }

        [Command("Roleinfo")]
        [Summary("roleinfo <@role>")]
        [Remarks("Displays information about given Role")]
        [Alias("RI")]
        [RequireContext(ContextType.Guild)]
        public async Task RoleInfoAsync(IRole role)
        {
            var srole = ((SocketRole) role).Permissions;
            var l = new List<string>();
            if (srole.AddReactions)
                l.Add("Can Add Reactions");
            if (srole.Administrator)
                l.Add("Is Administrator");
            if (srole.AttachFiles)
                l.Add("Can Attach Files");
            if (srole.BanMembers)
                l.Add("Can Ban Members");
            if (srole.ChangeNickname)
                l.Add("Can Change Nickname");
            if (srole.Connect)
                l.Add("Can Connect");
            if (srole.CreateInstantInvite)
                l.Add("Can Create Invite");
            if (srole.DeafenMembers)
                l.Add("Can Deafen Members");
            if (srole.EmbedLinks)
                l.Add("Can Embed Links");
            if (srole.KickMembers)
                l.Add("Can Kick Members");
            if (srole.ManageChannels)
                l.Add("Can Manage Channels");
            if (srole.ManageEmojis)
                l.Add("Can Manage Emojis");
            if (srole.ManageGuild)
                l.Add("Can Manage Guild");
            if (srole.ManageMessages)
                l.Add("Can Manage Messages");
            if (srole.ManageNicknames)
                l.Add("Can Manage Nicknames");
            if (srole.ManageRoles)
                l.Add("Can Manage Roles");
            if (srole.ManageWebhooks)
                l.Add("Can Manage Webhooks");
            if (srole.MentionEveryone)
                l.Add("Can Mention Everyone");
            if (srole.MoveMembers)
                l.Add("Can Move Members");
            if (srole.MuteMembers)
                l.Add("Can Mute Members");
            if (srole.ReadMessageHistory)
                l.Add("Can Read Message History");
            if (srole.ReadMessages)
                l.Add("Can Read Messages");
            if (srole.SendMessages)
                l.Add("Can Send Messages");
            if (srole.SendTTSMessages)
                l.Add("Can Send TTS Messages");
            if (srole.Speak)
                l.Add("Can Speak");
            if (srole.UseExternalEmojis)
                l.Add("Can Use Externam Emojis");
            if (srole.UseVAD)
                l.Add("Can Use VAD");

            var list = string.Join("\n", l.ToArray());
            if (list == "")
                list = "None :(";
            var embed = new EmbedBuilder()
                .WithTitle($"RoleInfo for {role.Name}")
                .AddInlineField("Colour", $"{role.Color}")
                .AddInlineField("ID", $"{role.Id}")
                .AddInlineField("Creation Date", $"{role.CreatedAt}")
                .AddInlineField("Displayed Separately?", $"{role.IsHoisted}")
                .AddInlineField("Mentionable?", $"{role.IsMentionable}")
                .AddInlineField("Discord Generated?", $"{role.IsManaged}")
                .AddField("Permissions", list)
                .WithFooter(x =>
                {
                    x.WithText("PassiveBOT");
                    x.WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
                });

            await ReplyAsync("", false, embed.Build());
        }

        [Command("UserCount")]
        [Alias("UC")]
        [Summary("usercount")]
        [Remarks("User Count for the current server")]
        [RequireContext(ContextType.Guild)]
        public async Task Ucount()
        {
            var botlist = Context.Socket.Guild.Users.Count(x => x.IsBot);
            var mem = Context.Socket.Guild.MemberCount;
            var guildusers = mem - botlist;

            var embed = new EmbedBuilder()
                .WithTitle($"User Count for {Context.Guild.Name}")
                .AddInlineField(":busts_in_silhouette: Total Members", mem)
                .AddInlineField(":robot: Total Bots", botlist)
                .AddInlineField(":man_in_tuxedo: Total Users", guildusers)
                .AddInlineField(":newspaper2: Total Channels", Context.Socket.Guild.Channels.Count)
                .AddInlineField(":microphone: Text/Voice Channels",
                    $"{Context.Socket.Guild.TextChannels.Count}/{Context.Socket.Guild.VoiceChannels.Count}")
                .AddInlineField(":spy: Role Count", Context.Guild.Roles.Count);

            await ReplyAsync("", false, embed);
        }

        [Command("RoleList")]
        [Summary("rolelist")]
        [Remarks("Displays roles for the current server")]
        [Alias("RL")]
        [RequireContext(ContextType.Guild)]
        public async Task RoleList()
        {
            var rol = Context.Guild.Roles;
            var rolepage = new List<string>();
            var list = "";
            foreach (var role in rol.OrderByDescending(x => x.Position))
            {
                list += $"{role.Name}\n";
                if (list.Split('\n').Length >= 20)
                {
                    rolepage.Add(list);
                    list = "";
                }
            }

            rolepage.Add(list);
            var msg = new PaginatedMessage
            {
                Title = $"Roles for {Context.Guild.Name}",
                Pages = rolepage.Select(x => new PaginatedMessage.Page
                {
                    description = x
                }),

                Color = new Color(114, 137, 218)
            };

            await PagedReplyAsync(msg);
        }

        [Command("RoleMembers")]
        [Summary("rolemembers <@role> <nick/username>")]
        [Remarks("Displays a list of members with the given role")]
        [Alias("RM")]
        [RequireContext(ContextType.Guild)]
        public async Task Rmem(IRole role, [Remainder] [Optional] string type)
        {
            var id = role.Id;
            var guild = Context.Socket.Guild;
            var members = new List<string>();
            var list = "";
            if (guild != null)
                foreach (var user in guild.Users)
                {
                    if (user.Roles.Contains(Context.Guild.GetRole(id)))
                        if (type == "username")
                            list += user.Username + "\n";
                        else
                            list += user.Nickname == null ? $"{user.Username}\n" : $"{user.Nickname}\n";
                    if (list.Split('\n').Length >= 20)
                    {
                        members.Add(list);
                        list = "";
                    }
                }

            members.Add(list);
            var msg = new PaginatedMessage
            {
                Title = $"Here is a list of Members with the role {role}",
                Pages = members.Select(x => new PaginatedMessage.Page
                {
                    description = x
                }),
                Color = new Color(114, 137, 218)
            };

            await PagedReplyAsync(msg);
        }
    }
}