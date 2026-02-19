using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.HealthConditions;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class AddRegisterViewModel : BaseViewModel
    {
        private readonly IUserDialogService _dialogService = App.ServiceProvider.GetRequiredService<IUserDialogService>();
        private readonly IDatabaseService _db = App.ServiceProvider.GetRequiredService<IDatabaseService>();
        private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();

        [ObservableProperty] private Patient currentPatient = new();
        [ObservableProperty] private ObservableCollection<ConditionCategoryVM> categories = [];

        public AddRegisterViewModel()
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadConditions());
        }

        public ICommand RegisterCommand => new Command(async () => await Save());
        public ICommand GoBack => new Command(async () => await Shell.Current.GoToAsync(".."));
        public ICommand OpenCategories => new Command<ConditionCategoryVM>(async (condition) => await OpenCategoriesAsync(condition));


        [ObservableProperty] private bool isLoading;

        public async Task LoadConditions(int? patientId = null)
        {
            var categories = await _db.Connection.Table<ConditionCategory>().ToListAsync();
            var conditions = await _db.Connection.Table<Models.HealthConditions.Condition>().ToListAsync();

            List<int> selectedIds = [];

            if (patientId is not null)
            {
                CurrentPatient = await _patientService.GetPatientById(patientId.Value);

                var patientConditions = await _db.Connection.Table<PatientCondition>()
                    .Where(pc => pc.PatientId == patientId.Value)
                    .ToListAsync();

                selectedIds = [.. patientConditions.Select(pc => pc.ConditionId)];
            }

            Categories = new ObservableCollection<ConditionCategoryVM>(
                categories.Select(cat => new ConditionCategoryVM
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    Conditions = new ObservableCollection<ConditionVM>(
                        conditions
                            .Where(c => c.CategoryId == cat.Id)
                            .Select(c => new ConditionVM
                            {
                                Id = c.Id,
                                Name = c.Name,
                                IsSelected = selectedIds.Contains(c.Id)
                            }))
                }));

            foreach (var c in Categories)
                Debug.WriteLine(c.Id);
        }

        private async Task OpenCategoriesAsync(ConditionCategoryVM condition)
        {
            await Shell.Current.ShowPopupAsync(new ConditionsPopup(condition));
        }

        private async Task Save()
        {
            try
            {
                var existing = await _patientService.GetPatientById(CurrentPatient.Id);

                if (existing is not null)
                    await _patientService.UpdatePatient(CurrentPatient);
                else
                    await _patientService.CreatePatient(CurrentPatient);

                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowError(ex.Message);
            }
        }
    }
}