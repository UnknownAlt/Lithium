namespace RavenBOT.Modules
{
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Addons.Interactive;
    using Discord.Commands;

    using RavenBOT.Core.Bot.Context;
    using RavenBOT.Extensions;
    using RavenBOT.Models;
    using RavenBOT.Preconditions;

    [CustomPermissions(DefaultPermissionLevel.AllUsers)]
    public class Tickets : Base
    {
        private readonly TicketService ticketService;

        public Tickets(TicketService ticketService)
        {
            this.ticketService = ticketService;
        }
       
        [CustomPermissions(DefaultPermissionLevel.Administrators)]
        [Command("SetChannel")]
        [Summary("Set the ticket channel")]
        public Task SetChannelAsync()
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            tickets.ChannelId = Context.Channel.Id;
            tickets.Save();
            return SimpleEmbedAsync("Channel Set");
        }

        [CustomPermissions(DefaultPermissionLevel.Moderators)]
        [Command("RefreshTickets")]
        [Summary("Refreshes the 10 most recent tickets in the ticket channel")]
        public Task RefreshTicketsAsync()
        {
            var tickets = ticketService.GetTickets(Context.Guild);
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
        [Alias("create", "ticket")]
        [Summary("Create a new ticket")]
        public Task AddTicketAsync([Remainder]string message)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            tickets.AddTicket(message, Context.User.CastToSocketGuildUser(), Context.Guild);
            return SimpleEmbedAsync("Success, added ticket");
        }

        [Command("AddComment")]
        [Alias("comment")]
        [Summary("Comment on a ticket")]
        public async Task AddCommentAsync(int ticketId, [Remainder]string message)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            await tickets.AddCommentAsync(ticketId, Context.User.CastToSocketGuildUser(), message);
            tickets.Save();
            await SimpleEmbedAsync("Success, added comment");
        }

        [Command("ViewTicket")]
        [Summary("View a ticket")]
        public Task ViewTicketAsync(int ticketId)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            var ticket = tickets.GetTicket(ticketId);
            var pager = tickets.GenerateTicketEmbedWithComments(ticket);
            return PagedReplyAsync(pager, new ReactionList { First = true, Last = true, Forward = true, Backward = true }, false);
        }

        [Command("UpVoteTicket")]
        [Alias("UpVote", "up", "vote up")]
        [Summary("UpVote a ticket")]
        public async Task UpVoteAsync(int ticketId)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            await tickets.SendUpVoteAsync(ticketId, Context.User.CastToSocketGuildUser());
            tickets.Save();
            await SimpleEmbedAsync("Success. UpVoted");
        }

        [Command("DownVoteTicket")]
        [Alias("DownVote", "Down", "vote down")]
        [Summary("DownVote a ticket")]
        public async Task DownVoteAsync(int ticketId)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            await tickets.SendDownVoteAsync(ticketId, Context.User.CastToSocketGuildUser());
            tickets.Save();
            await SimpleEmbedAsync("Success. DownVoted");
        }

        [CustomPermissions(DefaultPermissionLevel.Administrators)]
        [Command("SolveTicket")]
        [Alias("Close", "Solve")]
        [Summary("Set a ticket's status as solved")]
        public async Task SolveAsync(int ticketId, [Remainder]string reason = null)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            await tickets.SolveActionAsync(ticketId, true, reason, Context.User.CastToSocketGuildUser());
            tickets.Save();
            await SimpleEmbedAsync($"Success. Ticket #{ticketId} Set as solved");
        }

        [CustomPermissions(DefaultPermissionLevel.Administrators)]
        [Command("UnSolveTicket")]
        [Alias("UnSolve", "ReOpen", "Re-Open", "Open", "OpenTicket")]
        [Summary("Re-Open a ticket")]
        public async Task OpenAsync(int ticketId, [Remainder]string reason = null)
        {
            var tickets = ticketService.GetTickets(Context.Guild);
            await tickets.SolveActionAsync(ticketId, false, reason, Context.User.CastToSocketGuildUser());
            tickets.Save();
            await SimpleEmbedAsync($"Ticket #{ticketId} Re-Opened");
        }
    }
}
