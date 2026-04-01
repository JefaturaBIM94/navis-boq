using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IPreconstruccion2Service
    {
        ToolEnvelope<object> Run(RunOptions options);
    }
}