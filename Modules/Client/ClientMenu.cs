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
            ConsoleUI.ClearScreen();
            while (true)
            {
                int choice = ConsoleUI.PrintMenu(
                    "Client Menu",
                    new string[]
                    {
                        "Create Ticket",
                        "View My Tickets And Details",
                        "Add Comment",
                        "Close Ticket",
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
                        ViewMyTicketsAndDetails(client);
                        break;

                    case 3:
                        AddComment(client);
                        break;

                    case 4:
                        CloseTicket(client);
                        break;

                    case 5:
                        EditProfile(client);
                        break;

                    case 6:
                        ChangePassword(client);
                        break;

                    case 7:
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

        private static void ViewMyTicketsAndDetails(Models.Client client)
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

            
            foreach (Ticket ticket in tickets)
            {
                ConsoleUI.PrintInfo(
                    $"#{ticket.Id} | {ticket.Title} | {ticket.Status} | {ticket.CreatedAt:yyyy-MM-dd}"
                );
            }

            ConsoleUI.PrintDivider();

           
            ConsoleUI.Print("Enter Ticket ID to view details : ");

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

       
            ConsoleUI.PrintDivider();
            ConsoleUI.PrintHeader("Ticket Details");

            ConsoleUI.PrintInfo($"ID: {selectedTicket.Id}");
            ConsoleUI.PrintInfo($"Title: {selectedTicket.Title}");
            ConsoleUI.PrintInfo($"Description: {selectedTicket.Description}");
            ConsoleUI.PrintInfo($"Status: {selectedTicket.Status}");
            ConsoleUI.PrintInfo($"Priority: {selectedTicket.Priority}");
            ConsoleUI.PrintInfo($"Department: {selectedTicket.Department}");
            ConsoleUI.PrintInfo($"Created: {selectedTicket.CreatedAt:yyyy-MM-dd}");

            ConsoleUI.PrintDivider();
            ConsoleUI.PrintInfo("Comments:");

            if (selectedTicket.Comments.Count == 0)
            {
                ConsoleUI.PrintInfo("No comments yet.");
            }
            else
            {
                foreach (Comment comment in selectedTicket.Comments)
                {
                    ConsoleUI.PrintInfo(
                        $"{comment.CreatedAt:yyyy-MM-dd HH:mm} - {comment.Text}"
                    );
                }
            }

            ConsoleUI.Pause();
        }
        
        private static void AddComment(Models.Client client)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Add Comment");

            ConsoleUI.Print("Enter Ticket ID: ");

            if (!int.TryParse(Console.ReadLine(), out int ticketId))
            {
                ConsoleUI.PrintError("Invalid Ticket ID.");
                ConsoleUI.Pause();
                return;
            }

            Ticket ticket = DataStore.GetTicketById(ticketId);

            if (ticket == null || ticket.ClientId != client.Id)
            {
                ConsoleUI.PrintError("Ticket not found.");
                ConsoleUI.Pause();
                return;
            }
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

        private static void CloseTicket(Models.Client client)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Close Ticket");

            ConsoleUI.Print("Enter Ticket ID: ");

            if (!int.TryParse(Console.ReadLine(), out int ticketId))
            {
                ConsoleUI.PrintError("Invalid Ticket ID.");
                ConsoleUI.Pause();
                return;
            }

            Ticket ticket = DataStore.GetTicketById(ticketId);

            if (ticket == null || ticket.ClientId != client.Id)
            {
                ConsoleUI.PrintError("Ticket not found.");
                ConsoleUI.Pause();
                return;
            }

            if (ticket.Status != TicketStatus.Resolved)
            {
                ConsoleUI.PrintError("Only resolved tickets can be closed.");
                ConsoleUI.Pause();
                return;
            }

            if (!ConsoleUI.Confirm("Close this ticket?"))
            {
                return;
            }

            string error;

            if (TicketStatusService.TryTransition(
                ticket,
                TicketStatus.Closed,
                client,
                out error))
            {
                ConsoleUI.PrintSuccess("Ticket closed successfully.");
            }
            else
            {
                ConsoleUI.PrintError(error);
            }

            ConsoleUI.Pause();
        }
        private static void EditProfile(Models.Client client)
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                ConsoleUI.PrintHeader("Edit Profile");

                ConsoleUI.PrintInfo("1. Edit Name");
                ConsoleUI.PrintInfo("2. Edit Email");
                ConsoleUI.PrintInfo("0. Back");

                ConsoleUI.Print("Choose: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ConsoleUI.ClearScreen();
                        ConsoleUI.PrintHeader("Edit Name");

                        string name = ConsoleUI.ReadName("Enter new name: ");
                        client.Name = name;

                        ConsoleUI.PrintSuccess("Name updated successfully.");
                        ConsoleUI.Pause();
                        break;

                    case "2":
                        ConsoleUI.ClearScreen();
                        ConsoleUI.PrintHeader("Edit Email");

                        string email = ConsoleUI.ReadEmail("Enter new email: ");
                        client.Email = email;

                        ConsoleUI.PrintSuccess("Email updated successfully.");
                        ConsoleUI.Pause();
                        break;

                    case "0":
                        return;

                    default:
                        ConsoleUI.PrintError("Invalid choice.");
                        ConsoleUI.Pause();
                        break;
                }
            }
        }

        private static void ChangePassword(Models.Client client)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Change Password");

            string currentPassword =
                ConsoleUI.ReadPassword("Enter current password: ");

            if (!PasswordHelper.Verify(
                currentPassword,
                client.PasswordHash))
            {
                ConsoleUI.PrintError("Current password is incorrect.");
                ConsoleUI.Pause();
                return;
            }

            string newPassword =
                ConsoleUI.ReadPassword("Enter new password: ");

            string confirmPassword =
                ConsoleUI.ReadPassword("Confirm new password: ");

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