using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Security;
using HelpDeskTicketingSystem.Enums;

namespace HelpDeskTicketingSystem.Data
{
    internal class DataStore
    {
        public static List<User> Users { get; set; } = new List<User>();
        public static List<Ticket> Tickets { get; set; } = new List<Ticket>();

        // Simple auto-increment counters. Call GetNextUserId() / GetNextTicketId() /
        // GetNextCommentId() instead of hand-picking numbers, so IDs never collide.
        private static int _nextUserId = 1;
        private static int _nextTicketId = 1;
        private static int _nextCommentId = 1;

        public static int GetNextUserId() => _nextUserId++;
        public static int GetNextTicketId() => _nextTicketId++;
        public static int GetNextCommentId() => _nextCommentId++;

        public static void Seed()
        {
            // ---------- Admin ----------
            Admin admin = new Admin
            {
                Id = GetNextUserId(),
                Name = "System Admin",
                Email = "admin@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Admin123!"),
                MustChangePassword = false
            };
            Users.Add(admin);

            // ---------- Dispatcher ----------
            Dispatcher dispatcher = new Dispatcher
            {
                Id = GetNextUserId(),
                Name = "Ali - Dispatcher",
                Email = "dispatcher@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Dispatcher123!"),
                MustChangePassword = false
            };
            Users.Add(dispatcher);

            // ---------- Agents ----------
            // Hardware: two agents, uneven load on purpose so you can test the
            // least-busy tiebreaker/selection logic once you assign new tickets.
            Agent hwAgent1 = new Agent
            {
                Id = GetNextUserId(),
                Name = "Alex",
                Email = "alex.hw@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Agent123!"),
                MustChangePassword = false,
                Department = Department.Hardware,
                LastAssignedAt = DateTime.Now.AddHours(-5)
            };
            Agent hwAgent2 = new Agent
            {
                Id = GetNextUserId(),
                Name = "Bilal",
                Email = "bilal.hw@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Agent123!"),
                MustChangePassword = false,
                Department = Department.Hardware,
                LastAssignedAt = DateTime.Now.AddHours(-1)
            };
            Users.Add(hwAgent1);
            Users.Add(hwAgent2);

            // Software: one agent, never assigned yet - good for testing the
            // "never assigned wins the tie" rule if you add a second later.
            Agent swAgent = new Agent
            {
                Id = GetNextUserId(),
                Name = "Ahmed",
                Email = "ahmed.sw@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Agent123!"),
                MustChangePassword = false,
                Department = Department.Software,
                LastAssignedAt = null
            };
            Users.Add(swAgent);

            // Billing: one agent, so you can test a normal single-agent assignment.
            Agent billingAgent = new Agent
            {
                Id = GetNextUserId(),
                Name = "Omar",
                Email = "omar.billing@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Agent123!"),
                MustChangePassword = false,
                Department = Department.Billing,
                LastAssignedAt = null
            };
            Users.Add(billingAgent);

            // Network: deliberately NO agents here - lets you test the doc 4.3
            // rejection ("no agents available in that department").

            // ---------- Clients ----------
            Client client1 = new Client
            {
                Id = GetNextUserId(),
                Name = "Khalid",
                Email = "khalid@client.com",
                PasswordHash = PasswordHelper.Hash("Client123!"),
                MustChangePassword = false
            };
            Client client2 = new Client
            {
                Id = GetNextUserId(),
                Name = "Muhammad",
                Email = "Muhammad@client.com",
                PasswordHash = PasswordHelper.Hash("Client123!"),
                MustChangePassword = false
            };
            Users.Add(client1);
            Users.Add(client2);

            // ---------- Tickets ----------

            // 1) Open, untouched - test Dispatcher's "review open tickets" flow.
            Tickets.Add(new Ticket
            {
                Id = GetNextTicketId(),
                Title = "Laptop won't turn on",
                Description = "Pressed the power button, nothing happens.",
                Status = TicketStatus.Open,
                ClientId = client1.Id,
                CreatedAt = DateTime.Now.AddHours(-2)
            });

            // 2) Assigned to hwAgent1, still Assigned (not started) - test Agent's
            // "Start progress" action, and test that comments are blocked until then.
            Tickets.Add(new Ticket
            {
                Id = GetNextTicketId(),
                Title = "Printer jamming constantly",
                Description = "Every print job jams on page 2.",
                Status = TicketStatus.Assigned,
                ClientId = client1.Id,
                AssignedUserId = hwAgent1.Id,
                Priority = TicketPriority.Medium,
                Department = Department.Hardware,
                CreatedAt = DateTime.Now.AddHours(-10),
                DueDate = DateTime.Now.AddHours(38) // 48h medium window, partly elapsed
            });

            // 3) InProgress, already has a comment thread - test adding more comments,
            // resolving, and viewing an existing thread with mixed authors.
            Ticket inProgressTicket = new Ticket
            {
                Id = GetNextTicketId(),
                Title = "VPN keeps disconnecting",
                Description = "Drops every 10 minutes while working from home.",
                Status = TicketStatus.InProgress,
                ClientId = client2.Id,
                AssignedUserId = swAgent.Id,
                Priority = TicketPriority.High,
                Department = Department.Software,
                CreatedAt = DateTime.Now.AddHours(-20),
                DueDate = DateTime.Now.AddHours(4) // due soon, not overdue yet
            };
            inProgressTicket.Comments.Add(new Comment
            {
                Id = GetNextCommentId(),
                TicketId = inProgressTicket.Id,
                AuthorId = client2.Id,
                Text = "Any update on this? It's been happening all week.",
                CreatedAt = DateTime.Now.AddHours(-15)
            });
            inProgressTicket.Comments.Add(new Comment
            {
                Id = GetNextCommentId(),
                TicketId = inProgressTicket.Id,
                AuthorId = swAgent.Id,
                Text = "Looking into it now, checking your router logs.",
                CreatedAt = DateTime.Now.AddHours(-14)
            });
            Tickets.Add(inProgressTicket);

            // 4) InProgress and deliberately OVERDUE - test the overdue flag/warning
            // showing up consistently across Agent/Dispatcher/Admin ticket lists.
            Tickets.Add(new Ticket
            {
                Id = GetNextTicketId(),
                Title = "Production server down",
                Description = "Main server unreachable since this morning.",
                Status = TicketStatus.InProgress,
                ClientId = client1.Id,
                AssignedUserId = billingAgent.Id, // deliberately cross-department to keep seed simple
                Priority = TicketPriority.Urgent,
                Department = Department.Billing,
                CreatedAt = DateTime.Now.AddHours(-10),
                DueDate = DateTime.Now.AddHours(-6) // 4h urgent window, well passed - OVERDUE
            });

            // 5) Resolved, waiting on client to close or auto-close - test Client's
            // "Close ticket" action, and (if you fast-forward ResolvedAt) the
            // 3-day auto-close job.
            Tickets.Add(new Ticket
            {
                Id = GetNextTicketId(),
                Title = "Password reset request",
                Description = "Locked out of my account after too many attempts.",
                Status = TicketStatus.Resolved,
                ClientId = client2.Id,
                AssignedUserId = hwAgent2.Id,
                Priority = TicketPriority.Low,
                Department = Department.Hardware,
                CreatedAt = DateTime.Now.AddHours(-30),
                DueDate = DateTime.Now.AddHours(42),
                ResolvedAt = DateTime.Now.AddHours(-1) // fresh - not yet eligible for auto-close
            });

            // 6) Already Closed - test that no further comments/actions are offered
            // anywhere it's displayed.
            Tickets.Add(new Ticket
            {
                Id = GetNextTicketId(),
                Title = "Monitor flickering",
                Description = "External monitor flickers on startup.",
                Status = TicketStatus.Closed,
                ClientId = client1.Id,
                AssignedUserId = hwAgent1.Id,
                Priority = TicketPriority.Low,
                Department = Department.Hardware,
                CreatedAt = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(-2),
                ResolvedAt = DateTime.Now.AddDays(-4)
            });

            // 7) Resolved 4 days ago - test the auto-close housekeeping job fires
            // correctly the next time RunAutoCloseCheck() runs (e.g. on next
            // return to the welcome screen).
            Tickets.Add(new Ticket
            {
                Id = GetNextTicketId(),
                Title = "Slow internet speeds",
                Description = "Wifi noticeably slower than usual.",
                Status = TicketStatus.Resolved,
                ClientId = client2.Id,
                AssignedUserId = swAgent.Id,
                Priority = TicketPriority.Medium,
                Department = Department.Software,
                CreatedAt = DateTime.Now.AddDays(-6),
                DueDate = DateTime.Now.AddDays(-4),
                ResolvedAt = DateTime.Now.AddDays(-4) // 4 days ago -> should auto-close on next check
            });
        }

        public static int GetActiveTicketCount(int userId)
        {
            int count = 0;
            foreach (var ticket in Tickets)
            {
                if (ticket.AssignedUserId == userId && ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed)
                    count++;
            }
            return count;
        }

        public static User? GetUserByEmail(string email)
        {
            foreach (var user in Users)
            {
                if (user.Email.ToLower() == email.ToLower()) return user;
            }
            return null;
        }

        public static User? GetUserById(int id)
        {
            foreach (var user in Users)
            {
                if (user.Id == id) return user;
            }
            return null;
        }

        public static Ticket? GetTicketById(int id)
        {
            foreach (var ticket in Tickets)
            {
                if (ticket.Id == id) return ticket;
            }
            return null;
        }

        public static List<Agent> GetAgentsByDepartment(Department department)
        {
            var result = new List<Agent>();
            foreach (var user in Users)
            {
                if (user is Agent agent && agent.Department == department)
                    result.Add(agent);
            }
            return result;
        }

        public static List<Ticket> GetTicketsByClient(int clientId)
        {
            var result = new List<Ticket>();
            foreach (var ticket in Tickets)
            {
                if (ticket.ClientId == clientId)
                    result.Add(ticket);
            }
            return result;
        }

        public static List<Ticket> GetTicketsByAssignedUser(int userId)
        {
            var result = new List<Ticket>();
            foreach (var ticket in Tickets)
            {
                if (ticket.AssignedUserId == userId)
                    result.Add(ticket);
            }
            return result;
        }
        public static List<Ticket> GetOpenTickets()
        {
            var result = new List<Ticket>();
            foreach (var ticket in Tickets)
            {
                if (ticket.Status == TicketStatus.Open)
                    result.Add(ticket);
            }
            return result;
        }
        
        public static bool IsUserLinkedToAnyTicket(int userId)
        {
            foreach (var ticket in Tickets)
            {
                if (ticket.ClientId == userId || ticket.AssignedUserId == userId)
                    return true;
            }
            return false;
        }

    }
}
