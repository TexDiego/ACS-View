using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Services;
using System.Collections.ObjectModel;

namespace ACS_View.MVVM.ViewModel
{
    public class HealthRecordViewModel : BaseViewModel
    {
        private readonly HealthRecordService _healthRecordService;

        public ObservableCollection<HealthRecord> Records { get; set; } = new();

        public HealthRecordViewModel() { }
        public HealthRecordViewModel(HealthRecordService healthRecordService)
        {
            _healthRecordService = healthRecordService;
            LoadRecordsCommand = new Command(async () => await LoadRecordsAsync());
            Console.WriteLine("comando adicionado");
        }

        public Command LoadRecordsCommand { get; }

        private async Task LoadRecordsAsync()
        {
            var records = await _healthRecordService.GetAllRecordsAsync();
            Records.Clear();
            foreach (var record in records)
                Records.Add(record);
        }
    }
}
