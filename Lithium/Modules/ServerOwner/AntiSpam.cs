using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Preconditions;

namespace Lithium.Modules.ServerOwner
{
    [RequireRole.RequireAdmin]
    public class AntiSpam : Base
    {
        [Command("NoInvite")]
        [Summary("NoInvite")]
        [Remarks("Disable the posting of discord invite links in the server")]
        public async Task NoInvite()
        {
            Context.Server.Antispam.Advertising.Invite = !Context.Server.Antispam.Advertising.Invite;
            Context.Server.Save();
            await ReplyAsync($"NoInvite: {Context.Server.Antispam.Advertising.Invite}");
        }
        [Command("NoMentionAll")]
        [Summary("NoMentionAll")]
        [Remarks("Disable the use of @everyone and @here for users")]
        public async Task NoMentionAll()
        {
            Context.Server.Antispam.Mention.MentionAll = !Context.Server.Antispam.Mention.MentionAll;
            Context.Server.Save();
            await ReplyAsync($"NoMentionAll: {Context.Server.Antispam.Mention.MentionAll}");
        }
        [Command("NoMentionMessage")]
        [Summary("NoMentionMessage")]
        [Remarks("Set the No Mention Message response")]
        public async Task NoMentionAll([Remainder]string message = null)
        {
            Context.Server.Antispam.Mention.MentionAllMessage = message;
            Context.Server.Save();
            await ReplyAsync($"No Mention Message: {message ?? "N/A"}");
        }
        [Command("NoMassMention")]
        [Summary("NoMassMention")]
        [Remarks("Toggle auto-deletion of messages with 5+ role or user mentions")]
        public async Task NoMassMention()
        {
            Context.Server.Antispam.Mention.RemoveMassMention = !Context.Server.Antispam.Mention.RemoveMassMention;
            Context.Server.Save();
            await ReplyAsync($"NoMassMention: {Context.Server.Antispam.Mention.RemoveMassMention}");
        }
        [Command("NoIPs")]
        [Summary("NoIps")]
        [Remarks("Toggle auto-deletion of messages containing valid IP addresses")]
        public async Task NoIPs()
        {
            Context.Server.Antispam.Privacy.RemoveIPs = !Context.Server.Antispam.Privacy.RemoveIPs;
            Context.Server.Save();
            await ReplyAsync($"No IP Addresses: {Context.Server.Antispam.Privacy.RemoveIPs}");
        }
        [Command("NoToxicity")]
        [Summary("NoToxicity <threshhold>")]
        [Remarks("Toggle auto-deletion of messages that are too toxic")]
        public async Task NoToxicity(int threshhold = 999)
        {
            if (threshhold == 999 || threshhold < 50 || threshhold > 99)
            {
                await ReplyAsync("Pick a threshhold between 50 and 99");
                return;
            }
            Context.Server.Antispam.Toxicity.ToxicityThreshHold = threshhold;
            Context.Server.Antispam.Toxicity.UsePerspective = !Context.Server.Antispam.Toxicity.UsePerspective;
            Context.Server.Save();
            await ReplyAsync($"Remove Toxic Messages: {Context.Server.Antispam.Toxicity.UsePerspective}\n" +
                             $"Threshhold: {threshhold}");
        }
    }
}
