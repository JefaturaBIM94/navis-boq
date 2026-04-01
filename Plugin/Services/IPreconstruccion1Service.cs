using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IPreconstruccion1Service
    {
        ToolEnvelope<object> Run(RunOptions options);
    }
}
