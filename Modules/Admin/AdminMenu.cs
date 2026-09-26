using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Security;
using HelpDeskTicketingSystem.UI;
using System;
using System.Collections.Generic;

namespace HelpDeskTicketingSystem.Modules.Admin
{
    internal class AdminMenu
    {
        public static void Show(HelpDeskTicketingSystem.Models.Admin currentAdmin)
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                ConsoleUI.PrintHeader($"Welcome Admin: {currentAdmin.Name}");

                string[] options = new string[] {
                    "Create Agent Account",
                    "Create Dispatcher Account",
                    "View / Filter All Users",
                    "View / Filter All Tickets",
                    "Logout"
                };

                int choice = ConsoleUI.PrintMenu("Admin Dashboard - Choose action:", options);

                switch (choice)
                {
                    case 1:
                        CreateAgent();
                        break;
                    case 2:
                        CreateDispatcher();
                        break;
                    case 3:
                        ViewAndManageUsers(currentAdmin);
                        break;
                    case 4:
                        ViewAndManageTickets();
                        break;
                    case 5:
                        ConsoleUI.PrintSuccess("Logged out successfully.");
                        ConsoleUI.Pause();
                        return;
                }
            }
        }

        private static void CreateAgent()
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Create New Agent Account");

            string name = ConsoleUI.ReadName("Enter Agent Name: ");
            string email = ConsoleUI.ReadEmail("Enter Agent Email: ");

            User existingUser = null;
            foreach (var u in DataStore.Users)
            {
                if (u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    existingUser = u;
                    break;
                }
            }

            if (existingUser != null)
            {
                ConsoleUI.PrintError("Email already exists in the system.");
                ConsoleUI.Pause();
                return;
            }

            string password = ConsoleUI.ReadPassword("Enter Agent Password: ");

            string[] deptOptions = new string[] {
                "Hardware",
                "Software",
                "Network",
                "Billing"
            };

            int deptChoice = ConsoleUI.PrintMenu("Select Department for Agent", deptOptions);
            string selectedDept = deptOptions[deptChoice - 1];

            var agent = new HelpDeskTicketingSystem.Models.Agent
            {
                Id = DataStore.GetNextUserId(),
                Name = name,
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                MustChangePassword = true
            };

            DataStore.Users.Add(agent);
            ConsoleUI.PrintSuccess($"Agent '{agent.Name}' created successfully in department '{selectedDept}'!");
            ConsoleUI.Pause();
        }

        private static void CreateDispatcher()
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Create New Dispatcher Account");

            string name = ConsoleUI.ReadName("Enter Dispatcher Name: ");
            string email = ConsoleUI.ReadEmail("Enter Dispatcher Email: ");

            User existingUser = null;
            foreach (var u in DataStore.Users)
            {
                if (u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    existingUser = u;
                    break;
                }
            }

            if (existingUser != null)
            {
                ConsoleUI.PrintError("Email already exists in the system.");
                ConsoleUI.Pause();
                return;
            }

            string password = ConsoleUI.ReadPassword("Enter Dispatcher Password: ");

            var dispatcher = new HelpDeskTicketingSystem.Models.Dispatcher
            {
                Id = DataStore.GetNextUserId(),
                Name = name,
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                MustChangePassword = true
            };

            DataStore.Users.Add(dispatcher);
            ConsoleUI.PrintSuccess($"Dispatcher account '{dispatcher.Name}' created successfully!");
            ConsoleUI.Pause();
        }

        private static void ViewAndManageUsers(HelpDeskTicketingSystem.Models.Admin currentAdmin)
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                ConsoleUI.PrintHeader("View or Filter Users");

                string[] filter = new string[] {
                    "All Users",
                    "Clients Only",
                    "Agents Only",
                    "Dispatchers Only",
                    "Delete a User",
                    "Back to Menu"
                };

                int choice = ConsoleUI.PrintMenu("Select user filter/action option:", filter);

                if (choice == 6) return;

                if (choice == 5)
                {
                    DeleteUserProcess(currentAdmin);
                    continue;
                }

                List<User> filteredUsers = new List<User>();
                foreach (var user in DataStore.Users)
                {
                    if (choice == 1)
                    {
                        filteredUsers.Add(user);
                    }
                    else if (choice == 2 && user is HelpDeskTicketingSystem.Models.Client)
                    {
                        filteredUsers.Add(user);
                    }
                    else if (choice == 3 && user is HelpDeskTicketingSystem.Models.Agent)
                    {
                        filteredUsers.Add(user);
                    }
                    else if (choice == 4 && user is HelpDeskTicketingSystem.Models.Dispatcher)
                    {
                        filteredUsers.Add(user);
                    }
                }

                if (filteredUsers.Count == 0)
                {
                    ConsoleUI.PrintWarning("No users found for the selected filter.");
                }
                else
                {
                    ConsoleUI.PrintHeader("Users List");
                    string[] headers = new string[] { "ID", "Name", "Email", "Role" };
                    List<string[]> rows = new List<string[]>();

                    foreach (var user in filteredUsers)
                    {
                        string role = user.GetType().Name;
                        rows.Add(new string[] { user.Id.ToString(), user.Name, user.Email, role });
                    }

                    ConsoleUI.PrintTable(headers, rows);
                }

                ConsoleUI.Pause();
            }
        }

        private static void DeleteUserProcess(HelpDeskTicketingSystem.Models.Admin currentAdmin)
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Delete User Account");

            string email = ConsoleUI.ReadEmail("Enter the email of the user to delete: ");

            User targetUser = null;
            foreach (var u in DataStore.Users)
            {
                if (u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    targetUser = u;
                    break;
                }
            }

            if (targetUser == null)
            {
                ConsoleUI.PrintError("User not found.");
                ConsoleUI.Pause();
                return;
            }

            if (targetUser.Id == currentAdmin.Id)
            {
                ConsoleUI.PrintError("You cannot delete your own account.");
                ConsoleUI.Pause();
                return;
            }

            bool hasTickets = false;
            foreach (var t in DataStore.Tickets)
            {
                if (t.ClientId == targetUser.Id || t.AssignedUserId == targetUser.Id)
                {
                    hasTickets = true;
                    break;
                }
            }

            if (hasTickets)
            {
                ConsoleUI.PrintError("Cannot delete this user because they are still linked to active/existing tickets.");
                ConsoleUI.Pause();
                return;
            }

            if (ConsoleUI.Confirm($"Are you sure you want to delete user '{targetUser.Name}'? (y/n): "))
            {
                DataStore.Users.Remove(targetUser);
                ConsoleUI.PrintSuccess($"User '{targetUser.Name}' deleted successfully.");
            }
            else
            {
                ConsoleUI.PrintInfo("User deletion canceled.");
            }
            ConsoleUI.Pause();
        }

        private static void ViewAndManageTickets()
        {
            while (true)
            {
                ConsoleUI.ClearScreen();
                ConsoleUI.PrintHeader("View or Filter All Tickets");

                string[] filterOptions = new string[] {
                    "All Tickets",
                    "Open Tickets",
                    "Assigned Tickets",
                    "In Progress Tickets",
                    "Resolved Tickets",
                    "Closed Tickets",
                    "Delete a Ticket",
                    "Back to Menu"
                };

                int choice = ConsoleUI.PrintMenu("Select ticket filter/action:", filterOptions);

                if (choice == 8) return;

                if (choice == 7)
                {
                    DeleteTicketProcess();
                    continue;
                }

                List<Ticket> filteredTickets = new List<Ticket>();
                foreach (var t in DataStore.Tickets)
                {
                    if (choice == 1) filteredTickets.Add(t);
                    else if (choice == 2 && t.Status == TicketStatus.Open) filteredTickets.Add(t);
                    else if (choice == 3 && t.Status == TicketStatus.Assigned) filteredTickets.Add(t);
                    else if (choice == 4 && t.Status == TicketStatus.InProgress) filteredTickets.Add(t);
                    else if (choice == 5 && t.Status == TicketStatus.Resolved) filteredTickets.Add(t);
                    else if (choice == 6 && t.Status == TicketStatus.Closed) filteredTickets.Add(t);
                }

                if (filteredTickets.Count == 0)
                {
                    ConsoleUI.PrintWarning("No tickets found for the selected filter.");
                }
                else
                {
                    ConsoleUI.PrintHeader("Tickets List");
                    string[] headers = new string[] { "ID", "Title", "Priority", "Department", "Status" };
                    List<string[]> rows = new List<string[]>();

                    foreach (var ticket in filteredTickets)
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
                }
                ConsoleUI.Pause();
            }
        }

        private static void DeleteTicketProcess()
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Delete Ticket");

            ConsoleUI.Print("Enter ticket ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int ticketId))
            {
                ConsoleUI.PrintError("Invalid ticket ID.");
                ConsoleUI.Pause();
                return;
            }

            Ticket targetTicket = null;
            foreach (var t in DataStore.Tickets)
            {
                if (t.Id == ticketId)
                {
                    targetTicket = t;
                    break;
                }
            }

            if (targetTicket == null)
            {
                ConsoleUI.PrintError($"No ticket found with ID: {ticketId}");
                ConsoleUI.Pause();
                return;
            }

            if (ConsoleUI.Confirm($"Are you sure you want to delete ticket '{targetTicket.Title}'? (y/n): "))
            {
                DataStore.Tickets.Remove(targetTicket);
                ConsoleUI.PrintSuccess($"Ticket '{targetTicket.Title}' deleted successfully.");
            }
            else
            {
                ConsoleUI.PrintInfo("Ticket deletion canceled.");
            }
            ConsoleUI.Pause();
        }
    }
}