using System.Collections.Generic;

namespace NavisBOQ.Plugin.Models
{
    public class ExecutionModeDecision
    {
        public string Mode { get; set; } = "auto_safe"; // auto_safe | auto_summary_only | manual_required
        public string Reason { get; set; } = "";
        public bool ForceSummary { get; set; }
        public bool AllowAutoRun { get; set; } = true;
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }
}