namespace ACS_View.MVVM.Models.Interfaces
{
    public interface IHealthRecordFilterService
    {
        List<HealthRecord> ApplyFilters(
            IEnumerable<HealthRecord> records,
            string condition,
            string search,
            string filter,
            string order);
    }
}