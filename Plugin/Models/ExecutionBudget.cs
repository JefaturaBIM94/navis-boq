using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    /// <summary>
    /// Define límites de elementos y nodos para cada tipo de corrida.
    /// </summary>
    public class ExecutionBudget
    {
        public int GreenCandidateLimit { get; set; }
        public int YellowCandidateLimit { get; set; }
        public int MaxNodesToVisit { get; set; }
        public int MaxDetailRows { get; set; }
        public int TimeoutMs { get; set; }
    }

    /// <summary>
    /// Presupuestos predefinidos por tipo de corrida.
    /// </summary>
    public static class BudgetProfiles
    {
        public static ExecutionBudget Corrida1 => new ExecutionBudget
        {
            GreenCandidateLimit = 10_000,
            YellowCandidateLimit = 25_000,
            MaxNodesToVisit = 50_000,
            MaxDetailRows = 6_000,
            TimeoutMs = 90_000
        };

        public static ExecutionBudget Corrida2 => new ExecutionBudget
        {
            GreenCandidateLimit = 10_000,
            YellowCandidateLimit = 25_000,
            MaxNodesToVisit = 50_000,
            MaxDetailRows = 6_000,
            TimeoutMs = 90_000
        };

        public static ExecutionBudget Corrida3 => new ExecutionBudget
        {
            GreenCandidateLimit = 5_000,
            YellowCandidateLimit = 12_000,
            MaxNodesToVisit = 50_000,
            MaxDetailRows = 5_000,
            TimeoutMs = 120_000
        };

        public static ExecutionBudget Corrida4 => new ExecutionBudget
        {
            GreenCandidateLimit = 8_000,
            YellowCandidateLimit = 18_000,
            MaxNodesToVisit = 50_000,
            MaxDetailRows = 6_000,
            TimeoutMs = 90_000
        };

    }
}
