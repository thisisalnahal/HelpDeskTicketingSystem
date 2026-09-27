using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Services;
using HelpDeskTicketingSystem.UI;
using System;
using System.Collections.Generic;

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

            Ticket? ticket = null;
            foreach (var t in openTickets)
            {
                if (t.Id == ticketId)
                {
                    ticket = t;
                    break;
                }
            }

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
            int action = ConsoleUI.PrintMenu("Assign this ticket",
            [
                "Route to a department (auto-assign to least busy agent)",
                "Take ownership personally",
                "Cancel"
            ]);

            if (action == 3) return;

            // Priority is needed either way, so read it up front.
            TicketPriority priority = ReadPriority();

            if (action == 1)
            {
                Department department = ReadDepartment();
                Models.Agent? agent = AssignmentService.GetLeastBusyAgent(department);

                if (agent == null)
                {
                    // Doc 4.3: nothing is saved until a department with available
                    // staff is chosen. The ticket has not been touched at all yet,
                    // so simply returning here leaves it exactly as it was: Open,
                    // no priority/department/due date set.
                    ConsoleUI.PrintError(
                        $"No agents found in the {department} department. Nothing was saved - " +
                        "take ownership personally instead, or ask an Admin to add an agent.");
                    ConsoleUI.Pause();
                    return;
                }

                // Only commit to the ticket now that we know the assignment will succeed.
                ticket.Priority = priority;
                ticket.Department = department;
                ticket.DueDate = DueDateCalculator.Calculate(priority, ticket.CreatedAt);
                ticket.AssignedUserId = agent.Id;
                agent.LastAssignedAt = DateTime.Now;

                if (TicketStatusService.TryTransition(ticket, TicketStatus.Assigned, dispatcher, out string error))
                    ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} assigned to {agent.Name}. Due: {ticket.DueDate:g}");
                else
                    ConsoleUI.PrintError(error);
            }
            else if (action == 2)
            {
                // Doc 6.3: taking ownership personally sets the department to
                // "General questions" automatically - the Dispatcher does not
                // choose a department in this branch.
                ticket.Priority = priority;
                ticket.Department = Department.GeneralQuestions;
                ticket.DueDate = DueDateCalculator.Calculate(priority, ticket.CreatedAt);
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

            Ticket? ticket = null;
            foreach (var t in myTickets)
            {
                if (t.Id == ticketId)
                {
                    ticket = t;
                    break;
                }
            }

            if (ticket == null)
            {
                ConsoleUI.PrintError("Ticket not found among your tickets.");
                ConsoleUI.Pause();
                return;
            }

            WorkOnTicketLoop(ticket, dispatcher);
        }

        // Loops on one ticket so the dispatcher can start progress, comment,
        // and resolve across multiple visits without re-picking the ticket
        // each time - same pattern Module 2/4 use for their ticket screens.
        private static void WorkOnTicketLoop(Ticket ticket, Models.Dispatcher dispatcher)
        {
            bool back = false;
            while (!back)
            {
                ConsoleUI.ClearScreen();
                PrintTicketDetails(ticket);

                if (ticket.Status == TicketStatus.Closed)
                {
                    ConsoleUI.PrintInfo("This ticket is closed. No actions available.");
                    ConsoleUI.Pause();
                    return;
                }

                var labels = new List<string>();
                var keys = new List<string>();

                if (ticket.Status == TicketStatus.Assigned)
                {
                    labels.Add("Start progress");
                    keys.Add("start");
                }

                if (CommentPermissionService.CanComment(ticket, dispatcher))
                {
                    labels.Add("Add a comment");
                    keys.Add("comment");
                }

                if (ticket.Status == TicketStatus.InProgress)
                {
                    labels.Add("Resolve ticket");
                    keys.Add("resolve");
                }

                if (ticket.Status == TicketStatus.Resolved)
                {
                    labels.Add("Waiting on client to close (or 3-day auto-close)");
                    keys.Add("noop");
                }

                labels.Add("Back to menu");
                keys.Add("back");

                int choice = ConsoleUI.PrintMenu($"Ticket #{ticket.Id} - Actions", labels.ToArray());
                string key = keys[choice - 1];

                if (key == "start")
                {
                    if (TicketStatusService.TryTransition(ticket, TicketStatus.InProgress, dispatcher, out string error))
                        ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} is now In Progress.");
                    else
                        ConsoleUI.PrintError(error);
                    ConsoleUI.Pause();
                }
                else if (key == "comment")
                {
                    string text = ConsoleUI.ReadName("Comment: ");
                    Comment comment = new Comment
                    {
                        Id = DataStore.GetNextCommentId(),
                        TicketId = ticket.Id,
                        AuthorId = dispatcher.Id,
                        Text = text,
                        CreatedAt = DateTime.Now
                    };
                    ticket.Comments.Add(comment);
                    ConsoleUI.PrintSuccess("Comment added.");
                    ConsoleUI.Pause();
                }
                else if (key == "resolve")
                {
                    if (TicketStatusService.TryTransition(ticket, TicketStatus.Resolved, dispatcher, out string error))
                        ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} marked as Resolved.");
                    else
                        ConsoleUI.PrintError(error);
                    ConsoleUI.Pause();
                }
                else if (key == "noop")
                {
                    ConsoleUI.Pause();
                }
                else
                {
                    back = true;
                }
            }
        }

        private static void PrintTicketDetails(Ticket ticket)
        {
            ConsoleUI.PrintHeader($"Ticket #{ticket.Id}");
            ConsoleUI.PrintInfo($"Title: {ticket.Title}");
            ConsoleUI.PrintInfo($"Description: {ticket.Description}");
            ConsoleUI.PrintInfo($"Status: {ticket.Status}");
            ConsoleUI.PrintInfo($"Priority: {ticket.Priority?.ToString() ?? "-"}");
            ConsoleUI.PrintInfo($"Department: {ticket.Department?.ToString() ?? "-"}");
            ConsoleUI.PrintInfo($"Due: {(ticket.DueDate.HasValue ? ticket.DueDate.Value.ToString("g") : "-")}");

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
                    User? author = DataStore.GetUserById(comment.AuthorId);
                    string roleLabel = author != null ? author.Role.ToString() : "Unknown";
                    ConsoleUI.PrintLine($"  [{roleLabel}] {comment.Text}");
                }
            }

            ConsoleUI.PrintDivider();
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
            // Only real departments here - GeneralQuestions is set automatically
            // by the "take ownership personally" path and is never chosen manually.
            string[] allNames = Enum.GetNames(typeof(Department));
            List<string> names = new List<string>();

            foreach (string n in allNames)
            {
                if (n != nameof(Department.GeneralQuestions))
                    names.Add(n);
            }

            string[] namesArray = names.ToArray();
            int choice = ConsoleUI.PrintMenu("Select department", namesArray);
            return (Department)Enum.Parse(typeof(Department), namesArray[choice - 1]);
        }
    }
}