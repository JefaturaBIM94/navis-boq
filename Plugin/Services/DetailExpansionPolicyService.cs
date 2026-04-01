using System;
using NavisBOQ.Plugin.Constants;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class DetailExpansionPolicyService : IDetailExpansionPolicyService
    {
        public DetailExpansionRequest NormalizeRequest(DetailExpansionRequest request)
        {
            request ??= new DetailExpansionRequest();

            if (request.Options == null)
                request.Options = new RunOptions();

            if (string.IsNullOrWhiteSpace(request.RunName))
                request.RunName = "run_preconstruccion_4";

            if (string.IsNullOrWhiteSpace(request.DetailProfile))
                request.DetailProfile = DetailProfileNames.TypeLight;

            if (request.MaxItems <= 0)
                request.MaxItems = 100;

            switch (request.DetailProfile.Trim().ToLowerInvariant())
            {
                case DetailProfileNames.ElectricalFull:
                    request.MaxItems = Math.Min(request.MaxItems, 100);
                    request.IncludeFamilyFields = true;
                    request.IncludeTypeFields = true;
                    request.IncludeInstanceFields = true;
                    break;

                case DetailProfileNames.TubeFocus:
                    request.MaxItems = Math.Min(request.MaxItems, 250);
                    request.IncludeFamilyFields = false;
                    request.IncludeTypeFields = true;
                    request.IncludeInstanceFields = true;
                    break;

                case DetailProfileNames.FixtureFocus:
                    request.MaxItems = Math.Min(request.MaxItems, 150);
                    request.IncludeFamilyFields = true;
                    request.IncludeTypeFields = true;
                    request.IncludeInstanceFields = true;
                    break;

                case DetailProfileNames.TypeLight:
                default:
                    request.DetailProfile = DetailProfileNames.TypeLight;
                    request.MaxItems = Math.Min(request.MaxItems, 500);
                    request.IncludeFamilyFields = true;
                    request.IncludeTypeFields = true;
                    request.IncludeInstanceFields = false;
                    break;
            }

            return request;
        }
    }
}