using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
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

        public async Task<CidSubcategory?> GetSubcategoryById(int id)
        {
            return await _database.Table<CidSubcategory>()
                                  .FirstOrDefaultAsync(x => x.Id == id);
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

        public async Task<CidChapter> GetChapterBySubcategoryAsync(int subcategoryId)
        {
            const string sql = @"
                SELECT ch.*
                FROM CidChapter ch
                INNER JOIN CidGroup g ON g.ChapterCode = ch.Code
                INNER JOIN CidCategory ca ON ca.GroupCode = g.Code
                INNER JOIN CidSubcategory s ON s.CategoryCode = ca.Code
                WHERE s.Id = ?
                LIMIT 1";

            var chapters = await _database.QueryAsync<CidChapter>(sql, subcategoryId);
            return chapters.Count > 0 ? chapters[0] : new CidChapter();
        }

        public async Task<List<CidSubcategory>> GetAllSubcategories()
        {
            return await _database.Table<CidSubcategory>().ToListAsync();
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

        public Task InsertCommonConditionsAsync(List<CommonConditions> list)
        {
            // Insert list of common conditions into the database in a transaction
            return _database.RunInTransactionAsync(tran =>
            {
                foreach (var item in list)
                {
                    tran.Insert(item);
                }
            });
        }

        public async Task<List<CommonConditions>> GetCommonConditionsAsync()
        {
            return await _database.Table<CommonConditions>().ToListAsync();
        }
    }
}