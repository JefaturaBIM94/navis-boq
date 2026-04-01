using System;
using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin.Services
{
    public class ModelStructurePolicyService : IModelStructurePolicyService
    {
        public bool IsWrapperNode(string displayName)
        {
            var n = (displayName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n)) return false;

            return n.EndsWith(".nwc", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith(".nwd", StringComparison.OrdinalIgnoreCase)
                || n.IndexOf(".rvt", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("ubicación Internal", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("location Internal", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf(": ubicación Internal", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf(": location Internal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool IsIgnorableContainer(string displayName)
        {
            var n = (displayName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n)) return true;
            if (IsWrapperNode(n)) return true;
            if (IsNoLevelNode(n)) return true;

            return false;
        }

        public bool IsNoLevelNode(string displayName)
        {
            var n = (displayName ?? "").Trim();
            return string.Equals(n, "<Sin nivel>", StringComparison.OrdinalIgnoreCase)
                || string.Equals(n, "<No level>", StringComparison.OrdinalIgnoreCase);
        }

        public bool LooksLikeLevelNode(string displayName)
        {
            var n = (displayName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n)) return false;
            if (IsWrapperNode(n)) return false;

            return n.IndexOf("LEVEL", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("NIVEL", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Level", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("N.P.T.", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("BASEMENT", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("ROOF", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("GROUND", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("PEDESTAL", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool LooksLikeLinkedContainer(string displayName)
        {
            var n = (displayName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n)) return false;

            return n.IndexOf(".rvt", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf(".ifc", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf(".dwg", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Internal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public int ScoreElementCandidate(ModelItem candidate)
        {
            if (candidate == null) return int.MinValue;

            int score = 0;

            try
            {
                if (candidate.HasGeometry)
                    score += 25;
            }
            catch { }

            try
            {
                var name = (candidate.DisplayName ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(name))
                    score += 5;

                if (IsWrapperNode(name))
                    score -= 40;

                if (IsNoLevelNode(name))
                    score -= 10;
            }
            catch { }

            try
            {
                var guid = candidate.InstanceGuid;
                if (guid != Guid.Empty)
                    score += 10;
            }
            catch { }

            return score;
        }
    }
}