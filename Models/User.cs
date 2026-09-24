using HelpDeskTicketingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDeskTicketingSystem.Models
{
    internal abstract class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        // True for every Agent/Dispatcher account created by an Admin (doc 8.2).
        // The login flow (Module 1) must force a password change while this is true, before the user can reach their menu.
        public bool MustChangePassword { get; set; }

        // Each subclass returns its own role name. Program.cs uses this after
        // login to decide which menu module to hand control to.
        public abstract Role Role { get; }

        public override string ToString() => $"[{Id}] {Name} ({Role}) - {Email}";
    }
}
