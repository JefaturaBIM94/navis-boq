using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IDetailExpansionPolicyService
    {
        DetailExpansionRequest NormalizeRequest(DetailExpansionRequest request);
    }
}