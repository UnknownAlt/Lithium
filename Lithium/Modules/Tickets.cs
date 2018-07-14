namespace Lithium.Modules
{
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord.Commands;

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Discord.Preconditions;
    using Lithium.Discord.Services;

    [CustomPermissions(DefaultPermissionLevel.AllUsers)]
    public class Tickets : Base
    {
        [CustomPermissions(DefaultPermissionLevel.Administrators)]
        [Command("SetChannel")]
        [Summary("Set the ticket channel")]
        public Task SetChannelAsync()
        {
            var tickets = Context.Guild.GetTickets();
            tickets.ChannelId = Context.Channel.Id;
            tickets.Save();
            return SimpleEmbedAsync("Channel Set");
        }

        [CustomPermissions(DefaultPermissionLevel.Moderators)]
        [Command("RefreshTickets")]
        [Summary("Refreshes the 10 most recent tickets in the ticket channel")]
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
        [Summary("Create a new ticket")]
        public Task AddTicketAsync([Remainder]string message)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.AddTicket(message, Context.User.CastToSocketGuildUser(), Context.Guild);
            return SimpleEmbedAsync("Success, added ticket");
        }

        [Command("AddComment")]
        [Summary("Comment on a ticket")]
        public Task AddCommentAsync(int ticketId, [Remainder]string message)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.AddComment(ticketId, Context.User.CastToSocketGuildUser(), message);
            tickets.Save();
            return SimpleEmbedAsync("Success, added comment");
        }

        [Command("UpVoteTicket")]
        [Summary("UpVote a ticket")]
        public Task UpVoteAsync(int ticketId)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SendUpVote(ticketId, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. UpVoted");
        }

        [Command("DownVoteTicket")]
        [Summary("DownVote a ticket")]
        public Task DownVoteAsync(int ticketId)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SendDownVote(ticketId, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. DownVoted");
        }

        [CustomPermissions(DefaultPermissionLevel.Administrators)]
        [Command("SolveTicket")]
        [Summary("Set a ticket's status as solved")]
        public Task SolveAsync(int ticketId, [Remainder]string reason)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SolveAction(ticketId, true, reason, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. Set as solved");
        }

        [CustomPermissions(DefaultPermissionLevel.Administrators)]
        [Command("UnSolveTicket")]
        [Summary("Re-Open a ticket")]
        public Task OpenAsync(int ticketId, [Remainder]string reason)
        {
            var tickets = Context.Guild.GetTickets();
            tickets.SolveAction(ticketId, false, reason, Context.User.CastToSocketGuildUser());
            tickets.Save();
            return SimpleEmbedAsync("Success. Set as Unsolved");
        }
    }
}
