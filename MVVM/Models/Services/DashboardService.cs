using ACS_View.Enums;
using ACS_View.MVVM.Models.DTOs;
using ACS_View.MVVM.Models.HealthConditions;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using SQLite;
using System.Diagnostics;

namespace ACS_View.MVVM.Models.Services
{
    internal class DashboardService : IDashboardService
    {
        private readonly IDatabaseService _databaseService = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        private readonly SQLiteAsyncConnection _connection;

        public DashboardService()
        {
            _connection = _databaseService.Connection;
        }

        public async Task AddItemAsync(DashboardItemType type, int itemId)
        {
            var exists = await _connection.Table<DashboardItem>()
            .Where(x => x.ItemType == type && x.ItemId == itemId)
            .CountAsync() > 0;

            if (exists)
                return;

            var maxOrder = await _connection.Table<DashboardItem>()
                .OrderByDescending(x => x.DisplayOrder)
                .FirstOrDefaultAsync();

            await _connection.InsertAsync(new DashboardItem
            {
                ItemType = type,
                ItemId = itemId,
                IsVisible = true,
                DisplayOrder = (maxOrder?.DisplayOrder ?? 0) + 1
            });
        }

        public async Task<List<DashboardItemResult>> GetDashboardSummaryAsync()
        {
            var dashboardItems = await _connection.Table<DashboardItem>()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            if (dashboardItems.Count < 1)
            {
                var initial = await _connection.Table<ConditionCategory>().ToListAsync();

                foreach (var cat in initial)
                {
                    await AddItemAsync(DashboardItemType.Category, cat.Id);
                }

                dashboardItems = await _connection.Table<DashboardItem>()
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();
            }


            // Carrega tudo de uma vez
            var categories = await _connection.Table<ConditionCategory>().ToListAsync();
            var conditions = await _connection.Table<HealthConditions.Condition>().ToListAsync();

            // Totais por categoria
            var categoryTotals = await _connection.QueryAsync<CategoryTotalDto>(@"
                SELECT c.CategoryId as CategoryId,
                       COUNT(pc.Id) as Total
                FROM Condition c
                LEFT JOIN PatientCondition pc ON pc.ConditionId = c.Id
                GROUP BY c.CategoryId");

            Debug.WriteLine($"Total por categoria encontrado: [{categoryTotals.Count}]");

            var categoryDict = categoryTotals
                .ToDictionary(x => x.CategoryId, x => x.Total);

            // Totais por condição
            var conditionTotals = await _connection.QueryAsync<ConditionTotalDto>(@"
                SELECT ConditionId,
                       COUNT(Id) as Total
                FROM PatientCondition
                GROUP BY ConditionId");

            Debug.WriteLine($"Total por condição encontrado: [{conditionTotals.Count}]");

            var conditionDict = conditionTotals
                .ToDictionary(x => x.ConditionId, x => x.Total);

            var results = new List<DashboardItemResult>();

            foreach (var item in dashboardItems)
            {
                if (item.ItemType == DashboardItemType.Category)
                {
                    var category = categories.FirstOrDefault(x => x.Id == item.ItemId);

                    results.Add(new DashboardItemResult
                    {
                        Id = item.Id,
                        ItemId = item.ItemId,
                        ItemType = item.ItemType,
                        Name = category?.Name ?? "Categoria removida",
                        Total = categoryDict.TryGetValue(item.ItemId, out var total)
                            ? total
                            : 0,
                        DisplayOrder = item.DisplayOrder
                    });
                }
                else
                {
                    var condition = conditions.FirstOrDefault(x => x.Id == item.ItemId);

                    results.Add(new DashboardItemResult
                    {
                        Id = item.Id,
                        ItemId = item.ItemId,
                        ItemType = item.ItemType,
                        Name = condition?.Name ?? "Condição removida",
                        Total = conditionDict.TryGetValue(item.ItemId, out var total)
                            ? total
                            : 0,
                        DisplayOrder = item.DisplayOrder
                    });
                }
            }

            Debug.WriteLine($"Total de DashboardItemResult encontrado: [{results.Count}]");

            return results;
        }

        public async Task RemoveItemAsync(int id)
        {
            await _connection.DeleteAsync<DashboardItem>(id);
        }

        public async Task UpdateOrderAsync(List<DashboardItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].DisplayOrder = i;
                await _connection.UpdateAsync(items[i]);
            }
        }
    }
}