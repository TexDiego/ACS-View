using ACS_View.Domain.Entities;
using ACS_View.Domain.Interfaces;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class AddRegisterViewModel(IUserDialogService _dialogService, IPatientService _patientService) : BaseViewModel
    {
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private Patient currentPatient = new();
        //[ObservableProperty] private ObservableCollection<ConditionCategoryVM> categories = [];

        public DateTime MinimumDate => DateTime.Today.AddYears(-120);
        public DateTime MaximumDate => DateTime.Today;

        public ICommand RegisterCommand => new Command(async () => await Save());
        public ICommand GoBack => new Command(async () => await Shell.Current.GoToAsync(".."));
        //public ICommand OpenCategories => new Command<ConditionCategoryVM>(async (condition) => await OpenCategoriesAsync(condition));

        internal async Task LoadPatiant(int id)
        {
            try
            {
                CurrentPatient = await _patientService.GetPatientById(id) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", "Não foi possível obter dados do paciente solicitado", "Voltar");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private async Task Save()
        {
            try
            {
                var existing = await _patientService.GetAllPatients();
                var exists = existing.Find(p => p.SusNumber == CurrentPatient.SusNumber);

                if (exists is not null)
                {
                    bool update = await Shell.Current.DisplayAlert("Aviso", "Já existe um cadastro com este número SUS. Deseja continuar para atualizar o cadastro?", "Continuar", "Cancelar");
                    if (!update) return;
                }

                if (exists is not null)
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