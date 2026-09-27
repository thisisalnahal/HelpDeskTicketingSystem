using System;
using System.Collections.Generic;
using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Services;

namespace HelpDeskTicketingSystem.Modules.Agent
{
    // Static to match the rest of the codebase (DataStore, TicketStatusService
    // are static too). This class holds ONLY logic — no Console.* calls here.
    // AgentMenu.cs is the only place allowed to talk to ConsoleUI.
    internal static class AgentService
    {
        // Section 7: Agent only ever sees tickets currently assigned to them.
        public static List<Ticket> GetAssignedTickets(int agentId)
        {
            return DataStore.GetTicketsByAssignedUser(agentId);
        }

        public static Ticket GetTicketDetails(int ticketId, int agentId, out string error)
        {
            error = "";
            Ticket ticket = DataStore.GetTicketById(ticketId);

            if (ticket == null)
            {
                error = "Ticket not found.";
                return null;
            }

            if (ticket.AssignedUserId != agentId)
            {
                error = "This ticket is not assigned to you.";
                return null;
            }

            return ticket;
        }

        // Assigned -> InProgress. TicketStatusService already checks that
        // only the assigned owner can do this, so we don't repeat that check here.
        public static bool StartProgress(int ticketId, User agent, out string error)
        {
            Ticket ticket = DataStore.GetTicketById(ticketId);

            if (ticket == null)
            {
                error = "Ticket not found.";
                return false;
            }

            return TicketStatusService.TryTransition(ticket, TicketStatus.InProgress, agent, out error);
        }

        // InProgress -> Resolved.
        public static bool ResolveTicket(int ticketId, User agent, out string error)
        {
            Ticket ticket = DataStore.GetTicketById(ticketId);

            if (ticket == null)
            {
                error = "Ticket not found.";
                return false;
            }

            return TicketStatusService.TryTransition(ticket, TicketStatus.Resolved, agent, out error);
        }

        // TEMP: Services/CommentPermissionService.cs is still empty (checked the file,
        // no methods in it yet). Once whoever owns it fills it in, replace this whole
        // check with a call to it. This matches doc section 6.2 for the Agent case only:
        // must be the assigned agent, ticket must have started progress, and never on Closed.
        public static bool AddComment(int ticketId, User agent, string text, out string error)
        {
            error = "";
            Ticket ticket = DataStore.GetTicketById(ticketId);

            if (ticket == null)
            {
                error = "Ticket not found.";
                return false;
            }

            if (ticket.AssignedUserId != agent.Id)
            {
                error = "This ticket is not assigned to you.";
                return false;
            }

            if (ticket.Status == TicketStatus.Closed)
            {
                error = "This ticket is closed. No further comments allowed.";
                return false;
            }

            bool hasStartedProgress = ticket.Status == TicketStatus.InProgress || ticket.Status == TicketStatus.Resolved;
            if (!hasStartedProgress)
            {
                error = "You can't comment yet. Start progress on the ticket first.";
                return false;
            }

            // Confirmed against the real Comment.cs: Id, TicketId, AuthorId, Text, CreatedAt.
            // There's no AuthorRole field on Comment — the author's role is looked up
            // via AuthorId + DataStore.GetUserById() whenever a comment is displayed.
            Comment comment = new Comment();
            comment.Id = DataStore.GetNextCommentId();
            comment.TicketId = ticket.Id;
            comment.AuthorId = agent.Id;
            comment.Text = text;
            comment.CreatedAt = DateTime.Now;

            ticket.Comments.Add(comment);
            return true;
        }
    }
}
