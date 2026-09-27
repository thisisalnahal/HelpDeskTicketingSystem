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

            // Nobody, regardless of role, can comment once Closed.
            if (ticket.Status == TicketStatus.Closed)
                return false;

            // Client can comment on their own ticket any time before Closed.
            if (ticket.ClientId == user.Id)
                return true;

            // Agent, or Dispatcher who took ownership personally: can only
            // comment once they've explicitly started progress - being merely
            // assigned is not enough. This covers InProgress and Resolved;
            // Assigned (not yet started) is deliberately excluded.
            bool isOwner = ticket.AssignedUserId == user.Id;
            bool hasStartedProgress = ticket.Status == TicketStatus.InProgress
                                    || ticket.Status == TicketStatus.Resolved;

            if (isOwner && hasStartedProgress)
                return true;

            // Not the client, and not the owner (or owner hasn't started yet).
            return false;
        }
    }
}