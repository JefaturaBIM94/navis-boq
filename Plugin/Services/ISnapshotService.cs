using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface ISnapshotService
    {
        ElementSnapshot TryBuildSnapshot(ModelItem item);
    }
}