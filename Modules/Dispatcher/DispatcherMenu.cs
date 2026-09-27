using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Services;
using HelpDeskTicketingSystem.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpDeskTicketingSystem.Modules.Dispatcher
{
    internal class DispatcherMenu
    {
        public static void Show(Models.Dispatcher dispatcher)
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                ConsoleUI.ClearScreen();
                int choice = ConsoleUI.PrintMenu($"DISPATCHER MENU - {dispatcher.Name}",
                [
                    "Review open tickets",
                    "Work on tickets I own",
                    "Logout"
                ]);

                switch (choice)
                {
                    case 1:
                        ReviewOpenTickets(dispatcher);
                        break;
                    case 2:
                        WorkOnOwnTickets(dispatcher);
                        break;
                    case 3:
                        loggedIn = false;
                        break;
                }
            }
        }

        private static void ReviewOpenTickets(Models.Dispatcher dispatcher)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("OPEN TICKETS");

            List<Ticket> openTickets = DataStore.GetOpenTickets();

            if (openTickets.Count == 0)
            {
                ConsoleUI.PrintInfo("No open tickets to review.");
                ConsoleUI.Pause();
                return;
            }

            PrintTicketTable(openTickets);

            ConsoleUI.Print("Enter ticket ID to process (0 to go back): ");
            int.TryParse(Console.ReadLine(), out int ticketId);
            if (ticketId == 0) return;

            Ticket? ticket = openTickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null)
            {
                ConsoleUI.PrintError("Ticket not found among open tickets.");
                ConsoleUI.Pause();
                return;
            }

            ProcessOpenTicket(ticket, dispatcher);
        }

        private static void ProcessOpenTicket(Ticket ticket, Models.Dispatcher dispatcher)
        {
            TicketPriority priority = ReadPriority();
            Department department = ReadDepartment();

            int action = ConsoleUI.PrintMenu("Assign this ticket",
            [
                "Auto-assign to the least busy agent",
                "Take ownership personally",
                "Cancel"
            ]);

            if (action == 3) return;

            // Priority/department/due date get set regardless of who ends up owning it.
            ticket.Priority = priority;
            ticket.Department = department;
            ticket.DueDate = DueDateCalculator.Calculate(priority, ticket.CreatedAt);

            if (action == 1)
            {
                Models.Agent? agent = AssignmentService.GetLeastBusyAgent(department);
                if (agent == null)
                {
                    ConsoleUI.PrintError(
                        $"No agents found in the {department} department. " +
                        "Take ownership personally instead, or ask an Admin to add one.");
                    ConsoleUI.Pause();
                    return;
                }

                ticket.AssignedUserId = agent.Id;
                agent.LastAssignedAt = DateTime.Now;

                if (TicketStatusService.TryTransition(ticket, TicketStatus.Assigned, dispatcher, out string error))
                    ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} assigned to {agent.Name}. Due: {ticket.DueDate:g}");
                else
                    ConsoleUI.PrintError(error);
            }
            else if (action == 2)
            {
                ticket.AssignedUserId = dispatcher.Id;

                if (TicketStatusService.TryTransition(ticket, TicketStatus.Assigned, dispatcher, out string error))
                    ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} is now yours. Due: {ticket.DueDate:g}");
                else
                    ConsoleUI.PrintError(error);
            }

            ConsoleUI.Pause();
        }

        private static void WorkOnOwnTickets(Models.Dispatcher dispatcher)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("TICKETS YOU OWN");

            List<Ticket> myTickets = DataStore.GetTicketsByAssignedUser(dispatcher.Id);

            if (myTickets.Count == 0)
            {
                ConsoleUI.PrintInfo("You haven't taken ownership of any tickets.");
                ConsoleUI.Pause();
                return;
            }

            PrintTicketTable(myTickets);

            ConsoleUI.Print("Enter ticket ID to work on (0 to go back): ");
            int.TryParse(Console.ReadLine(), out int ticketId);
            if (ticketId == 0) return;

            Ticket? ticket = myTickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null)
            {
                ConsoleUI.PrintError("Ticket not found among your tickets.");
                ConsoleUI.Pause();
                return;
            }

            WorkOnTicket(ticket, dispatcher);
        }

        private static void WorkOnTicket(Ticket ticket, Models.Dispatcher dispatcher)
        {
            switch (ticket.Status)
            {
                case TicketStatus.Assigned:
                    if (TicketStatusService.TryTransition(ticket, TicketStatus.InProgress, dispatcher, out string startError))
                        ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} is now In Progress.");
                    else
                        ConsoleUI.PrintError(startError);
                    break;

                case TicketStatus.InProgress:
                    if (TicketStatusService.TryTransition(ticket, TicketStatus.Resolved, dispatcher, out string resolveError))
                        ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} marked as Resolved.");
                    else
                        ConsoleUI.PrintError(resolveError);
                    break;

                default:
                    // Resolved: waiting on the Client to close it, or the 3-day auto-close job.
                    // Closed: nothing left to do.
                    ConsoleUI.PrintInfo($"Ticket #{ticket.Id} is currently {ticket.Status} - nothing to do here.");
                    break;
            }

            // NOTE: commenting isn't wired up here yet - CommentPermissionService
            // (Module 2) is still an empty stub in the repo. Once that's implemented,
            // add a comment option here that calls into it, the same way Module 4 will.

            ConsoleUI.Pause();
        }

        private static void PrintTicketTable(List<Ticket> tickets)
        {
            List<string[]> rows = new();
            foreach (var t in tickets)
            {
                rows.Add(new[]
                {
                    t.Id.ToString(),
                    t.Title,
                    t.Priority?.ToString() ?? "-",
                    t.Department?.ToString() ?? "-",
                    t.Status.ToString(),
                    t.IsOverdue() ? "OVERDUE" : "-"
                });
            }

            ConsoleUI.PrintTable(["ID", "Title", "Priority", "Department", "Status", "Overdue"], rows);
        }

        private static TicketPriority ReadPriority()
        {
            string[] names = Enum.GetNames(typeof(TicketPriority));
            int choice = ConsoleUI.PrintMenu("Select priority", names);
            return (TicketPriority)Enum.Parse(typeof(TicketPriority), names[choice - 1]);
        }

        private static Department ReadDepartment()
        {
            string[] names = Enum.GetNames(typeof(Department));
            int choice = ConsoleUI.PrintMenu("Select department", names);
            return (Department)Enum.Parse(typeof(Department), names[choice - 1]);
        }
    }
}