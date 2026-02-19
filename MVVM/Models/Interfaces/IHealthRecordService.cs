using System.Linq.Expressions;

namespace ACS_View.MVVM.Models.Interfaces
{
    internal interface IHealthRecordService
    {
        Task<List<Patient>> GetAllRecordsAsync();
        Task<Patient?> GetRecordBySusAsync(string sus);
        Task<List<Patient>> GetRecordByNameOrSusAsync(string search);
        Task<int> SaveRecordAsync(Patient record);
        Task AddRecordAsync(Patient record);
        Task UpdateRecordAsync(Patient record);
        Task DeleteRecordAsync(string sus);
        Task<int> GetConditionCountAsync(Expression<Func<Patient, bool>> condition);
        Task<int> GetTotalCountAsync();
        Task<int> GetElderCountAsync();
        Task<int> GetYoungerCountAsync();
        Task<List<Patient>> GetRecordsByFamilyAndHouseAsync(int idFamily, int idHouse);
        Task<List<Patient>> GetRecordsByHouseIdAsync(int idHouse);
    }
}