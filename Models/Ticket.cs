using HelpDeskTicketingSystem.Enums;

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;


namespace HelpDeskTicketingSystem.Models
{
    internal class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TicketStatus Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public Department? Department { get; set; }
        public int ClientId { get; set; }

        // The Agent OR Dispatcher currently responsible for this ticket.
        public int? AssignedUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Calculated the moment the Dispatcher sets Priority (doc section 5).
        // CreatedAt + {4h Urgent, 24h High, 48h Medium, 72h Low}.
        public DateTime? DueDate { get; set; }

        // Set the moment the owner marks the ticket Resolved. Used for two things:
        //  1) Overdue checks stop counting once this is set (doc section 5).
        //  2) The 3-day auto-close housekeeping job measures from this timestamp (doc section 9).
        public DateTime? ResolvedAt { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public bool IsOverdue()
        {
            if (DueDate == null) return false;
            if (Status == TicketStatus.Resolved || Status == TicketStatus.Closed) return false;
            return DateTime.Now > DueDate.Value;
        }
    }
}
