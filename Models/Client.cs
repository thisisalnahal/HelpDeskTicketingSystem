using HelpDeskTicketingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDeskTicketingSystem.Models
{
    internal class Client : User
    {
        public override Role Role
        {
            get { return Role.Client; }
        }
    }
}
