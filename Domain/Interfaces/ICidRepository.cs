using ACS_View.Domain.Entities.Health;

namespace ACS_View.Domain.Interfaces
{
    internal interface ICidRepository
    {
        Task<bool> AnyAsync();

        Task<CidSubcategory?> GetSubcategoryByCodeAsync(string code);

        Task<List<CidCategory>> GetCategoriesByGroupAsync(string groupCode);

        Task<List<CidSubcategory>> GetSubcategoriesByCategoryAsync(string categoryCode);

        Task InsertChaptersAsync(IEnumerable<CidChapter> chapters);
        Task InsertGroupsAsync(IEnumerable<CidGroup> groups);
        Task InsertCategoriesAsync(IEnumerable<CidCategory> categories);
        Task InsertSubcategoriesAsync(IEnumerable<CidSubcategory> subcategories);
    }
}