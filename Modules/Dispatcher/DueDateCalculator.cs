using HelpDeskTicketingSystem.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HelpDeskTicketingSystem.Modules.Dispatcher
{
    internal class DueDateCalculator
    {
        // doc section 5: CreatedAt + {4h Urgent, 24h High, 48h Medium, 72h Low}
        public static DateTime Calculate(TicketPriority priority, DateTime from)
        {
            int hours = priority switch
            {
                TicketPriority.Urgent => 4,
                TicketPriority.High => 24,
                TicketPriority.Medium => 48,
                TicketPriority.Low => 72,
                _ => 72
            };

            return from.AddHours(hours);
        }
    }
}