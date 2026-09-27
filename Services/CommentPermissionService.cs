using System;
using System.Collections.Generic;
using System.Text;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;

namespace HelpDeskTicketingSystem.Services
{
    internal class CommentPermissionService
    {
        public static bool CanComment(Ticket ticket, User user)
        {
            if (ticket == null || user == null)
                return false;

            if (ticket.Status == TicketStatus.Closed)
                return false;

            if (ticket.ClientId == user.Id)
                return true;

            return false;
        }
    }
}