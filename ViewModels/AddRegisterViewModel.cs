using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    internal partial class AddRegisterViewModel(
        IPatientService patientService,
        ICidRepository cidRepo,
        IPatientCidRepository patientCid,
        ISQLiteConditionsRepository conditionsRepository,
        IPopupService popupService) : BaseViewModel
    {
        private readonly IPatientService _patientService = patientService;
        private readonly ICidRepository _cidRepo = cidRepo;
        private readonly IPatientCidRepository _patientCid = patientCid;
        private readonly ISQLiteConditionsRepository _conditionsRepository = conditionsRepository;
        private readonly IPopupService _popupService = popupService;

        internal List<CidSubcategory> Subcategories = [];

        [ObservableProperty] private Patient currentPatient = new();
        [ObservableProperty] private ObservableCollection<HealthConditions> healthCategories = [];
        [ObservableProperty] private ObservableCollection<HealthConditions> commonConditions = [];

        public DateTime MinimumDate => DateTime.Today.AddYears(-120);
        public DateTime MaximumDate => DateTime.Today;

        public ICommand RegisterCommand => new Command(async () => await Save());
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());
        public ICommand OpenConditionsPopup => new Command(async () => await OpenCidPopup());
        public ICommand SwitchConditionCommand => new Command<HealthConditions>(async (h) => await SwitchConditionSelected(h));
        public ICommand RemoveConditionCommand => new Command<HealthConditions>(RemoveCondition);

        internal async Task LoadPatient(int id)
        {
            try
            {
                var p = await _patientService.GetPatientById(id);
                if (p == null)
                {
                    await DisplayAlertAsync("Erro", "Paciente não encontrado.", "Voltar");
                    return;
                }

                CurrentPatient = p;

                HealthCategories.Clear();
                CommonConditions.Clear();
                LoadFixedConditions();

                var patientConditions = await _conditionsRepository.GetConditionsByPatientIdAsync(id);
                foreach (var patientCondition in patientConditions.Where(c => !string.IsNullOrWhiteSpace(c.Description)))
                {
                    var key = HealthConditionCatalog.GetKey(patientCondition.Description);
                    var fixedCondition = CommonConditions.FirstOrDefault(c => string.Equals(c.ConditionKey, key, StringComparison.OrdinalIgnoreCase));
                    if (fixedCondition == null) continue;

                    fixedCondition.Selected = true;
                    fixedCondition.Name = patientCondition.Description;
                }

                if (CurrentPatient.BolsaFamilia)
                {
                    var bolsaFamilia = CommonConditions.FirstOrDefault(c => c.ConditionKey == HealthConditionCatalog.BolsaFamilia);
                    if (bolsaFamilia != null)
                    {
                        bolsaFamilia.Selected = true;
                    }
                }

                var patientCids = await _patientCid.GetPatientCIDsByPatientId(id);

                if (patientCids == null) return;

                foreach (var patientCid in patientCids)
                {
                    var subcategory = Subcategories.FirstOrDefault(s => s.Id == patientCid.CidId);
                    if (subcategory == null) continue;

                    HealthCategories.Add(new HealthConditions
                    {
                        Name = subcategory.Description,
                        CID = subcategory.Code,
                        IsCid = true,
                        Selected = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlertAsync("Erro", "Não foi possível obter dados do paciente solicitado", "Voltar");
            }
        }

        internal async Task LoadSubcategories()
        {
            try
            {
                Subcategories = await _cidRepo.GetAllSubcategories();
                LoadFixedConditions();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlertAsync("Erro", "Não foi possível obter as subcategorias de CID", "Voltar");
            }
        }

        private async Task Save()
        {
            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    if (string.IsNullOrWhiteSpace(CurrentPatient?.Name) || string.IsNullOrWhiteSpace(CurrentPatient?.SusNumber))
                    {
                        await DisplayAlertAsync("Aviso", "Nome e SUS do paciente são obrigatórios.");
                        return;
                    }

                    var allPatients = await _patientService.GetAllPatients() ?? [];

                    var duplicate = allPatients.FirstOrDefault(p => !string.IsNullOrWhiteSpace(CurrentPatient.SusNumber) && p.SusNumber == CurrentPatient.SusNumber && p.Id != CurrentPatient.Id);
                    if (duplicate is not null)
                    {
                        bool update = await DisplayConfirmationAsync("Aviso", "Já existe um cadastro com este número SUS. Deseja continuar para atualizar o cadastro?", "Continuar");
                        if (!update) return;
                    }

                    if (CurrentPatient.Id > 0)
                    {
                        await _patientService.UpdatePatient(CurrentPatient);
                    }
                    else
                    {
                        await _patientService.CreatePatient(CurrentPatient);

                    }

                    await _patientCid.DeletePatientCIDByPatientId(CurrentPatient.Id);
                    await _conditionsRepository.DeleteConditionsByPatientIdAsync(CurrentPatient.Id);

                    var selectedConditions = CommonConditions.Where(condition => condition.Selected).ToList();
                    CurrentPatient.BolsaFamilia = selectedConditions.Any(condition => condition.ConditionKey == HealthConditionCatalog.BolsaFamilia);
                    await _patientService.UpdatePatient(CurrentPatient);

                    foreach (var condition in selectedConditions)
                    {
                        await _conditionsRepository.InsertConditionAsync(new PatientConditions
                        {
                            PatientId = CurrentPatient.Id,
                            Description = condition.Name
                        });
                    }

                    foreach (var cid in HealthCategories)
                    {
                        var cidId = !string.IsNullOrWhiteSpace(cid.CID)
                            ? Subcategories.FirstOrDefault(s => string.Equals(s.Code, cid.CID, StringComparison.OrdinalIgnoreCase))?.Id
                            : Subcategories.FirstOrDefault(s => string.Equals(s.Description, cid.Name, StringComparison.OrdinalIgnoreCase))?.Id;

                        await _patientCid.CreatePatientCID(new PatientCID
                        {
                            PatientId = CurrentPatient.Id,
                            CidId = cidId ?? throw new Exception($"CID não encontrado para condição: {cid.Name}")
                        });
                    }

                    await NavigateBackAsync();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlertAsync("Erro", ex.Message, "Voltar");
            }
        }

        private async Task OpenCidPopup()
        {
            var popup = new ConditionsPopup(_cidRepo);

            var popupResult = await _popupService.ShowAsync<object>(
                popup,
                new Dictionary<string, object> { { "record", HealthCategories.ToList() } });

            if (popupResult.WasDismissed) return;

            if (popupResult.Result is List<HealthConditions> list)
            {
                Debug.WriteLine($"Received {list.Count} conditions from popup", "[OpenCidPopup DEBUG]");

                HealthCategories.Clear();

                foreach (HealthConditions cid in list) Debug.WriteLine($"CID name: [{cid.Name}]", "[OpenCidPopup DEBUG]");

                foreach (HealthConditions cid in list) HealthCategories.Add(cid);

                OnPropertyChanged(nameof(HealthCategories));
            }
        }

        private async Task SwitchConditionSelected(HealthConditions condition)
        {
            if (condition == null) return;

            if (condition.ConditionKey == HealthConditionCatalog.Diabetes)
            {
                await HandleDiabetesSelectionAsync(condition);
            }

            if (!condition.Selected)
            {
                condition.Name = condition.ConditionKey;
            }
        }

        private void RemoveCondition(HealthConditions condition)
        {
            if (condition == null) return;

            var hc = HealthCategories.FirstOrDefault(h => string.Equals(h.Name, condition.Name, StringComparison.OrdinalIgnoreCase));
            if (hc != null)
                HealthCategories.Remove(hc);

            OnPropertyChanged(nameof(HealthCategories));
        }

        private void LoadFixedConditions()
        {
            if (CommonConditions is { Count: > 0 }) return;

            foreach (var condition in HealthConditionCatalog.Conditions)
            {
                CommonConditions.Add(new HealthConditions
                {
                    Name = condition,
                    ConditionKey = condition
                });
            }
        }

        private async Task HandleDiabetesSelectionAsync(HealthConditions condition)
        {
            if (!condition.Selected)
            {
                condition.Name = HealthConditionCatalog.Diabetes;
                return;
            }

            var selectedType = await DisplayActionSheetAsync(
                "Tipo de diabetes",
                "Cancelar",
                null,
                [.. HealthConditionCatalog.DiabetesTypes]);

            if (string.IsNullOrWhiteSpace(selectedType) || selectedType == "Cancelar")
            {
                condition.Selected = false;
                condition.Name = HealthConditionCatalog.Diabetes;
                return;
            }

            condition.Name = selectedType;
        }
    }
}
