using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
using ACS_View.Application.DTOs;
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
        IPatientBolsaFamiliaRepository bolsaFamiliaRepository,
        IPatientInsulinDependencyRepository insulinDependencyRepository,
        IPopupService popupService) : BaseViewModel
    {
        private readonly IPatientService _patientService = patientService;
        private readonly ICidRepository _cidRepo = cidRepo;
        private readonly IPatientCidRepository _patientCid = patientCid;
        private readonly ISQLiteConditionsRepository _conditionsRepository = conditionsRepository;
        private readonly IPatientBolsaFamiliaRepository _bolsaFamiliaRepository = bolsaFamiliaRepository;
        private readonly IPatientInsulinDependencyRepository _insulinDependencyRepository = insulinDependencyRepository;
        private readonly IPopupService _popupService = popupService;

        internal List<CidSubcategory> Subcategories = [];

        [ObservableProperty] private Patient currentPatient = new();
        [ObservableProperty] private ObservableCollection<HealthConditions> healthCategories = [];
        [ObservableProperty] private ObservableCollection<HealthConditions> commonConditions = [];
        [ObservableProperty] private string motherNameInput = string.Empty;
        [ObservableProperty] private string fatherNameInput = string.Empty;
        [ObservableProperty] private DateTime birthDateInput = DateTime.Today;
        [ObservableProperty] private bool isBolsaFamiliaSelected;
        [ObservableProperty] private string bolsaFamiliaNisInput = string.Empty;
        [ObservableProperty] private string bolsaFamiliaResponsibleNameInput = string.Empty;
        [ObservableProperty] private bool hasBolsaFamiliaResponsible;
        [ObservableProperty] private bool isInsulinDependent;

        private bool _updatingParentNameInputs;
        private bool _updatingBirthDateInput;
        private string? _linkedMotherName;
        private string? _linkedFatherName;
        private int? _bolsaFamiliaResponsiblePatientId;
        private bool _initialIsActive = true;

        public DateTime MinimumDate => new(1900, 1, 1);
        public DateTime MaximumDate => DateTime.Today;
        public IReadOnlyList<string> DiabetesTypeOptions => HealthConditionCatalog.DiabetesTypes
            .Where(type => !string.Equals(type, "Diabetes sem tipo informado", StringComparison.OrdinalIgnoreCase))
            .ToList();

        public ICommand RegisterCommand => new Command(async () => await Save());
        public ICommand GoBack => new Command(async () => await NavigateBackAsync());
        public ICommand OpenConditionsPopup => new Command(async () => await OpenCidPopup());
        public ICommand SwitchConditionCommand => new Command<HealthConditions>(async (h) => await SwitchConditionSelected(h));
        public ICommand ClearDiabetesTypeCommand => new Command<HealthConditions>(ClearDiabetesType);
        public ICommand RemoveConditionCommand => new Command<HealthConditions>(RemoveCondition);
        public ICommand SelectMotherCommand => new Command(async () => await SelectLinkedParentAsync(isMother: true));
        public ICommand SelectFatherCommand => new Command(async () => await SelectLinkedParentAsync(isMother: false));
        public ICommand SelectBolsaFamiliaResponsibleCommand => new Command(async () => await SelectBolsaFamiliaResponsibleAsync());
        public ICommand ClearBolsaFamiliaResponsibleCommand => new Command(ClearBolsaFamiliaResponsible);

        partial void OnMotherNameInputChanged(string value)
        {
            if (CurrentPatient is null)
            {
                return;
            }

            CurrentPatient.MotherName = value ?? string.Empty;
            if (_updatingParentNameInputs)
            {
                return;
            }

            if (CurrentPatient.MotherPatientId.HasValue &&
                !string.Equals(value, _linkedMotherName, StringComparison.Ordinal))
            {
                CurrentPatient.MotherPatientId = null;
                _linkedMotherName = null;
            }
        }

        partial void OnFatherNameInputChanged(string value)
        {
            if (CurrentPatient is null)
            {
                return;
            }

            CurrentPatient.FatherName = value ?? string.Empty;
            if (_updatingParentNameInputs)
            {
                return;
            }

            if (CurrentPatient.FatherPatientId.HasValue &&
                !string.Equals(value, _linkedFatherName, StringComparison.Ordinal))
            {
                CurrentPatient.FatherPatientId = null;
                _linkedFatherName = null;
            }
        }

        partial void OnBirthDateInputChanged(DateTime value)
        {
            if (_updatingBirthDateInput || CurrentPatient is null)
            {
                return;
            }

            CurrentPatient.BirthDate = CoerceBirthDate(value);
        }

        partial void OnIsBolsaFamiliaSelectedChanged(bool value)
        {
            if (value)
            {
                return;
            }

            BolsaFamiliaNisInput = string.Empty;
            ClearBolsaFamiliaResponsible();
        }

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

                NormalizePatientForEditing(p);
                CurrentPatient = p;
                _initialIsActive = p.IsActive;
                SetParentNameInputs(p);
                SetBirthDateInput(p.BirthDate);
                await LoadBolsaFamiliaAsync(p);
                await LoadInsulinDependencyAsync(p.Id);

                HealthCategories.Clear();
                CommonConditions.Clear();
                LoadFixedConditions();

                var patientConditions = await _conditionsRepository.GetConditionsByPatientIdAsync(id);
                foreach (var patientCondition in patientConditions.Where(c => !string.IsNullOrWhiteSpace(c.Description)))
                {
                    var key = HealthConditionCatalog.GetKey(patientCondition.Description);
                    if (string.Equals(key, HealthConditionCatalog.Insulinodependente, StringComparison.OrdinalIgnoreCase))
                    {
                        IsInsulinDependent = true;
                        continue;
                    }

                    var fixedCondition = CommonConditions.FirstOrDefault(c => string.Equals(c.ConditionKey, key, StringComparison.OrdinalIgnoreCase));
                    if (fixedCondition == null) continue;

                    fixedCondition.Selected = true;
                    if (string.Equals(key, HealthConditionCatalog.Diabetes, StringComparison.OrdinalIgnoreCase))
                    {
                        fixedCondition.Name = HealthConditionCatalog.Diabetes;
                        fixedCondition.SelectedDiabetesType = GetPersistedDiabetesType(patientCondition.Description);
                    }
                    else
                    {
                        fixedCondition.Name = patientCondition.Description;
                    }
                }

                if (IsInsulinDependent)
                {
                    var diabetesCondition = CommonConditions.FirstOrDefault(IsDiabetesCondition);
                    if (diabetesCondition is not null)
                    {
                        diabetesCondition.Selected = true;
                        diabetesCondition.InsulinDependentSelected = true;
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

        internal Task ShowLoadErrorAsync()
        {
            return DisplayAlertAsync("Erro", "Nao foi possivel carregar o cadastro para edicao.");
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

                    if (CurrentPatient.IsActive != _initialIsActive)
                    {
                        CurrentPatient.StatusChangedAt = DateTime.UtcNow;
                    }

                    if (!await ValidateBolsaFamiliaNisAsync())
                    {
                        return;
                    }

                    CurrentPatient.BirthDate = BirthDateInput.Date;

                    if (CurrentPatient.Id > 0)
                    {
                        await _patientService.UpdatePatient(CurrentPatient);
                    }
                    else
                    {
                        await _patientService.CreatePatient(CurrentPatient);

                    }

                    _initialIsActive = CurrentPatient.IsActive;

                    await _patientCid.DeletePatientCIDByPatientId(CurrentPatient.Id);
                    await _conditionsRepository.DeleteConditionsByPatientIdAsync(CurrentPatient.Id);

                    var selectedConditions = CommonConditions.Where(condition => condition.Selected).ToList();
                    var diabetesCondition = selectedConditions.FirstOrDefault(IsDiabetesCondition);
                    await SyncBolsaFamiliaAsync();
                    await SyncInsulinDependencyAsync(diabetesCondition?.InsulinDependentSelected == true);

                    foreach (var condition in selectedConditions)
                    {
                        await _conditionsRepository.InsertConditionAsync(new PatientConditions
                        {
                            PatientId = CurrentPatient.Id,
                            Description = GetPersistedConditionDescription(condition)
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

        private async Task LoadBolsaFamiliaAsync(Patient patient)
        {
            var benefit = await _bolsaFamiliaRepository.GetByPatientIdAsync(patient.Id);
            if (benefit is null)
            {
                IsBolsaFamiliaSelected = false;
                BolsaFamiliaNisInput = string.Empty;
                ClearBolsaFamiliaResponsible();
                return;
            }

            IsBolsaFamiliaSelected = true;
            BolsaFamiliaNisInput = benefit.NisNumber ?? string.Empty;

            if (benefit.ResponsiblePatientId > 0 && benefit.ResponsiblePatientId != patient.Id)
            {
                var responsible = await _patientService.GetPatientById(benefit.ResponsiblePatientId);
                _bolsaFamiliaResponsiblePatientId = responsible?.Id;
                BolsaFamiliaResponsibleNameInput = responsible?.Name ?? string.Empty;
                HasBolsaFamiliaResponsible = responsible is not null;
                return;
            }

            ClearBolsaFamiliaResponsible();
        }

        private async Task SyncBolsaFamiliaAsync()
        {
            if (!IsBolsaFamiliaSelected)
            {
                await _bolsaFamiliaRepository.DeleteByPatientIdAsync(CurrentPatient.Id);
                return;
            }

            await _bolsaFamiliaRepository.UpsertAsync(new PatientBolsaFamilia
            {
                PatientId = CurrentPatient.Id,
                ResponsiblePatientId = _bolsaFamiliaResponsiblePatientId ?? CurrentPatient.Id,
                NisNumber = NisNumberRules.Normalize(BolsaFamiliaNisInput)
            });
        }

        private async Task<bool> ValidateBolsaFamiliaNisAsync()
        {
            if (!IsBolsaFamiliaSelected)
            {
                BolsaFamiliaNisInput = string.Empty;
                return true;
            }

            var normalizedNis = NisNumberRules.Normalize(BolsaFamiliaNisInput);
            BolsaFamiliaNisInput = normalizedNis;

            if (string.IsNullOrWhiteSpace(normalizedNis))
            {
                return true;
            }

            if (NisNumberRules.IsValid(normalizedNis))
            {
                return true;
            }

            await DisplayAlertAsync("Aviso", "O numero NIS informado e invalido.");
            return false;
        }

        private async Task LoadInsulinDependencyAsync(int patientId)
        {
            IsInsulinDependent = await _insulinDependencyRepository.GetByPatientIdAsync(patientId) is not null;
        }

        private async Task SyncInsulinDependencyAsync(bool shouldPersist)
        {
            if (!shouldPersist)
            {
                await _insulinDependencyRepository.DeleteByPatientIdAsync(CurrentPatient.Id);
                IsInsulinDependent = false;
                return;
            }

            await _insulinDependencyRepository.UpsertAsync(CurrentPatient.Id);
            IsInsulinDependent = true;
        }

        private async Task SelectLinkedParentAsync(bool isMother)
        {
            var popup = new ParentPatientPickerPopup(
                _patientService,
                CurrentPatient?.Id > 0 ? CurrentPatient.Id : null,
                isMother ? "Selecionar mãe" : "Selecionar pai");

            var popupResult = await _popupService.ShowAsync<PatientListItemDto>(popup);
            if (popupResult.WasDismissed || popupResult.Result is not PatientListItemDto selectedPatient)
            {
                return;
            }

            _updatingParentNameInputs = true;
            try
            {
                if (isMother)
                {
                    CurrentPatient.MotherPatientId = selectedPatient.Id;
                    _linkedMotherName = selectedPatient.Name;
                    MotherNameInput = selectedPatient.Name;
                }
                else
                {
                    CurrentPatient.FatherPatientId = selectedPatient.Id;
                    _linkedFatherName = selectedPatient.Name;
                    FatherNameInput = selectedPatient.Name;
                }
            }
            finally
            {
                _updatingParentNameInputs = false;
            }
        }

        private async Task SelectBolsaFamiliaResponsibleAsync()
        {
            var popup = new ParentPatientPickerPopup(
                _patientService,
                CurrentPatient?.Id > 0 ? CurrentPatient.Id : null,
                "Selecionar responsavel");

            var popupResult = await _popupService.ShowAsync<PatientListItemDto>(popup);
            if (popupResult.WasDismissed || popupResult.Result is not PatientListItemDto selectedPatient)
            {
                return;
            }

            _bolsaFamiliaResponsiblePatientId = selectedPatient.Id;
            BolsaFamiliaResponsibleNameInput = selectedPatient.Name;
            HasBolsaFamiliaResponsible = true;
        }

        private void ClearBolsaFamiliaResponsible()
        {
            _bolsaFamiliaResponsiblePatientId = null;
            BolsaFamiliaResponsibleNameInput = string.Empty;
            HasBolsaFamiliaResponsible = false;
        }

        private void SetParentNameInputs(Patient patient)
        {
            _updatingParentNameInputs = true;
            try
            {
                _linkedMotherName = patient.MotherPatientId.HasValue ? patient.MotherName : null;
                _linkedFatherName = patient.FatherPatientId.HasValue ? patient.FatherName : null;
                MotherNameInput = patient.MotherName ?? string.Empty;
                FatherNameInput = patient.FatherName ?? string.Empty;
            }
            finally
            {
                _updatingParentNameInputs = false;
            }
        }

        private void SetBirthDateInput(DateTime birthDate)
        {
            _updatingBirthDateInput = true;
            try
            {
                BirthDateInput = CoerceBirthDate(birthDate);
            }
            finally
            {
                _updatingBirthDateInput = false;
            }
        }

        private void NormalizePatientForEditing(Patient patient)
        {
            patient.Name ??= string.Empty;
            patient.SusNumber ??= string.Empty;
            patient.MotherName ??= string.Empty;
            patient.FatherName ??= string.Empty;
            patient.Observacao ??= string.Empty;
            patient.StatusReason ??= string.Empty;

            if (string.IsNullOrWhiteSpace(patient.Sexo))
            {
                patient.Sexo = "Indeterminado";
            }
        }

        private DateTime CoerceBirthDate(DateTime birthDate)
        {
            var date = birthDate.Date;
            if (date < MinimumDate.Date || date > MaximumDate.Date)
            {
                return DateTime.Today;
            }

            return date;
        }

        private async Task SwitchConditionSelected(HealthConditions condition)
        {
            if (condition == null) return;

            if (!condition.Selected)
            {
                condition.Name = condition.ConditionKey;
                if (condition.ConditionKey == HealthConditionCatalog.Diabetes)
                {
                    IsInsulinDependent = false;
                    condition.SelectedDiabetesType = string.Empty;
                    condition.InsulinDependentSelected = false;
                }
            }

            await Task.CompletedTask;
        }

        private static bool IsDiabetesCondition(HealthConditions condition)
        {
            return string.Equals(condition.ConditionKey, HealthConditionCatalog.Diabetes, StringComparison.OrdinalIgnoreCase);
        }

        private static void ClearDiabetesType(HealthConditions condition)
        {
            if (condition is not null)
            {
                condition.SelectedDiabetesType = string.Empty;
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
                if (string.Equals(condition, HealthConditionCatalog.BolsaFamilia, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CommonConditions.Add(new HealthConditions
                {
                    Name = condition,
                    ConditionKey = condition
                });
            }
        }

        private static string GetPersistedConditionDescription(HealthConditions condition)
        {
            if (!IsDiabetesCondition(condition))
            {
                return condition.Name;
            }

            return string.IsNullOrWhiteSpace(condition.SelectedDiabetesType)
                ? HealthConditionCatalog.Diabetes
                : condition.SelectedDiabetesType.Trim();
        }

        private static string GetPersistedDiabetesType(string description)
        {
            if (string.IsNullOrWhiteSpace(description) ||
                string.Equals(description, HealthConditionCatalog.Diabetes, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(description, "Diabetes sem tipo informado", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return HealthConditionCatalog.DiabetesTypes.FirstOrDefault(type =>
                string.Equals(type, description, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }
    }
}
