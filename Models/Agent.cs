using HelpDeskTicketingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDeskTicketingSystem.Models
{
    internal class Agent : User
    {
        public override Role Role
        {
            get { return Role.Agent; }
        }
        public Department Department { get; set; }

        // Store the datetime every time this agent
        // receives a new ticket. Used ONLY to break ties when two or more agents
        // have the same active ticket count - whoever has the OLDEST
        // LastAssignedAt wins the tie. Starts as null (never assigned anything yet).
        public DateTime? LastAssignedAt { get; set; }
    }
}
