using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin.Services
{
    public interface INodeResolutionPolicyService
    {
        ModelItem ResolvePreferredInstanceNode(ModelItem item, string category, string categoryId = "");
        string ResolvePreferredLevel(ModelItem item, string category);
        bool IsNestedOrSubcomponent(ModelItem item, string category);
        string GetResolvedFrom(string category);
        string GetLevelSource(ModelItem item, string category);
        string GetGeometryConfidence(ModelItem item, string category);
    }
}