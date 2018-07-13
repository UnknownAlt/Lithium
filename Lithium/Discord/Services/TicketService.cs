namespace Lithium.Discord.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using global::Discord;
    using global::Discord.WebSocket;

    using Lithium.Discord.Extensions;
    using Lithium.Handlers;

    public static class TicketService
    {
        public static Guild GetTickets(this IGuild guild)
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                return session.Load<Guild>($"{guild.Id}-Tickets") ?? new Guild
                {
                    ID = guild.Id
                };
            }
        }

        public class Guild
        {
            public void Save()
            {
                using (var session = DatabaseHandler.Store.OpenSession())
                {
                    session.Store(this, $"{ID}-Tickets");
                    session.SaveChanges();
                }
            }

            public void AddTicket(string message, IGuildUser creator, IGuild guild)
            {
                var ticket = new Ticket()
                {
                    Id = TicketCounter,
                    Info = new Ticket.TicketInfo
                    {
                        Creator = new Ticket.TicketUser
                        {
                            Id = creator.Id,
                            Name = creator.ToString()
                        },
                        Message = message,
                    }
                };
                var channel = (guild as SocketGuild).GetTextChannel(ChannelId);
                if (channel == null)
                {
                    throw new Exception("Ticket channel unable to be tracked");
                }

                var msg = channel.SendMessageAsync("", false, GenerateTicketEmbed(ticket).Build());
                ticket.LiveMessageId = msg.Result.Id;
                Tickets.Add(TicketCounter, ticket);

                Save();
            }

            public Ticket.TicketInfo.TicketComment GetComment(int ticketId, int commentId)
            {
                var ticket = GetTicket(ticketId);
                if (ticket.Info.Comments.TryGetValue(commentId, out Ticket.TicketInfo.TicketComment match))
                {
                    return match;
                }

                throw new Exception("Invalid Comment ID");
            }

            public void AddComment(int ticketId, IGuildUser user, string message)
            {
                var ticket = GetTicket(ticketId);
                ticket.Info.Comments.Add(ticket.Info.CommentCounter, new Ticket.TicketInfo.TicketComment
                {
                    Creator = new Ticket.TicketUser
                    {
                        Id = user.Id,
                        Name = user.ToString()
                    },
                    Id = ticket.Info.CommentCounter,
                    Message = message,
                    Votes = new Ticket.TicketVote()
                });
            }

            public void SendUpVote(int ticketId, IGuildUser voter)
            {
                var ticket = GetTicket(ticketId);
                if (ticket.Info.Votes.UpVotes.Contains(voter.Id))
                {
                    ticket.Info.Votes.UpVotes.Remove(voter.Id);
                }
                else
                {
                    if (ticket.Info.Votes.DownVotes.Contains(voter.Id))
                    {
                        ticket.Info.Votes.DownVotes.Remove(voter.Id);
                    }

                    ticket.Info.Votes.UpVotes.Add(voter.Id);
                }

                (voter.Guild.CastToSocketGuild().GetTextChannel(ChannelId).GetMessageAsync(ticket.LiveMessageId).Result as SocketUserMessage).ModifyAsync(m => m.Embed = GenerateTicketEmbed(ticket).Build());
            }

            public void SendDownVote(int ticketId, IGuildUser voter)
            {
                var ticket = GetTicket(ticketId);
                if (ticket.Info.Votes.DownVotes.Contains(voter.Id))
                {
                    ticket.Info.Votes.DownVotes.Remove(voter.Id);
                }
                else
                {
                    if (ticket.Info.Votes.UpVotes.Contains(voter.Id))
                    {
                        ticket.Info.Votes.UpVotes.Remove(voter.Id);
                    }

                    ticket.Info.Votes.DownVotes.Add(voter.Id);
                }

                (voter.Guild.CastToSocketGuild().GetTextChannel(ChannelId).GetMessageAsync(ticket.LiveMessageId).Result as SocketUserMessage).ModifyAsync(m => m.Embed = GenerateTicketEmbed(ticket).Build());
            }

            public void SolveAction(int ticketId, bool isSolved, string solveReason, IGuildUser closer)
            {
                var ticket = GetTicket(ticketId);
                ticket.Info.Solved = new Ticket.TicketInfo.TicketSolve
                {
                    Solved = isSolved,
                    SolveReason = solveReason,
                    Solver = new Ticket.TicketUser
                    {
                        Id = closer.Id,
                        Name = closer.ToString()
                    }
                };

                (closer.Guild.CastToSocketGuild().GetTextChannel(ChannelId).GetMessageAsync(ticket.LiveMessageId)
                     .Result as SocketUserMessage).ModifyAsync(
                    m => m.Embed = GenerateTicketEmbed(ticket).AddField(
                             $"Solved: {isSolved}",
                             $"**Reason:** {solveReason}\n" +
                             $"**Performed by:** {closer.ToString()}").Build());
            }


            public EmbedBuilder GenerateTicketEmbed(Ticket ticket)
            {
                var embed = new EmbedBuilder
                {
                    Fields = new List<EmbedFieldBuilder>
                                                     {
                                                         new EmbedFieldBuilder
                                                             {
                                                                 Name = $"Ticket #{ticket.Id}",
                                                                 Value = $"{ticket.Info.Message ?? "N/A"}"
                                                             },
                                                         new EmbedFieldBuilder
                                                             {
                                                                 Name = "Creator",
                                                                 Value = $"{ticket.Info.Creator.Name}"
                                                             },
                                                         new EmbedFieldBuilder
                                                             {
                                                                 Name = "Votes",
                                                                 Value = $":arrow_up_small:{ticket.Info.Votes.UpVotes.Count} :arrow_down_small:{ticket.Info.Votes.DownVotes.Count}"
                                                             }
                                                     },
                    Timestamp = DateTimeOffset.UtcNow,
                    Color = Color.DarkPurple
                };

                return embed;
            }

            public int TicketCounter => Tickets.Count;

            public ulong ID { get; set; }

            public ulong ChannelId { get; set; }

            public Ticket GetTicket(int id)
            {
                if (Tickets.TryGetValue(id, out Ticket match))
                {
                    return match;
                }

                throw new Exception("Invalid Ticket ID");
            }

            public Dictionary<int, Ticket> Tickets { get; set; } = new Dictionary<int, Ticket>();

            public class Ticket
            {
                public int Id { get; set; }

                public ulong LiveMessageId { get; set; }

                public TicketInfo Info { get; set; } = new TicketInfo();

                public class TicketInfo
                {
                    public TicketUser Creator { get; set; }

                    public string Message { get; set; }

                    public TicketVote Votes { get; set; } = new TicketVote();

                    public int CommentCounter => Comments.Count;

                    public Dictionary<int, TicketComment> Comments { get; set; } = new Dictionary<int, TicketComment>();

                    public TicketSolve Solved { get; set; } = new TicketSolve();

                    public class TicketSolve
                    {
                        public TicketUser Solver { get; set; }

                        public bool Solved { get; set; } = false;

                        public string SolveReason { get; set; }
                    }

                    public class TicketComment
                    {
                        public int Id { get; set; }

                        public string Message { get; set; }

                        public TicketUser Creator { get; set; }

                        public TicketVote Votes { get; set; } = new TicketVote();
                    }
                }

                public class TicketUser
                {
                    public ulong Id { get; set; }

                    public string Name { get; set; }
                }

                public class TicketVote
                {
                    public HashSet<ulong> UpVotes { get; set; } = new HashSet<ulong>();

                    public HashSet<ulong> DownVotes { get; set; } = new HashSet<ulong>();
                }
            }
        }
    }
}
