using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using HelpDeskTicketingSystem.Modules.Admin;
using HelpDeskTicketingSystem.Modules.Agent;
using HelpDeskTicketingSystem.Modules.Client;
using HelpDeskTicketingSystem.Modules.Dispatcher;
using HelpDeskTicketingSystem.Security;
using HelpDeskTicketingSystem.Services;
using HelpDeskTicketingSystem.UI;

namespace HelpDeskTicketingSystem
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            //ConsoleUI.PrintHeader("Help Desk Ticketing System");
            //ConsoleUI.PrintDivider();
            //ConsoleUI.PrintSuccess("Ticket added successfuly");
            //ConsoleUI.PrintError("Failed");
            //ConsoleUI.PrintWarning("OverDue");
            //ConsoleUI.PrintInfo("Hello");

            //ConsoleUI.PrintMenu("choose: ", ["a", "b", "c", "d"]);

            //List<Ticket> tickets = new() {
            //    new Ticket() { Id = 1 , Title = "T1", Description="D1"},
            //    new Ticket() { Id = 2 , Title = "T2", Description="D2"},
            //    new Ticket() { Id = 3 , Title = "T3", Description="D3"}
            //};

            //List<string[]> rows = new();
            //foreach (var ticket in tickets)
            //{
            //    rows.Add([
            //        ticket.Id.ToString(),
            //        ticket.Title
            //        ]);
            //}

            //ConsoleUI.PrintTable(["ID", "Title"], rows);

            //ConsoleUI.ReadEmail("Enter your Email: ");
            //ConsoleUI.ReadPassword("Enter your password: ");

            //Client client = new Client { Id = 1, Name = "Ahmed", Email = "ahmed@gmail.com", PasswordHash = "00000000", MustChangePassword = false};
            //ConsoleUI.PrintLine(client.ToString());
            //ConsoleUI.Pause();

            DataStore.Seed();

            while (true)
            {
                ConsoleUI.ClearScreen();
                ConsoleUI.PrintHeader("HELP DESK TICKETING SYSTEM");

                // Doc section 9: run auto-close housekeeping every time we return to
                // the login screen, so no Resolved ticket sits past 3 days unnoticed.
                TicketStatusService.RunAutoCloseCheck();

                int choice = ConsoleUI.PrintMenu("Welcome",
                [
                    "Login",
                    "Register",
                    "Exit"
                ]);

                if (choice == 1)
                {
                    Login();
                }
                else if (choice == 2)
                {
                    Register();
                }
                else
                {
                    ConsoleUI.PrintLine("Goodbye!");
                    break;
                }
            }

        }
        static void Login()
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("LOGIN");
            
            string email = ConsoleUI.ReadEmail("Email: ");
            string password = ConsoleUI.ReadPassword("Password: ");

            User? user = DataStore.GetUserByEmail(email);
            if (user == null || !PasswordHelper.Verify(password, user.PasswordHash))
            {
                ConsoleUI.PrintError("Invalid email or password.");
                ConsoleUI.Pause();
                return;
            }

            if (user.MustChangePassword)
            {
                ConsoleUI.PrintWarning("You must change your password before continuing.");
                string newPassword = ConsoleUI.ReadPassword("New password: ");
                user.PasswordHash = PasswordHelper.Hash(newPassword);
                user.MustChangePassword = false;
                ConsoleUI.PrintSuccess("Password changed.");
                ConsoleUI.Pause();
            }

            switch (user)
            {
                case Client client:
                    ClientMenu.Show(client);
                    break;

                case Models.Dispatcher dispatcher:
                    DispatcherMenu.Show(dispatcher);
                    break;

                case Agent agent:
                    AgentMenu.Show(agent);
                    break;

                case Admin admin:
                    AdminMenu.Show(admin);
                    break;
            }
        }

        static void Register()
        {
            ConsoleUI.ClearScreen();
            ConsoleUI.PrintHeader("CLIENT REGISTRATION");

            string name = ConsoleUI.ReadName("Enter Your Name: ");
            string email = ConsoleUI.ReadEmail("Enter Your Email: ");
            string password = ConsoleUI.ReadPassword("Enter Your Password: ");

            if (DataStore.GetUserByEmail(email) != null)
            {
                ConsoleUI.PrintError("An account with this email already exists.");
                ConsoleUI.Pause();
                return;
            }

            var client = new Client
            {
                Id = DataStore.GetNextUserId(),
                Name = name,
                Email = email,
                PasswordHash = PasswordHelper.Hash(password),
                MustChangePassword = false
            };

            DataStore.Users.Add(client);
            ConsoleUI.PrintSuccess("Account created. You can now log in.");
            ConsoleUI.Pause();
        }
    }
}
