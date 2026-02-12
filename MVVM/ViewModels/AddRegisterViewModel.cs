using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    public partial class AddRegisterViewModel : BaseViewModel
    {
        private readonly IUserDialogService _dialogService = App.ServiceProvider.GetRequiredService<IUserDialogService>();
        private readonly IHealthRecordService _healthRecordService = App.ServiceProvider.GetRequiredService<IHealthRecordService>();

        [ObservableProperty] private HealthRecord record = new();

        public ICommand RegisterCommand => new Command(async () => await Save());
        public ICommand GoBack => new Command(async () => await Application.Current.MainPage.Navigation.PopAsync());

        [ObservableProperty] private bool isLoading;

        public void SetRecord(HealthRecord record) => Record = record;

        private async Task Save()
        {
            try
            {
                var existing = await _healthRecordService.GetRecordBySusAsync(Record.SusNumber);

                if (existing != null)
                    await _healthRecordService.UpdateRecordAsync(Record);
                else
                    await _healthRecordService.AddRecordAsync(Record);

                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowError(ex.Message);
            }
        }
    }
}