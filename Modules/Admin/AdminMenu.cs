using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Security;
using HelpDeskTicketingSystem.UI;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

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
                    "View - Filter All Users",
                    "Delete User Account",
                    "View - Filter All Tickets",
                    "Delete Tickets",
                    "Logout"
                };

                int choice = ConsoleUI.PrintMenu("Admin Dashboard - Choose action plz:", options);

                switch (choice)
                {
                    case 1:
                        CreateAgent();
                        break;
                    case 2:
                        CreateDispatcher();
                        break;
                    case 3:
                        ViewUsers();
                        break;
                    case 4:
                        DeleteUser(currentAdmin);
                        break;
                    case 5:
                        ViewTickets();
                        break;
                    case 6:
                        DeleteTicket();
                        break;
                    case 7:
                        ConsoleUI.PrintSuccess("Logged out successfully.");
                        ConsoleUI.Pause();
                        return;
                }
            }
        }

        private static void CreateAgent() {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Create New Agent Account");
            
            string name =ConsoleUI.ReadName("Enter Agent Name: ");
            string email = ConsoleUI.ReadEmail("Enter Agent Email: ");

            if(DataStore.GetUserByEmail(email) != null)
            {
                ConsoleUI.PrintError("Email already exists. Please try again.");
                ConsoleUI.Pause();
                return;
            }
            string password = ConsoleUI.ReadPassword("Enter Agent Password: ");

            string[]departments = Enum.GetNames(typeof(Department));
            int dept=ConsoleUI.PrintMenu("Select Department:", departments);
            Department selectedDepartment= (Department)Enum.Parse(typeof(Department), departments[dept-1]);

            var agent = new HelpDeskTicketingSystem.Models.Agent
            {
                Id = DataStore.GetNextUserId(),
                Name = name,
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                Department = selectedDepartment,
                MustChangePassword = true
            };
            DataStore.Users.Add(agent);

            ConsoleUI.PrintSuccess($"Agent account '{agent.Name}' created successfully for '{selectedDepartment}' department!");
            ConsoleUI.Pause();
        }
        private static void CreateDispatcher() {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Create New Dispatcher Account");

            string name = ConsoleUI.ReadName("Enter Dispatcher Name: ");
            string email = ConsoleUI.ReadEmail("Enter Dispatcher Email: ");
            if (DataStore.GetUserByEmail(email) != null)
            {
                ConsoleUI.PrintError("Email already exists. Please try again.");
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
        private static void ViewUsers() {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("View or Filter Users");

            string[] filter = new string[] {
                "1- All Users",
                "2- Clients Only",
                "3- Agents Only",
                "4- Dispatchers Only",
                "5- Back to Menu"
            };
            int choice = ConsoleUI.PrintMenu("Select user filter option:", filter);
            if(choice == 5) return;
            var users = DataStore.Users;
            List<HelpDeskTicketingSystem.Models.User> filteredUsers = new List<HelpDeskTicketingSystem.Models.User>();
            switch(choice)
            {
                case 1:
                    filteredUsers = users;
                    break;
                case 2:
                    filteredUsers = users.FindAll(u => u is HelpDeskTicketingSystem.Models.Client);
                    break;
                case 3:
                    filteredUsers = users.FindAll(u => u is HelpDeskTicketingSystem.Models.Agent);
                    break;
                case 4:
                    filteredUsers = users.FindAll(u => u is HelpDeskTicketingSystem.Models.Dispatcher);
                    break;
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
        private static void DeleteUser(HelpDeskTicketingSystem.Models.Admin currentAdmin) {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Delete User Account");
            string email = ConsoleUI.ReadEmail("Enter the email of the user to delete: ");
            var user = DataStore.GetUserByEmail(email);
            if (user == null)
            {
                ConsoleUI.PrintError("User not found.");
                ConsoleUI.Pause();
                return;
            }
            if (user.Id == currentAdmin.Id)
            {
                ConsoleUI.PrintError("You cannot delete your own account.");
                ConsoleUI.Pause();
                return;
            }
            bool tickets = DataStore.Tickets.Exists(t => t.ClientId == user.Id || t.AssignedUserId == user.Id);
            if (tickets)
            {
                ConsoleUI.PrintError("Cannot delete this user because they are still linked to active/existing tickets.");
                ConsoleUI.Pause();
                return;
            }
            if(ConsoleUI.Confirm($"Are you sure you want to delete user '{user.Name}'? This action cannot be undone. (y/n): "))
            {
                DataStore.Users.Remove(user);
                ConsoleUI.PrintSuccess($"User '{user.Name}' deleted successfully.");
            }
            else
            {
                ConsoleUI.PrintInfo("User deletion canceled.");
            }
            ConsoleUI.Pause();
        }
        private static void ViewTickets()
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("View or Filter All Tickets");
            var tickets = DataStore.Tickets;
            if (tickets.Count == 0)
            {
                ConsoleUI.PrintWarning("No tickets found.");
                ConsoleUI.Pause();
                return;
            }
            string[] filterOptions = new string[] {
                "All Tickets",
                "Open Tickets",
                "Assigned Tickets",
                "In Progress Tickets",
                "Resolved Tickets",
                "Closed Tickets",
                "Back to Menu"
            };
            int choice = ConsoleUI.PrintMenu("Select ticket filter:", filterOptions);

            if (choice == 7) return;

            var filteredTickets = choice switch
            {
                1 => tickets,
                2 => tickets.FindAll(t => t.Status == HelpDeskTicketingSystem.Enums.TicketStatus.Open),
                3 => tickets.FindAll(t => t.Status == HelpDeskTicketingSystem.Enums.TicketStatus.Assigned),
                4 => tickets.FindAll(t => t.Status == HelpDeskTicketingSystem.Enums.TicketStatus.InProgress),
                5 => tickets.FindAll(t => t.Status == HelpDeskTicketingSystem.Enums.TicketStatus.Resolved),
                6 => tickets.FindAll(t => t.Status == HelpDeskTicketingSystem.Enums.TicketStatus.Closed),
                _ => tickets
            };
            if (filteredTickets.Count == 0)
            {
                ConsoleUI.PrintWarning("No tickets found for the selected filter.");
            }
            else
            {
                ConsoleUI.PrintHeader("Tickets List");
                string[] headers = new string[] {
                    "ID", 
                    "Title",
                    "Priority",
                    "Department", 
                    "Status"
                };
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
        private static void DeleteTicket() {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("Delete Ticket");

            ConsoleUI.Print("Enter ticket ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int ticketId))
            {
                ConsoleUI.PrintError("Invalid ticket ID.");
                ConsoleUI.Pause();
                return;
            }
            var ticket = DataStore.Tickets.Find(t => t.Id == ticketId);
            if (ticket == null)
            {
                ConsoleUI.PrintError($"No ticket found with ID: {ticketId}");
                ConsoleUI.Pause();
                return;
            }
          if(ConsoleUI.Confirm($"Are you sure you want to delete ticket '{ticket.Title}'? This action cannot be undone. (y/n): "))
            {
                DataStore.Tickets.Remove(ticket);
                ConsoleUI.PrintSuccess($"Ticket '{ticket.Title}' deleted successfully.");
            }
            else
            {
                ConsoleUI.PrintInfo("Ticket deletion canceled.");
            }
            ConsoleUI.Pause();
        }

    }
}
