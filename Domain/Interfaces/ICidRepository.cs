using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.ValueObjects;

namespace ACS_View.Domain.Interfaces
{
    public interface ICidRepository
    {
        Task<bool> AnyAsync();

        Task<CidSubcategory?> GetSubcategoryByCodeAsync(string code);

        Task<CidSubcategory?> GetSubcategoryById(int id);

        Task<List<CidCategory>> GetCategoriesByGroupAsync(string groupCode);

        Task<List<CidSubcategory>> GetSubcategoriesByCategoryAsync(string categoryCode);

        Task<CidChapter> GetChapterBySubcategoryAsync(int subcategoryId);

        Task<List<CidSubcategory>> GetAllSubcategories();

        Task InsertChaptersAsync(IEnumerable<CidChapter> chapters);
        Task InsertGroupsAsync(IEnumerable<CidGroup> groups);
        Task InsertCategoriesAsync(IEnumerable<CidCategory> categories);
        Task InsertSubcategoriesAsync(IEnumerable<CidSubcategory> subcategories);

        Task InsertCommonConditionsAsync(List<CommonConditions> list);

        Task<List<CommonConditions>> GetCommonConditionsAsync();
    }
}