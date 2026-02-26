using ACS_View.Domain.Entities.Health;
using ACS_View.Domain.Interfaces;
using SQLite;

namespace ACS_View.Infrastructure.Data.SQLite
{
    internal class SQLiteCidRepository(SQLiteAsyncConnection database) : ICidRepository
    {
        private readonly SQLiteAsyncConnection _database = database;

        public async Task<bool> AnyAsync()
        {
            return await _database.Table<CidSubcategory>().CountAsync() > 0;
        }

        public async Task<CidSubcategory?> GetSubcategoryByCodeAsync(string code)
        {
            return await _database.Table<CidSubcategory>()
                                  .FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task<List<CidCategory>> GetCategoriesByGroupAsync(string groupCode)
        {
            return await _database.Table<CidCategory>()
                                  .Where(x => x.GroupCode == groupCode)
                                  .ToListAsync();
        }

        public async Task<List<CidSubcategory>> GetSubcategoriesByCategoryAsync(string categoryCode)
        {
            return await _database.Table<CidSubcategory>()
                                  .Where(x => x.CategoryCode == categoryCode)
                                  .ToListAsync();
        }

        public async Task InsertChaptersAsync(IEnumerable<CidChapter> chapters)
        {
            await _database.RunInTransactionAsync(tran =>
            {
                foreach (var item in chapters)
                    tran.Insert(item);
            });
        }

        public async Task InsertGroupsAsync(IEnumerable<CidGroup> groups)
        {
            await _database.RunInTransactionAsync(tran =>
            {
                foreach (var item in groups)
                    tran.Insert(item);
            });
        }

        public async Task InsertCategoriesAsync(IEnumerable<CidCategory> categories)
        {
            await _database.RunInTransactionAsync(tran =>
            {
                foreach (var item in categories)
                    tran.Insert(item);
            });
        }

        public async Task InsertSubcategoriesAsync(IEnumerable<CidSubcategory> subcategories)
        {
            await _database.RunInTransactionAsync(tran =>
            {
                foreach (var item in subcategories)
                    tran.Insert(item);
            });
        }
    }
}