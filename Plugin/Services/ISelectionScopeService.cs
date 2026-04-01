using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface ISelectionScopeService
    {
        IEnumerable<ModelItem> ResolveScopeItems(RunOptions options);
        ScopePreflight BuildPreflight(RunOptions options, ExecutionBudget budget);
    }
}