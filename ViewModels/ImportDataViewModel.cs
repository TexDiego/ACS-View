using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class ImportDataViewModel(
    IPatientImportService patientImportService,
    IHouseImportService houseImportService) : BaseViewModel
{
    [ObservableProperty] private string nameColumn = "CADASTRO NOME";
    [ObservableProperty] private string susNumberColumn = "CADASTRO CNS";
    [ObservableProperty] private string motherNameColumn = "NOME DA MÃE";
    [ObservableProperty] private string fatherNameColumn = "NOME DO PAI";
    [ObservableProperty] private string sexColumn = "SEXO";
    [ObservableProperty] private string birthDateColumn = "CADASTRO DN";
    [ObservableProperty] private string observationColumn = "OBSERVACAO";
    [ObservableProperty] private string bolsaFamiliaColumn = "BOLSA FAMILIA";
    [ObservableProperty] private string patientCepColumn = "CEP";
    [ObservableProperty] private string patientStreetTypeColumn = "TIPO LOGRADOURO";
    [ObservableProperty] private string patientStreetColumn = "LOGRADOURO";
    [ObservableProperty] private string patientHouseNumberColumn = "NUMERO IMOVEL";
    [ObservableProperty] private string patientNeighborhoodColumn = "BAIRRO";
    [ObservableProperty] private string patientCityColumn = "CIDADE";
    [ObservableProperty] private string patientStateColumn = "ESTADO";
    [ObservableProperty] private string patientComplementColumn = "COMPLEMENTO";
    [ObservableProperty] private string isFamilyResponsibleColumn = "RESP. FAMILIAR?";
    [ObservableProperty] private string familyResponsibleSusColumn = "RESP. FAMILIAR CNS";
    [ObservableProperty] private string patientImportSummary = string.Empty;
    [ObservableProperty] private bool isImporting;
    [ObservableProperty] private bool canStartImport = true;
    [ObservableProperty] private bool canCancelImport;
    [ObservableProperty] private double importProgress;
    [ObservableProperty] private string importProgressText = string.Empty;
    [ObservableProperty] private ObservableCollection<PatientImportConditionColumnDto> healthConditionColumns = new(
        HealthConditionCatalog.Conditions.Select(condition => new PatientImportConditionColumnDto
        {
            ConditionName = condition,
            ColumnName = condition
        }));

    [ObservableProperty] private string houseCepColumn = "CEP";
    [ObservableProperty] private string houseStreetTypeColumn = "TIPO LOGRADOURO";
    [ObservableProperty] private string houseStreetColumn = "LOGRADOURO";
    [ObservableProperty] private string houseNumberColumn = "NUMERO IMOVEL";
    [ObservableProperty] private string houseNeighborhoodColumn = "BAIRRO";
    [ObservableProperty] private string houseCityColumn = "CIDADE";
    [ObservableProperty] private string houseStateColumn = "ESTADO";
    [ObservableProperty] private string houseCountryColumn = "PAIS";
    [ObservableProperty] private string houseComplementColumn = "COMPLEMENTO";
    [ObservableProperty] private string houseImportSummary = string.Empty;
    [ObservableProperty] private bool isHouseImporting;
    [ObservableProperty] private bool canStartHouseImport = true;
    [ObservableProperty] private bool canCancelHouseImport;
    [ObservableProperty] private double houseImportProgress;
    [ObservableProperty] private string houseImportProgressText = string.Empty;

    public ICommand ImportPatientsCommand => new Command(async () => await ImportPatientsAsync());
    public ICommand ImportHousesCommand => new Command(async () => await ImportHousesAsync());
    public ICommand CancelImportCommand => new Command(CancelImport);
    public ICommand CancelHouseImportCommand => new Command(CancelHouseImport);

    private CancellationTokenSource? importCancellationTokenSource;
    private CancellationTokenSource? houseImportCancellationTokenSource;

    private async Task ImportPatientsAsync()
    {
        try
        {
            var file = await PickSpreadsheetAsync("Selecionar planilha de pacientes");
            if (file is null)
            {
                return;
            }

            importCancellationTokenSource?.Dispose();
            importCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = importCancellationTokenSource.Token;

            IsImporting = true;
            CanStartImport = false;
            CanCancelImport = true;
            ImportProgress = 0;
            ImportProgressText = "Preparando importação...";

            var progress = new Progress<ImportProgressDto>(value =>
            {
                ImportProgress = value.Progress;
                ImportProgressText = string.IsNullOrWhiteSpace(value.CurrentStep)
                    ? $"{value.ProcessedItems}/{value.TotalItems}"
                    : $"{value.CurrentStep} ({value.ProcessedItems}/{value.TotalItems})";
            });

            await using var stream = await file.OpenReadAsync();
            var columnMap = BuildPatientColumnMap();
            var result = await Task.Run(
                () => patientImportService.ImportAsync(stream, columnMap, progress, cancellationToken),
                cancellationToken);

            PatientImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount);

            if (result.Errors.Count > 0)
            {
                await DisplayAlertAsync("Importação", string.Join(Environment.NewLine, result.Errors), "Voltar");
                return;
            }

            await DisplayAlertAsync("Importação concluída", PatientImportSummary, "Voltar");
        }
        catch (OperationCanceledException)
        {
            await DisplayAlertAsync("Importação", "Importação cancelada.", "Voltar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlertAsync("Erro", BuildImportErrorMessage("Não foi possível importar a planilha de pacientes.", ex), "Voltar");
        }
        finally
        {
            IsImporting = false;
            CanStartImport = true;
            CanCancelImport = false;
            importCancellationTokenSource?.Dispose();
            importCancellationTokenSource = null;
        }
    }

    private async Task ImportHousesAsync()
    {
        try
        {
            var file = await PickSpreadsheetAsync("Selecionar planilha de residências");
            if (file is null)
            {
                return;
            }

            houseImportCancellationTokenSource?.Dispose();
            houseImportCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = houseImportCancellationTokenSource.Token;

            IsHouseImporting = true;
            CanStartHouseImport = false;
            CanCancelHouseImport = true;
            HouseImportProgress = 0;
            HouseImportProgressText = "Preparando importação...";

            var progress = new Progress<ImportProgressDto>(value =>
            {
                HouseImportProgress = value.Progress;
                HouseImportProgressText = string.IsNullOrWhiteSpace(value.CurrentStep)
                    ? $"{value.ProcessedItems}/{value.TotalItems}"
                    : $"{value.CurrentStep} ({value.ProcessedItems}/{value.TotalItems})";
            });

            await using var stream = await file.OpenReadAsync();
            var columnMap = BuildHouseColumnMap();
            var result = await Task.Run(
                () => houseImportService.ImportAsync(stream, columnMap, progress, cancellationToken),
                cancellationToken);

            HouseImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount);

            if (result.Errors.Count > 0)
            {
                await DisplayAlertAsync("Importação", string.Join(Environment.NewLine, result.Errors), "Voltar");
                return;
            }

            await DisplayAlertAsync("Importação concluída", HouseImportSummary, "Voltar");
        }
        catch (OperationCanceledException)
        {
            await DisplayAlertAsync("Importação", "Importação cancelada.", "Voltar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlertAsync("Erro", BuildImportErrorMessage("Não foi possível importar a planilha de residencias.", ex), "Voltar");
        }
        finally
        {
            IsHouseImporting = false;
            CanStartHouseImport = true;
            CanCancelHouseImport = false;
            houseImportCancellationTokenSource?.Dispose();
            houseImportCancellationTokenSource = null;
        }
    }

    private HouseImportColumnMapDto BuildHouseColumnMap()
    {
        return new HouseImportColumnMapDto
        {
            CepColumn = HouseCepColumn,
            StreetTypeColumn = HouseStreetTypeColumn,
            StreetColumn = HouseStreetColumn,
            NumberColumn = HouseNumberColumn,
            NeighborhoodColumn = HouseNeighborhoodColumn,
            CityColumn = HouseCityColumn,
            StateColumn = HouseStateColumn,
            CountryColumn = HouseCountryColumn,
            ComplementColumn = HouseComplementColumn
        };
    }

    private PatientImportColumnMapDto BuildPatientColumnMap()
    {
        return new PatientImportColumnMapDto
        {
            NameColumn = NameColumn,
            SusNumberColumn = SusNumberColumn,
            MotherNameColumn = MotherNameColumn,
            FatherNameColumn = FatherNameColumn,
            SexColumn = SexColumn,
            BirthDateColumn = BirthDateColumn,
            ObservationColumn = ObservationColumn,
            BolsaFamiliaColumn = BolsaFamiliaColumn,
            PatientCepColumn = PatientCepColumn,
            PatientStreetTypeColumn = PatientStreetTypeColumn,
            PatientStreetColumn = PatientStreetColumn,
            PatientHouseNumberColumn = PatientHouseNumberColumn,
            PatientNeighborhoodColumn = PatientNeighborhoodColumn,
            PatientCityColumn = PatientCityColumn,
            PatientStateColumn = PatientStateColumn,
            PatientComplementColumn = PatientComplementColumn,
            IsFamilyResponsibleColumn = IsFamilyResponsibleColumn,
            FamilyResponsibleSusColumn = FamilyResponsibleSusColumn,
            HealthConditionColumns = HealthConditionColumns
                .Select(condition => new PatientImportConditionColumnDto
                {
                    ConditionName = condition.ConditionName,
                    ColumnName = condition.ColumnName
                })
                .ToList()
        };
    }

    private void CancelImport()
    {
        if (CanCancelImport)
        {
            importCancellationTokenSource?.Cancel();
        }
    }

    private void CancelHouseImport()
    {
        if (CanCancelHouseImport)
        {
            houseImportCancellationTokenSource?.Cancel();
        }
    }

    private static Task<FileResult?> PickSpreadsheetAsync(string title)
    {
        return FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = GetExcelFileTypes()
        });
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

    private static string BuildSummary(int importedCount, int updatedCount, int ignoredCount)
    {
        return $"Importados: {importedCount} | Atualizados: {updatedCount} | Ignorados: {ignoredCount}";
    }

    private static string BuildImportErrorMessage(string baseMessage, Exception exception)
    {
        var reason = exception.GetBaseException().Message;
        return string.IsNullOrWhiteSpace(reason)
            ? baseMessage
            : $"{baseMessage}\n\nMotivo: {reason}";
    }
}
