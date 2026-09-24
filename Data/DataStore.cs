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

        // Admin Seed
        public static void Seed()
        {
            Admin admin = new Admin
            {
                Id = GetNextUserId(),
                Name = "System Admin",
                Email = "admin@helpdesk.com",
                PasswordHash = PasswordHelper.Hash("Admin123!"),
                MustChangePassword = false
            };

            Users.Add(admin);
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
