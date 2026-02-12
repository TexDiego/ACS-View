using System.Linq.Expressions;

namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IHealthRecordService
    {
        Task<List<HealthRecord>> GetAllRecordsAsync();
        Task<HealthRecord?> GetRecordBySusAsync(string sus);
        Task<List<HealthRecord>> GetRecordByNameOrSusAsync(string search);
        Task<int> SaveRecordAsync(HealthRecord record);
        Task AddRecordAsync(HealthRecord record);
        Task UpdateRecordAsync(HealthRecord record);
        Task DeleteRecordAsync(string sus);
        Task<int> GetConditionCountAsync(Expression<Func<HealthRecord, bool>> condition);
        Task<int> GetTotalCountAsync();
        Task<int> GetElderCountAsync();
        Task<int> GetYoungerCountAsync();
        Task<List<HealthRecord>> GetRecordsByFamilyAndHouseAsync(int idFamily, int idHouse);
        Task<List<HealthRecord>> GetRecordsByHouseIdAsync(int idHouse);
    }
}