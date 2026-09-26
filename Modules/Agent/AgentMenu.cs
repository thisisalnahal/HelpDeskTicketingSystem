using System;
using System.Collections.Generic;
using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.UI;

namespace HelpDeskTicketingSystem.Modules.Agent
{
    internal class AgentMenu
    {
        // Static entry point, matching the stub Module 1 already left here:
        // public static void Show(Models.Agent agent)
        public static void Show(Models.Agent agent)
        {
            bool exit = false;

            while (!exit)
            {
                string[] options = new string[]
                {
                    "View my assigned tickets",
                    "View ticket details",
                    "Start progress on a ticket",
                    "Add a comment",
                    "Resolve a ticket",
                    "Logout"
                };

                int choice = ConsoleUI.PrintMenu("Agent Menu", options);

                switch (choice)
                {
                    case 1: ViewAssignedTickets(agent); break;
                    case 2: ViewTicketDetails(agent); break;
                    case 3: StartProgress(agent); break;
                    case 4: AddComment(agent); break;
                    case 5: ResolveTicket(agent); break;
                    case 6: exit = true; break;
                }
            }
        }

        private static void ViewAssignedTickets(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            var tickets = AgentService.GetAssignedTickets(agent.Id);

            if (tickets.Count == 0)
            {
                ConsoleUI.PrintWarning("You have no assigned tickets.");
                ConsoleUI.Pause();
                return;
            }

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
            ConsoleUI.Pause();
        }

        private static void ViewTicketDetails(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            int id = ReadTicketId();

            string error;
            Ticket ticket = AgentService.GetTicketDetails(id, agent.Id, out error);

            if (ticket == null)
            {
                ConsoleUI.PrintError(error);
                ConsoleUI.Pause();
                return;
            }

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

            ConsoleUI.Pause();
        }

        private static void StartProgress(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            int id = ReadTicketId();

            string error;
            bool success = AgentService.StartProgress(id, agent, out error);

            if (success)
                ConsoleUI.PrintSuccess("Ticket moved to In Progress.");
            else
                ConsoleUI.PrintError(error);

            ConsoleUI.Pause();
        }

        private static void AddComment(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            int id = ReadTicketId();

            // Reusing ReadName here since it already loops until non-empty input.
            string text = ConsoleUI.ReadName("Comment: ");

            string error;
            bool success = AgentService.AddComment(id, agent, text, out error);

            if (success)
                ConsoleUI.PrintSuccess("Comment added.");
            else
                ConsoleUI.PrintError(error);

            ConsoleUI.Pause();
        }

        private static void ResolveTicket(Models.Agent agent)
        {
            ConsoleUI.ClearScreen();
            int id = ReadTicketId();

            string error;
            bool success = AgentService.ResolveTicket(id, agent, out error);

            if (success)
                ConsoleUI.PrintSuccess("Ticket marked as Resolved.");
            else
                ConsoleUI.PrintError(error);

            ConsoleUI.Pause();
        }

        // ConsoleUI has no ReadInt helper, so this loops using Print + PrintError,
        // same pattern as ConsoleUI's own Read* methods.
        private static int ReadTicketId()
        {
            while (true)
            {
                ConsoleUI.Print("Ticket ID: ");
                string input = Console.ReadLine();

                int id;
                bool ok = int.TryParse(input, out id);

                if (ok)
                    return id;

                ConsoleUI.PrintError("Please enter a valid number.");
            }
        }
    }
}