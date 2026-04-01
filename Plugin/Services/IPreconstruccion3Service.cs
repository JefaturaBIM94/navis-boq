using System.Collections.Generic;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IPreconstruccion3Service
    {
        ToolEnvelope<object> Run(RunOptions options);
    }
}