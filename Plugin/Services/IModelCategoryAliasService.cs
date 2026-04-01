namespace NavisBOQ.Plugin.Services
{
    public interface IModelCategoryAliasService
    {
        bool IsStructuralFraming(string category, string categoryId = "");
        bool IsStructuralColumn(string category, string categoryId = "");
        bool IsStructuralFoundation(string category, string categoryId = "");
        bool IsGenericModel(string category, string categoryId = "");
        string NormalizeStructuralCategory(string category, string categoryId = "");
    }
}