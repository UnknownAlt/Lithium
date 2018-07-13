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
        /// <summary>
        /// Loads the ticket collection of the specified guild
        /// </summary>
        /// <param name="guild">
        /// The guild.
        /// </param>
        /// <returns>
        /// The <see cref="TicketModel"/>.
        /// </returns>
        public static TicketModel GetTickets(this IGuild guild)
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                return session.Load<TicketModel>($"{guild.Id}-Tickets") ?? new TicketModel
                {
                    ID = guild.Id
                };
            }
        }

        public class TicketModel
        {
            /// <summary>
            /// Saves the ticket model
            /// </summary>
            public void Save()
            {
                using (var session = DatabaseHandler.Store.OpenSession())
                {
                    session.Store(this, $"{ID}-Tickets");
                    session.SaveChanges();
                }
            }

            /// <summary>
            /// Creates a new ticket
            /// </summary>
            /// <param name="message">
            /// The message.
            /// </param>
            /// <param name="creator">
            /// The creator.
            /// </param>
            /// <param name="guild">
            /// The guild.
            /// </param>
            /// <exception cref="Exception">
            /// If the ticket channel is unavailable
            /// </exception>
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

            /// <summary>
            /// Gets a ticket comment based on the ticket ID and comment ID
            /// </summary>
            /// <param name="ticketId">
            /// The ticket id.
            /// </param>
            /// <param name="commentId">
            /// The comment id.
            /// </param>
            /// <returns>
            /// The <see cref="Ticket.TicketInfo.TicketComment"/>.
            /// </returns>
            /// <exception cref="Exception">
            /// If the ticket comment is unavailable
            /// </exception>
            public Ticket.TicketInfo.TicketComment GetComment(int ticketId, int commentId)
            {
                var ticket = GetTicket(ticketId);
                if (ticket.Info.Comments.TryGetValue(commentId, out Ticket.TicketInfo.TicketComment match))
                {
                    return match;
                }

                throw new Exception("Invalid Comment ID");
            }

            /// <summary>
            /// Adds a comment to a ticket
            /// </summary>
            /// <param name="ticketId">
            /// The ticket id.
            /// </param>
            /// <param name="user">
            /// The user.
            /// </param>
            /// <param name="message">
            /// The message.
            /// </param>
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

            /// <summary>
            /// UpVotes a ticket
            /// </summary>
            /// <param name="ticketId">
            /// The ticket id.
            /// </param>
            /// <param name="voter">
            /// The voter.
            /// </param>
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

            /// <summary>
            /// DownVotes a ticket
            /// </summary>
            /// <param name="ticketId">
            /// The ticket id.
            /// </param>
            /// <param name="voter">
            /// The voter.
            /// </param>
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

            /// <summary>
            /// Sets a ticket's solved status
            /// </summary>
            /// <param name="ticketId">
            /// The ticket id.
            /// </param>
            /// <param name="isSolved">
            /// The is solved.
            /// </param>
            /// <param name="solveReason">
            /// The solve reason.
            /// </param>
            /// <param name="closer">
            /// The closer.
            /// </param>
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

            /// <summary>
            /// Generates a discord embed based off a ticket
            /// </summary>
            /// <param name="ticket">
            /// The ticket.
            /// </param>
            /// <returns>
            /// The <see cref="EmbedBuilder"/>.
            /// </returns>
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

            /// <summary>
            /// The ticket counter.
            /// </summary>
            public int TicketCounter => Tickets.Count;

            /// <summary>
            /// Gets or sets the id of the guild
            /// </summary>
            public ulong ID { get; set; }

            /// <summary>
            /// Gets or sets the id of the ticket channel
            /// </summary>
            public ulong ChannelId { get; set; }

            /// <summary>
            /// Gets a ticket based of the ticket's ID
            /// </summary>
            /// <param name="id">
            /// The id.
            /// </param>
            /// <returns>
            /// The <see cref="Ticket"/>.
            /// </returns>
            /// <exception cref="Exception">
            /// Invalid ticket ID
            /// </exception>
            public Ticket GetTicket(int id)
            {
                if (Tickets.TryGetValue(id, out Ticket match))
                {
                    return match;
                }

                throw new Exception("Invalid Ticket ID");
            }

            /// <summary>
            /// Gets or sets the tickets [TicketID, Ticket]
            /// </summary>
            public Dictionary<int, Ticket> Tickets { get; set; } = new Dictionary<int, Ticket>();

            public class Ticket
            {
                /// <summary>
                /// Gets or sets the ticket's ID
                /// </summary>
                public int Id { get; set; }

                /// <summary>
                /// Gets or sets the live message id.
                /// This is the ulong value of the ticket announcement
                /// </summary>
                public ulong LiveMessageId { get; set; }

                /// <summary>
                /// Gets or sets the info.
                /// </summary>
                public TicketInfo Info { get; set; } = new TicketInfo();

                public class TicketInfo
                {
                    /// <summary>
                    /// Gets or sets the ticket creator.
                    /// </summary>
                    public TicketUser Creator { get; set; }

                    /// <summary>
                    /// Gets or sets the message.
                    /// </summary>
                    public string Message { get; set; }

                    /// <summary>
                    /// Gets or sets the votes.
                    /// </summary>
                    public TicketVote Votes { get; set; } = new TicketVote();

                    /// <summary>
                    /// The comment counter.
                    /// </summary>
                    public int CommentCounter => Comments.Count;

                    /// <summary>
                    /// Gets or sets the comments. [CommentID, Comment]
                    /// </summary>
                    public Dictionary<int, TicketComment> Comments { get; set; } = new Dictionary<int, TicketComment>();

                    /// <summary>
                    /// Gets or sets the solved status
                    /// </summary>
                    public TicketSolve Solved { get; set; } = new TicketSolve();

                    /// <summary>
                    /// The solved status of the ticket
                    /// </summary>
                    public class TicketSolve
                    {
                        /// <summary>
                        /// Gets or sets the solver of the ticket
                        /// </summary>
                        public TicketUser Solver { get; set; }

                        /// <summary>
                        /// Gets or sets a value indicating whether the ticket is solved.
                        /// </summary>
                        public bool Solved { get; set; }

                        /// <summary>
                        /// Gets or sets the solve reason.
                        /// </summary>
                        public string SolveReason { get; set; }
                    }

                    /// <summary>
                    /// The ticket comment.
                    /// </summary>
                    public class TicketComment
                    {
                        /// <summary>
                        /// Gets or sets the comment id.
                        /// </summary>
                        public int Id { get; set; }

                        /// <summary>
                        /// Gets or sets the comment.
                        /// </summary>
                        public string Message { get; set; }

                        /// <summary>
                        /// Gets or sets the comment creator.
                        /// </summary>
                        public TicketUser Creator { get; set; }

                        /// <summary>
                        /// Gets or sets the comment votes.
                        /// </summary>
                        public TicketVote Votes { get; set; } = new TicketVote();
                    }
                }

                /// <summary>
                /// A ticket user.
                /// </summary>
                public class TicketUser
                {
                    /// <summary>
                    /// Gets or sets the user's discord ID
                    /// </summary>
                    public ulong Id { get; set; }

                    /// <summary>
                    /// Gets or sets the name [NAME#DISCRIM]
                    /// </summary>
                    public string Name { get; set; }
                }

                /// <summary>
                /// A ticket vote set
                /// </summary>
                public class TicketVote
                {
                    /// <summary>
                    /// Gets or sets the up votes.
                    /// </summary>
                    public HashSet<ulong> UpVotes { get; set; } = new HashSet<ulong>();

                    /// <summary>
                    /// Gets or sets the down votes.
                    /// </summary>
                    public HashSet<ulong> DownVotes { get; set; } = new HashSet<ulong>();
                }
            }
        }
    }
}
