using System.Collections.Generic;

namespace NavisBOQ.Plugin.Models
{
    public class SelectionSetValidationResult
    {
        public string ScopeName { get; set; } = "";
        public int TotalVisited { get; set; }
        public int ElementNodes { get; set; }
        public int GeometricNodes { get; set; }
        public bool ContainsSteelCandidates { get; set; }
        public bool ContainsGenericOrAnnotations { get; set; }
        public List<string> TopCategories { get; set; } = new List<string>();
        public string RecommendedMode { get; set; } = "manual_required";
        public string Message { get; set; } = "";
        // Electrical RUN
        public bool ContainsElectricalCandidates { get; set; }
        public bool ContainsForbiddenPlumbing { get; set; }
    }
}