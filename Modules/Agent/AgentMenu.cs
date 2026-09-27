using HelpDeskTicketingSystem.UI;
using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDeskTicketingSystem.Modules.Agent
{
    // Main menu always stays exactly 3 options (per leader's note). Ticket
    // actions (start progress / comment / resolve) only show up after the
    // agent picks one specific ticket from the "assigned" list, and never
    // show up at all for closed tickets.
    internal class AgentMenu
    {
        public static void Show(Models.Agent agent)
        {
            bool exit = false;

            while (!exit)
            {
                ConsoleUI.ClearScreen();

                string[] options = new string[]
                {
                    "View my assigned tickets",
                    "View my closed tickets",
                    "Logout"
                };

                int choice = ConsoleUI.PrintMenu("Agent Menu", options);

                switch (choice)
                {
                    case 1: AssignedTicketsFlow(agent); break;
                    case 2: ClosedTicketsFlow(agent); break;
                    case 3: exit = true; break;
                }
            }
        }

        // ---------- Assigned (active) tickets ----------

        private static void AssignedTicketsFlow(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            var all = AgentService.GetAssignedTickets(agent.Id);
            var active = new List<Ticket>();

            foreach (var t in all)
            {
                if (t.Status != TicketStatus.Closed)
                    active.Add(t);
            }

            if (active.Count == 0)
            {
                ConsoleUI.PrintWarning("You have no active assigned tickets.");
                ConsoleUI.Pause();
                return;
            }

            PrintTicketsTable(active);

            int id = ReadTicketIdOrZero();
            if (id == 0)
                return;

            TicketDetailsAndActionsLoop(agent, id);
        }

        // Shows the ticket, then an actions submenu that only offers moves
        // that make sense for the ticket's current status. Loops so the
        // agent can do several things to the same ticket without going
        // back to the main menu each time.
        private static void TicketDetailsAndActionsLoop(Models.Agent agent, int ticketId)
        {
            bool back = false;

            while (!back)
            {
                ConsoleUI.ClearScreen();

                string error;
                Ticket ticket = AgentService.GetTicketDetails(ticketId, agent.Id, out error);

                if (ticket == null)
                {
                    ConsoleUI.PrintError(error);
                    ConsoleUI.Pause();
                    return;
                }

                PrintTicketDetails(ticket);

                if (ticket.Status == TicketStatus.Closed)
                {
                    ConsoleUI.PrintInfo("This ticket is closed. No actions available.");
                    ConsoleUI.Pause();
                    return;
                }

                var actionLabels = new List<string>();
                var actionKeys = new List<string>();

                if (ticket.Status == TicketStatus.Assigned)
                {
                    actionLabels.Add("Start progress");
                    actionKeys.Add("start");
                }

                if (ticket.Status == TicketStatus.InProgress)
                {
                    actionLabels.Add("Add a comment");
                    actionKeys.Add("comment");
                    actionLabels.Add("Resolve ticket");
                    actionKeys.Add("resolve");
                }

                if (ticket.Status == TicketStatus.Resolved)
                {
                    actionLabels.Add("Add a comment");
                    actionKeys.Add("comment");
                }

                actionLabels.Add("Back to menu");
                actionKeys.Add("back");

                int choice = ConsoleUI.PrintMenu("Ticket #" + ticket.Id + " - Actions", actionLabels.ToArray());
                string key = actionKeys[choice - 1];

                if (key == "start")
                {
                    bool success = AgentService.StartProgress(ticketId, agent, out error);
                    if (success) ConsoleUI.PrintSuccess("Ticket moved to In Progress.");
                    else ConsoleUI.PrintError(error);
                    ConsoleUI.Pause();
                }
                else if (key == "comment")
                {
                    string text = ConsoleUI.ReadName("Comment: ");
                    bool success = AgentService.AddComment(ticketId, agent, text, out error);
                    if (success) ConsoleUI.PrintSuccess("Comment added.");
                    else ConsoleUI.PrintError(error);
                    ConsoleUI.Pause();
                }
                else if (key == "resolve")
                {
                    bool success = AgentService.ResolveTicket(ticketId, agent, out error);
                    if (success) ConsoleUI.PrintSuccess("Ticket marked as Resolved.");
                    else ConsoleUI.PrintError(error);
                    ConsoleUI.Pause();
                }
                else
                {
                    back = true;
                }
            }
        }

        // ---------- Closed tickets (view-only) ----------

        private static void ClosedTicketsFlow(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            var all = AgentService.GetAssignedTickets(agent.Id);
            var closed = new List<Ticket>();

            foreach (var t in all)
            {
                if (t.Status == TicketStatus.Closed)
                    closed.Add(t);
            }

            if (closed.Count == 0)
            {
                ConsoleUI.PrintWarning("You have no closed tickets.");
                ConsoleUI.Pause();
                return;
            }

            PrintTicketsTable(closed);

            int id = ReadTicketIdOrZero();
            if (id == 0)
                return;

            ConsoleUI.ClearScreen();
            string error;
            Ticket ticket = AgentService.GetTicketDetails(id, agent.Id, out error);

            if (ticket == null)
            {
                ConsoleUI.PrintError(error);
                ConsoleUI.Pause();
                return;
            }

            PrintTicketDetails(ticket);
            ConsoleUI.PrintInfo("This ticket is closed. No actions available.");
            ConsoleUI.Pause();
        }

        // ---------- Shared display helpers ----------

        private static void PrintTicketsTable(List<Ticket> tickets)
        {
            string[] headers = new string[] { "ID", "Title", "Status", "Priority", "Due Date", "Overdue" };
            var rows = new List<string[]>();

            foreach (var ticket in tickets)
            {
                string overdueText = ticket.IsOverdue() ? "YES" : "no";
                string dueText = ticket.DueDate.HasValue ? ticket.DueDate.Value.ToString("g") : "-";
                string priorityText = ticket.Priority.HasValue ? ticket.Priority.Value.ToString() : "-";

                rows.Add(new string[]
                {
                    ticket.Id.ToString(),
                    ticket.Title,
                    ticket.Status.ToString(),
                    priorityText,
                    dueText,
                    overdueText
                });
            }

            ConsoleUI.PrintTable(headers, rows);
        }

        private static void PrintTicketDetails(Ticket ticket)
        {
            ConsoleUI.PrintHeader("Ticket #" + ticket.Id);
            ConsoleUI.PrintInfo("Title: " + ticket.Title);
            ConsoleUI.PrintInfo("Description: " + ticket.Description);
            ConsoleUI.PrintInfo("Status: " + ticket.Status);

            if (ticket.IsOverdue())
                ConsoleUI.PrintWarning("This ticket is OVERDUE.");

            ConsoleUI.PrintDivider();
            ConsoleUI.PrintInfo("Comments:");

            if (ticket.Comments.Count == 0)
            {
                ConsoleUI.PrintInfo("  (no comments yet)");
            }
            else
            {
                foreach (var comment in ticket.Comments)
                {
                    User author = DataStore.GetUserById(comment.AuthorId);
                    string roleLabel = author != null ? author.Role.ToString() : "Unknown";
                    ConsoleUI.PrintLine("  [" + roleLabel + "] " + comment.Text);
                }
            }

            ConsoleUI.PrintDivider();
        }

        // 0 means "go back" instead of picking a ticket.
        private static int ReadTicketIdOrZero()
        {
            while (true)
            {
                ConsoleUI.Print("Ticket ID (0 to go back): ");
                string input = Console.ReadLine();

                int id;
                bool ok = int.TryParse(input, out id);

                if (ok && id >= 0)
                    return id;

                ConsoleUI.PrintError("Please enter a valid number.");
            }
        }
    }
}