using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface ISteelWeightService
    {
        bool IsSteelCandidate(ElementSnapshot snapshot);
        SteelRow BuildSteelRow(ElementSnapshot snapshot);
    }
}