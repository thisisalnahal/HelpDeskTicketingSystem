using HelpDeskTicketingSystem.UI;
using System;
using System.Collections.Generic;
using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Security;
using HelpDeskTicketingSystem.Services;

namespace HelpDeskTicketingSystem.Modules.Client
{
    internal class ClientMenu
    {
        public static void Show(Models.Client client)
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                int choice = ConsoleUI.PrintMenu(
                    "Client Menu",
                    new string[]
                    {
                        "Create Ticket",
                        "View My Tickets",
                        "Edit Profile",
                        "Change Password",
                        "Logout"
                    });

                switch (choice)
                {
                    case 1:
                        CreateTicket(client);
                        break;

                    case 2:
                        ViewMyTickets(client);
                        break;

                    case 3:
                        EditProfile(client);
                        break;

                    case 4:
                        ChangePassword(client);
                        break;

                    case 5:
                        ConsoleUI.PrintInfo("Logging out...");
                        ConsoleUI.Pause();
                        return;
                }
            }
        }

        private static void CreateTicket(Models.Client client)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Create Ticket");

            string title = ConsoleUI.ReadName("Enter ticket title: ");
            string description = ConsoleUI.ReadName("Enter ticket description: ");

            Ticket ticket = new Ticket
            {
                Id = DataStore.GetNextTicketId(),
                Title = title,
                Description = description,
                Status = TicketStatus.Open,
                ClientId = client.Id
            };

            DataStore.Tickets.Add(ticket);

            ConsoleUI.PrintSuccess($"Ticket #{ticket.Id} created successfully.");
            ConsoleUI.Pause();
        }

        private static void ViewMyTickets(Models.Client client)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("My Tickets");

            List<Ticket> tickets = DataStore.GetTicketsByClient(client.Id);

            if (tickets.Count == 0)
            {
                ConsoleUI.PrintInfo("You have no tickets.");
                ConsoleUI.Pause();
                return;
            }

            string[] headers = new string[] { "ID", "Title", "Priority", "Department", "Status" };
            List<string[]> rows = new List<string[]>();

            foreach (var ticket in tickets)
            {
                rows.Add(new string[] {
                    ticket.Id.ToString(),
                    ticket.Title,
                    ticket.Priority.ToString(),
                    ticket.Department.ToString(),
                    ticket.Status.ToString()
                });
            }
            ConsoleUI.PrintTable(headers, rows);

            ConsoleUI.PrintDivider();
            ConsoleUI.Print("Enter Ticket ID to view details or 0 to go back: ");

            if (!int.TryParse(Console.ReadLine(), out int ticketId))
            {
                ConsoleUI.PrintError("Invalid Ticket ID.");
                ConsoleUI.Pause();
                return;
            }

            if (ticketId == 0)
                return;

            Ticket selectedTicket = DataStore.GetTicketById(ticketId);

            if (selectedTicket == null || selectedTicket.ClientId != client.Id)
            {
                ConsoleUI.PrintError("Ticket not found.");
                ConsoleUI.Pause();
                return;
            }

            ShowTicketDetails(client, selectedTicket);
        }

        // Loops on a single ticket's details screen so actions taken here (comment,
        // close) redraw the updated ticket immediately, instead of forcing the
        // client back to the list to see the result of what they just did.
        private static void ShowTicketDetails(Models.Client client, Ticket ticket)
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                ConsoleUI.PrintHeader("Ticket Details");

                ConsoleUI.PrintInfo($"ID: {ticket.Id}");
                ConsoleUI.PrintInfo($"Title: {ticket.Title}");
                ConsoleUI.PrintInfo($"Description: {ticket.Description}");
                ConsoleUI.PrintInfo($"Status: {ticket.Status}");
                ConsoleUI.PrintInfo($"Priority: {ticket.Priority}");
                ConsoleUI.PrintInfo($"Department: {ticket.Department}");
                ConsoleUI.PrintInfo($"Created: {ticket.CreatedAt:yyyy-MM-dd}");

                ConsoleUI.PrintDivider();
                ConsoleUI.PrintInfo("Comments:");

                if (ticket.Comments.Count == 0)
                {
                    ConsoleUI.PrintInfo("No comments yet.");
                }
                else
                {
                    foreach (Comment comment in ticket.Comments)
                    {
                        // Look up the ACTUAL author of each comment, not the
                        // currently logged-in client - a comment thread can
                        // include the Agent/Dispatcher too.
                        User author = DataStore.GetUserById(comment.AuthorId);
                        string authorName = author != null ? author.Name : "Unknown";

                        ConsoleUI.PrintInfo($"{authorName} - {comment.CreatedAt:yyyy-MM-dd HH:mm}");
                        ConsoleUI.PrintInfo($"  {comment.Text}");
                    }
                }

                ConsoleUI.PrintDivider();

                bool canComment = CommentPermissionService.CanComment(ticket, client);
                bool canClose = ticket.Status == TicketStatus.Resolved;

                var options = new List<string>();
                if (canComment) options.Add("Add comment");
                if (canClose) options.Add("Close ticket");
                options.Add("Back");

                int choice = ConsoleUI.PrintMenu("Choose operation", options.ToArray());
                string selected = options[choice - 1];

                if (selected == "Add comment")
                {
                    AddComment(client, ticket);
                }
                else if (selected == "Close ticket")
                {
                    if (CloseTicket(client, ticket))
                        return; // ticket closed - nothing left to do here, back to list
                }
                else
                {
                    return; // Back
                }
            }
        }

        private static void AddComment(Models.Client client, Ticket ticket)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Add Comment");

            if (!CommentPermissionService.CanComment(ticket, client))
            {
                ConsoleUI.PrintError("You are not allowed to comment on this ticket.");
                ConsoleUI.Pause();
                return;
            }

            string text = ConsoleUI.ReadName("Enter your comment: ");

            Comment comment = new Comment
            {
                Id = DataStore.GetNextCommentId(),
                TicketId = ticket.Id,
                AuthorId = client.Id,
                Text = text
            };

            ticket.Comments.Add(comment);

            ConsoleUI.PrintSuccess("Comment added successfully.");
            ConsoleUI.Pause();
        }

        // Returns true if the ticket was actually closed, so the caller knows
        // whether to keep showing details (cancelled) or go back (closed).
        private static bool CloseTicket(Models.Client client, Ticket ticket)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Close Ticket");

            if (ticket.Status != TicketStatus.Resolved)
            {
                ConsoleUI.PrintError("Only resolved tickets can be closed.");
                ConsoleUI.Pause();
                return false;
            }

            if (!ConsoleUI.Confirm("Close this ticket?"))
            {
                ConsoleUI.PrintInfo("Cancelled.");
                ConsoleUI.Pause();
                return false;
            }

            string error;
            if (TicketStatusService.TryTransition(ticket, TicketStatus.Closed, client, out error))
            {
                ConsoleUI.PrintSuccess("Ticket closed successfully.");
                ConsoleUI.Pause();
                return true;
            }
            else
            {
                ConsoleUI.PrintError(error);
                ConsoleUI.Pause();
                return false;
            }
        }

        private static void EditProfile(Models.Client client)
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                int choice = ConsoleUI.PrintMenu(
                    "Edit Profile",
                    new string[]
                    {
                        "Edit Name",
                        "Edit Email",
                        "Back"
                    });

                switch (choice)
                {
                    case 1:
                        ConsoleUI.ClearScreen();
                        ConsoleUI.PrintHeader("Edit Name");

                        string name = ConsoleUI.ReadName("Enter new name: ");
                        client.Name = name;

                        ConsoleUI.PrintSuccess("Name updated successfully.");
                        ConsoleUI.Pause();
                        break;

                    case 2:
                        ConsoleUI.ClearScreen();
                        ConsoleUI.PrintHeader("Edit Email");

                        string email = ConsoleUI.ReadEmail("Enter new email: ");
                        client.Email = email;

                        ConsoleUI.PrintSuccess("Email updated successfully.");
                        ConsoleUI.Pause();
                        break;

                    case 3:
                        return;
                }
            }
        }

        private static void ChangePassword(Models.Client client)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Change Password");

            string currentPassword = ConsoleUI.ReadPassword("Enter current password: ");

            if (!PasswordHelper.Verify(currentPassword, client.PasswordHash))
            {
                ConsoleUI.PrintError("Current password is incorrect.");
                ConsoleUI.Pause();
                return;
            }

            string newPassword = ConsoleUI.ReadPassword("Enter new password: ");
            string confirmPassword = ConsoleUI.ReadPassword("Confirm new password: ");

            if (newPassword != confirmPassword)
            {
                ConsoleUI.PrintError("Passwords do not match.");
                ConsoleUI.Pause();
                return;
            }

            client.PasswordHash = PasswordHelper.Hash(newPassword);
            client.MustChangePassword = false;

            ConsoleUI.PrintSuccess("Password changed successfully.");
            ConsoleUI.Pause();
        }
    }
}