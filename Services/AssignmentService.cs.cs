using HelpDeskTicketingSystem.Data;
using HelpDeskTicketingSystem.Enums;
using HelpDeskTicketingSystem.Models;
using System.Collections.Generic;

namespace HelpDeskTicketingSystem.Services
{
    internal static class AssignmentService
    {
        public static Agent? GetLeastBusyAgent(Department department)
        {
            // Step 1: get every agent in this department
            List<Agent> agents = DataStore.GetAgentsByDepartment(department);

            // If there are no agents, we cannot assign anyone
            if (agents.Count == 0)
            {
                return null;
            }

            // Step 2: we will loop through agents and keep track of the best one so far
            Agent? bestAgent = null;
            int bestAgentLoad = int.MaxValue; // start with a huge number so the first agent always wins at first

            foreach (var agent in agents)
            {
                // How many open tickets does this agent currently have?
                int currentLoad = DataStore.GetActiveTicketCount(agent.Id);

                // Case 1: this is the very first agent we are checking
                if (bestAgent == null)
                {
                    bestAgent = agent;
                    bestAgentLoad = currentLoad;
                    continue; // go check the next agent
                }

                // Case 2: this agent has fewer tickets than our current best -> they win
                if (currentLoad < bestAgentLoad)
                {
                    bestAgent = agent;
                    bestAgentLoad = currentLoad;
                    continue;
                }

                // Case 3: this agent has the SAME number of tickets as our current best
                // -> we need the tiebreaker rule to decide
                if (currentLoad == bestAgentLoad)
                {
                    bool thisAgentWaitedLonger = IsLessRecentlyAssigned(agent, bestAgent);

                    if (thisAgentWaitedLonger)
                    {
                        bestAgent = agent;
                        bestAgentLoad = currentLoad;
                    }
                }

                // Case 4 (not written): this agent has MORE tickets than our best -> do nothing, keep current best
            }

            return bestAgent;
        }

        // Decides: has "candidate" waited longer since their last assignment than "current"?
        private static bool IsLessRecentlyAssigned(Agent candidate, Agent current)
        {
            // If candidate never got a ticket before, they win the tie
            // (unless current also never got one - then nobody wins, keep current)
            if (candidate.LastAssignedAt == null)
            {
                if (current.LastAssignedAt == null)
                {
                    return false; // both never assigned - keep current, no reason to switch
                }
                return true; // candidate never assigned, current was -> candidate wins
            }

            // If current never got a ticket before, current already wins the tie
            if (current.LastAssignedAt == null)
            {
                return false;
            }

            // Both have real dates - whoever's date is OLDER (earlier) waited longer
            return candidate.LastAssignedAt.Value < current.LastAssignedAt.Value;
        }
    }
}
