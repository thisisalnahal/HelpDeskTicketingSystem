using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using System;
using System.Collections.Generic;

namespace HelpDeskTicketingSystem.Services
{
    internal static class TicketStatusService
    {
        public static bool TryTransition(Ticket ticket, TicketStatus newStatus, User actor,
        out string error, bool isSystemAction = false)
        {
            error = "";

            if (ticket == null)
            {
                error = "Ticket not found.";
                return false;
            }

            switch (ticket.Status)
            {
                case TicketStatus.Open:
                    if (newStatus != TicketStatus.Assigned)
                    {
                        error = "Open tickets can only be assigned.";
                        return false;
                    }

                    if (!isSystemAction && actor.Role != Role.Dispatcher)
                    {
                        error = "Only a Dispatcher can assign a ticket.";
                        return false;
                    }

                    break;

                case TicketStatus.Assigned:
                    if (newStatus != TicketStatus.InProgress)
                    {
                        error = "Assigned tickets can only move to InProgress.";
                        return false;
                    }

                    if (!isSystemAction && ticket.AssignedUserId != actor.Id)
                    {
                        error = "Only the assigned owner can start progress.";
                        return false;
                    }

                    break;

                case TicketStatus.InProgress:
                    if (newStatus != TicketStatus.Resolved)
                    {
                        error = "InProgress tickets can only be resolved.";
                        return false;
                    }

                    if (!isSystemAction && ticket.AssignedUserId != actor.Id)
                    {
                        error = "Only the assigned owner can resolve the ticket.";
                        return false;
                    }

                    break;

                case TicketStatus.Resolved:
                    if (newStatus != TicketStatus.Closed)
                    {
                        error = "Resolved tickets can only be closed.";
                        return false;
                    }

                    if (!isSystemAction && (actor.Role != Role.Client || ticket.ClientId != actor.Id))
                    {
                        error = "Only the client who submitted this ticket can close it.";
                        return false;
                    }

                    break;

                case TicketStatus.Closed:
                    error = "Closed tickets cannot be changed.";
                    return false;
            }

            ticket.Status = newStatus;

            if (newStatus == TicketStatus.Resolved)
                ticket.ResolvedAt = DateTime.Now;

            return true;
        }

        public static void RunAutoCloseCheck()
        {
            foreach (var ticket in DataStore.Tickets)
            {
                if (ticket.Status == TicketStatus.Resolved
                    && ticket.ResolvedAt.HasValue
                    && DateTime.Now >= ticket.ResolvedAt.Value.AddDays(3))
                {
                    // Reuses the same transition logic so the rules are never duplicated.
                    TryTransition(ticket, TicketStatus.Closed, actor: null, out _, isSystemAction: true);
                }
            }
        }
    }
}
