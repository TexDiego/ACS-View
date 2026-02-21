using ACS_View.MVVM.Models;
using ACS_View.MVVM.Models.HealthConditions;
using ACS_View.MVVM.Models.Interfaces;
using ACS_View.MVVM.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.MVVM.ViewModels
{
    internal partial class AddRegisterViewModel(IDatabaseService _db) : BaseViewModel
    {
        private readonly SQLiteAsyncConnection _connection = _db.Connection;
        private readonly IUserDialogService _dialogService = App.ServiceProvider.GetRequiredService<IUserDialogService>();
        private readonly IPatientService _patientService = App.ServiceProvider.GetRequiredService<IPatientService>();
        
        [ObservableProperty] private Patient currentPatient = new();
        [ObservableProperty] private ObservableCollection<ConditionCategoryVM> categories = [];

        public DateTime MinimumDate => DateTime.Today.AddYears(-120);
        public DateTime MaximumDate => DateTime.Today;

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

                List<int> conditionIds = [];

                foreach (var category in Categories)
                {
                    foreach (var condition in category.Conditions.Where(s => s.IsSelected))
                    {
                        conditionIds.Add(condition.Id);
                    }
                }

                await SavePatientConditionsAsync(CurrentPatient.Id, conditionIds);

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

        // O paciente está sendo salvo com todas as condições possíveis, não só as selecionadas
        public async Task SavePatientConditionsAsync(int patientId, List<int> selectedConditionIds)
        {
            var existing = await _connection.Table<PatientCondition>()
                .Where(pc => pc.PatientId == patientId)
                .ToListAsync();

            var existingIds = existing.Select(x => x.ConditionId).ToList();

            // Condições a remover
            var toRemove = existing
                .Where(x => !selectedConditionIds.Contains(x.ConditionId))
                .ToList();

            // Condições a adicionar
            var toAdd = selectedConditionIds
                .Where(id => !existingIds.Contains(id))
                .ToList();

            // Remove
            foreach (var item in toRemove)
                await _connection.DeleteAsync(item);

            // Adiciona
            foreach (var id in toAdd)
            {
                await _connection.InsertAsync(new PatientCondition
                {
                    PatientId = patientId,
                    ConditionId = id
                });
            }
        }
    }
}