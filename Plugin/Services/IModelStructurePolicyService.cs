using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin.Services
{
    public interface IModelStructurePolicyService
    {
        bool IsWrapperNode(string displayName);
        bool IsIgnorableContainer(string displayName);
        bool IsNoLevelNode(string displayName);
        bool LooksLikeLevelNode(string displayName);
        bool LooksLikeLinkedContainer(string displayName);
        int ScoreElementCandidate(ModelItem candidate);
    }
}