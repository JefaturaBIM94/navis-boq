using System.Collections.Generic;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IDetailFieldProfileService
    {
        List<PropertyFieldRequest> GetProfile(string runName, string profileName);
    }
}
