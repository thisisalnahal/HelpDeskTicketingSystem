using HelpDeskTicketingSystem.Domain.Entities;
using HelpDeskTicketingSystem.Domain.Enums;
using HelpDeskTicketingSystem.Interfaces.Auth;
using HelpDeskTicketingSystem.Interfaces.Roles;
using HelpDeskTicketingSystem.Interfaces.Tickets;
using HelpDeskTicketingSystem.Presentation.Helpers;
using HelpDeskTicketingSystem.Presentation.UI;

namespace HelpDeskTicketingSystem.Presentation.Menus
{
    internal class ClientMenu : IRoleMenu
    {
        private readonly IConsoleUI _ui;
        private readonly IClientService _clientService;
        private readonly IAuthService _authService;
        private readonly ICommentPermissionService _commentPermission;
        private readonly TicketDisplayHelper _ticketDisplay;

        public ClientMenu(
            IConsoleUI ui,
            IClientService clientService,
            IAuthService authService,
            ICommentPermissionService commentPermission,
            TicketDisplayHelper ticketDisplay)
        {
            _ui = ui;
            _clientService = clientService;
            _authService = authService;
            _commentPermission = commentPermission;
            _ticketDisplay = ticketDisplay;
        }

        public Role Role => Role.Client;

        public void Show(User user)
        {
            while (true)
            {
                _ui.ClearScreen();
                int choice = _ui.PrintMenu($"CLIENT MENU - {user.Name}",
                [
                    "Create ticket",
                    "My tickets",
                    "Edit profile",
                    "Change password",
                    "Logout"
                ]);

                switch (choice)
                {
                    case 1:
                        CreateTicket(user);
                        break;
                    case 2:
                        ViewMyTickets(user);
                        break;
                    case 3:
                        EditProfile(user);
                        break;
                    case 4:
                        ChangePassword(user);
                        break;
                    case 5:
                        _ui.PrintInfo("Logging out...");
                        _ui.Pause();
                        return;
                }
            }
        }

        private void CreateTicket(User user)
        {
            _ui.ClearScreen();
            _ui.PrintHeader("CREATE TICKET");

            string title = _ui.ReadText("Ticket title: ");
            string description = _ui.ReadText("Describe the problem: ");

            Ticket ticket = _clientService.CreateTicket(user.Id, title, description);

            _ui.PrintSuccess($"Ticket #{ticket.Id} created. A dispatcher will review it soon.");
            _ui.Pause();
        }

        private void ViewMyTickets(User user)
        {
            _ui.ClearScreen();
            _ui.PrintHeader("MY TICKETS");

            List<Ticket> tickets = _clientService.GetMyTickets(user.Id);

            if (tickets.Count == 0)
            {
                _ui.PrintInfo("You have no tickets.");
                _ui.Pause();
                return;
            }

            _ticketDisplay.PrintTicketTable(tickets);
            _ui.PrintDivider();

            int ticketId = _ui.ReadInt("Enter ticket ID to view details (0 to go back): ");

            if (ticketId == 0)
                return;

            if (_clientService.GetMyTicket(user.Id, ticketId) == null)
            {
                _ui.PrintError("Ticket not found.");
                _ui.Pause();
                return;
            }

            ShowTicketDetails(user, ticketId);
        }

        private void ShowTicketDetails(User user, int ticketId)
        {
            while (true)
            {
                Ticket? ticket = _clientService.GetMyTicket(user.Id, ticketId);

                if (ticket == null)
                {
                    _ui.PrintError("Ticket not found.");
                    _ui.Pause();
                    return;
                }

                _ui.ClearScreen();
                _ticketDisplay.PrintTicketDetails(ticket);

                List<string> labels = new List<string>();
                List<string> keys = new List<string>();

                if (_commentPermission.CanComment(ticket, user))
                {
                    labels.Add("Add comment");
                    keys.Add("comment");
                }

                if (ticket.Status == TicketStatus.Resolved)
                {
                    labels.Add("Confirm the solution and close the ticket");
                    keys.Add("close");
                }

                labels.Add("Back");
                keys.Add("back");

                int choice = _ui.PrintMenu("Choose operation", labels.ToArray());
                string key = keys[choice - 1];

                if (key == "comment")
                {
                    AddComment(user, ticket.Id);
                }
                else if (key == "close")
                {
                    if (CloseTicket(user, ticket.Id))
                        return;
                }
                else
                {
                    return;
                }
            }
        }

        private void AddComment(User user, int ticketId)
        {
            string text = _ui.ReadText("Your comment: ");

            if (_clientService.TryAddComment(user.Id, ticketId, text, out string error))
                _ui.PrintSuccess("Comment added.");
            else
                _ui.PrintError(error);

            _ui.Pause();
        }

        private bool CloseTicket(User user, int ticketId)
        {
            if (!_ui.Confirm("Close this ticket? No more comments will be possible"))
                return false;

            bool closed = _clientService.TryCloseTicket(user.Id, ticketId, out string error);

            if (closed)
                _ui.PrintSuccess("Ticket closed.");
            else
                _ui.PrintError(error);

            _ui.Pause();
            return closed;
        }

        private void EditProfile(User user)
        {
            while (true)
            {
                _ui.ClearScreen();
                int choice = _ui.PrintMenu("EDIT PROFILE",
                [
                    "Change name",
                    "Change email",
                    "Back"
                ]);

                if (choice == 1)
                {
                    string name = _ui.ReadName("New name: ");

                    if (_clientService.TryUpdateName(user.Id, name, out string error))
                        _ui.PrintSuccess("Name updated.");
                    else
                        _ui.PrintError(error);

                    _ui.Pause();
                }
                else if (choice == 2)
                {
                    string email = _ui.ReadEmail("New email: ");

                    if (_clientService.TryUpdateEmail(user.Id, email, out string error))
                        _ui.PrintSuccess("Email updated.");
                    else
                        _ui.PrintError(error);

                    _ui.Pause();
                }
                else
                {
                    return;
                }
            }
        }

        private void ChangePassword(User user)
        {
            _ui.ClearScreen();
            _ui.PrintHeader("CHANGE PASSWORD");

            string currentPassword = _ui.ReadPassword("Current password: ");
            string newPassword = _ui.ReadPassword("New password: ");
            string confirmPassword = _ui.ReadPassword("Confirm new password: ");

            if (newPassword != confirmPassword)
            {
                _ui.PrintError("The new passwords do not match.");
                _ui.Pause();
                return;
            }

            if (_authService.TryChangePassword(user.Id, currentPassword, newPassword, out string error))
                _ui.PrintSuccess("Password changed.");
            else
                _ui.PrintError(error);

            _ui.Pause();
        }
    }
}
