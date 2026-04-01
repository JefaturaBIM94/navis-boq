using System;
using System.Linq;
using NavisBOQ.Plugin.Constants;

namespace NavisBOQ.Plugin.Services
{
    public class ElectricalCategoryClassifierService : IElectricalCategoryClassifierService
    {
        public bool TryClassify(string revitCategory, out string boqCategory, out string unit)
        {
            boqCategory = "";
            unit = "";

            if (string.IsNullOrWhiteSpace(revitCategory))
                return false;

            string category = revitCategory.Trim();

            if (ElectricalCategoryConstants.StrictlyForbiddenPlumbingCategories.Contains(category))
                return false;

            var match = ElectricalCategoryConstants.BoqMap
                .FirstOrDefault(x => x.Value.RevitCategories.Any(c =>
                    string.Equals(c, category, StringComparison.OrdinalIgnoreCase)));

            if (string.IsNullOrWhiteSpace(match.Key))
                return false;

            boqCategory = match.Value.CanonicalName;
            unit = match.Value.Unit;
            return true;
        }
    }
}