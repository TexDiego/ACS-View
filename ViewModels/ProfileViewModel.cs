using ACS_View.Domain.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.UseCases.DTOs;
using ACS_View.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IPatientImportService _patientImportService;

        [ObservableProperty] private string nameColumn = "Nome";
        [ObservableProperty] private string susNumberColumn = "SUS";
        [ObservableProperty] private string motherNameColumn = "Mãe";
        [ObservableProperty] private string fatherNameColumn = "Pai";
        [ObservableProperty] private string birthDateColumn = "Data de nascimento";
        [ObservableProperty] private string observationColumn = "Observação";
        [ObservableProperty] private string bolsaFamiliaColumn = "Bolsa Família";
        [ObservableProperty] private string importSummary = string.Empty;
        [ObservableProperty] private ObservableCollection<PatientImportConditionColumnDto> healthConditionColumns = [];

        public ProfileViewModel(IPatientImportService patientImportService)
        {
            _patientImportService = patientImportService;
            LoadConditionColumns();
        }

        public ICommand OpenNotesPage => new Command(async () => await NavigateToAsync("notes"));
        public ICommand ImportPatientsCommand => new Command(async () => await ImportPatientsAsync());
        public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

        private async Task LogoutAsync()
        {
            var confirm = await DisplayConfirmationAsync(
                "Sair da conta",
                "Deseja encerrar a sessão atual?",
                "Sair");

            if (!confirm)
            {
                return;
            }

            Preferences.Remove("AuthToken");

            if (Application.Current is App app)
            {
                await app.ResetToLoginShellAsync();
            }
        }

        private async Task ImportPatientsAsync()
        {
            try
            {
                await ExecuteWithRunningAsync(async () =>
                {
                    var file = await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Selecionar planilha de pacientes",
                        FileTypes = GetExcelFileTypes()
                    });

                    if (file is null)
                    {
                        return;
                    }

                    await using var stream = await file.OpenReadAsync();
                    var result = await _patientImportService.ImportAsync(stream, new PatientImportColumnMapDto
                    {
                        NameColumn = NameColumn,
                        SusNumberColumn = SusNumberColumn,
                        MotherNameColumn = MotherNameColumn,
                        FatherNameColumn = FatherNameColumn,
                        BirthDateColumn = BirthDateColumn,
                        ObservationColumn = ObservationColumn,
                        BolsaFamiliaColumn = BolsaFamiliaColumn,
                        HealthConditionColumns = HealthConditionColumns
                            .Select(condition => new PatientImportConditionColumnDto
                            {
                                ConditionName = condition.ConditionName,
                                ColumnName = condition.ColumnName
                            })
                            .ToList()
                    });

                    ImportSummary = $"Importados: {result.ImportedCount} | Atualizados: {result.UpdatedCount} | Ignorados: {result.IgnoredCount}";

                    if (result.Errors.Count > 0)
                    {
                        await DisplayAlertAsync("Importação", string.Join(Environment.NewLine, result.Errors), "Voltar");
                        return;
                    }

                    await DisplayAlertAsync("Importação concluída", ImportSummary, "Voltar");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlertAsync("Erro", "Não foi possível importar a planilha selecionada.", "Voltar");
            }
        }

        private static FilePickerFileType GetExcelFileTypes()
        {
            return new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel.sheet.macroEnabled.12"] },
                { DevicePlatform.iOS, ["org.openxmlformats.spreadsheetml.sheet"] },
                { DevicePlatform.WinUI, [".xlsx", ".xlsm", ".xlns"] },
                { DevicePlatform.macOS, ["org.openxmlformats.spreadsheetml.sheet"] }
            });
        }

        private void LoadConditionColumns()
        {
            HealthConditionColumns = new ObservableCollection<PatientImportConditionColumnDto>(
                HealthConditionCatalog.Conditions.Select(condition => new PatientImportConditionColumnDto
                {
                    ConditionName = condition,
                    ColumnName = condition
                }));
        }
    }
}
