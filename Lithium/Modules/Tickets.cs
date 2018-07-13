using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;

namespace Lithium.Modules
{
    using System.Linq;
    using System.Threading.Tasks;

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Discord.Services;
    using Lithium.Models;

    [CustomPermissions(DefaultPermissionLevel.Moderators)]
    public class Tickets : Base
    {
        [Command("SetChannel")]
        public Task SetChannelAsync()
        {
            var tickets = Context.Guild.GetTickets();
            tickets.ChannelId = Context.Channel.Id;
            tickets.Save();
            return SimpleEmbedAsync("Channel Set");
        }

        [Command("RefreshTickets")]
        public Task RefreshTicketsAsync()
        {
            var tickets = Context.Guild.GetTickets();
            var available = tickets.Tickets.Where(t => !t.Value.Info.Solved.Solved).OrderBy(i => i.Key).Take(10);
            var ticketChannel = Context.Guild.GetTextChannel(tickets.ChannelId);
            foreach (var ticket in available)
            {
                var res = ticketChannel.SendMessageAsync("", false, tickets.GenerateTicketEmbed(ticket.Value).Build());
                ticket.Value.LiveMessageId = res.Result.Id;
            }

            tickets.Save();

            return SimpleEmbedAsync("Recent tickets have been refreshed");
        }

        [Command("AddTicket")]
        public Task AddTicketAsync([Remainder]string message)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.AddTicket(message, Context.User.CastToSocketGuildUser(), Context.Guild);
            return SimpleEmbedAsync("Success, added ticket");
        }

        [Command("AddComment")]
        public Task AddCommentAsync(int ticketId, [Remainder]string message)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.AddComment(ticketId, Context.User.CastToSocketGuildUser(), message);
            tickets.Save();
            return SimpleEmbedAsync("Success, added comment");
        }

        [Command("UpVoteTicket")]
        public Task UpVoteAsync(int ticketId)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SendUpVote(ticketId, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. UpVoted");
        }

        [Command("DownVoteTicket")]
        public Task DownVoteAsync(int ticketId)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SendDownVote(ticketId, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. DownVoted");
        }

        [Command("SolveTicket")]
        public Task SolveAsync(int ticketId, [Remainder]string reason)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SolveAction(ticketId, true, reason, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. Set as solved");
        }

        [Command("OpenTicket")]
        public Task OpenAsync(int ticketId, [Remainder]string reason)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SolveAction(ticketId, false, reason, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. Set as Unsolved");
        }
    }
}
