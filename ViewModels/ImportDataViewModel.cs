using ACS_View.Application.Interfaces;
using ACS_View.Domain.ValueObjects;
using ACS_View.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class ImportDataViewModel(
    IPatientImportService patientImportService,
    IHouseImportService houseImportService) : BaseViewModel
{
    [ObservableProperty] private string nameColumn = "Nome";
    [ObservableProperty] private string susNumberColumn = "SUS";
    [ObservableProperty] private string motherNameColumn = "Mãe";
    [ObservableProperty] private string fatherNameColumn = "Pai";
    [ObservableProperty] private string birthDateColumn = "Data de nascimento";
    [ObservableProperty] private string observationColumn = "Observação";
    [ObservableProperty] private string bolsaFamiliaColumn = "Bolsa Família";
    [ObservableProperty] private string patientImportSummary = string.Empty;
    [ObservableProperty] private ObservableCollection<PatientImportConditionColumnDto> healthConditionColumns = new(
        HealthConditionCatalog.Conditions.Select(condition => new PatientImportConditionColumnDto
        {
            ConditionName = condition,
            ColumnName = condition
        }));

    [ObservableProperty] private string houseCepColumn = "CEP";
    [ObservableProperty] private string houseStreetColumn = "Rua";
    [ObservableProperty] private string houseNumberColumn = "Número";
    [ObservableProperty] private string houseNeighborhoodColumn = "Bairro";
    [ObservableProperty] private string houseCityColumn = "Cidade";
    [ObservableProperty] private string houseStateColumn = "Estado";
    [ObservableProperty] private string houseCountryColumn = "País";
    [ObservableProperty] private string houseComplementColumn = "Complemento";
    [ObservableProperty] private string houseHasComplementColumn = "Possui complemento";
    [ObservableProperty] private string houseImportSummary = string.Empty;

    public ICommand ImportPatientsCommand => new Command(async () => await ImportPatientsAsync());
    public ICommand ImportHousesCommand => new Command(async () => await ImportHousesAsync());

    private async Task ImportPatientsAsync()
    {
        try
        {
            await ExecuteWithRunningAsync(async () =>
            {
                var file = await PickSpreadsheetAsync("Selecionar planilha de pacientes");
                if (file is null)
                {
                    return;
                }

                await using var stream = await file.OpenReadAsync();
                var result = await patientImportService.ImportAsync(stream, new PatientImportColumnMapDto
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

                PatientImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount);

                if (result.Errors.Count > 0)
                {
                    await DisplayAlertAsync("Importação", string.Join(Environment.NewLine, result.Errors), "Voltar");
                    return;
                }

                await DisplayAlertAsync("Importação concluída", PatientImportSummary, "Voltar");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlertAsync("Erro", "Não foi possível importar a planilha de pacientes.", "Voltar");
        }
    }

    private async Task ImportHousesAsync()
    {
        try
        {
            await ExecuteWithRunningAsync(async () =>
            {
                var file = await PickSpreadsheetAsync("Selecionar planilha de residências");
                if (file is null)
                {
                    return;
                }

                await using var stream = await file.OpenReadAsync();
                var result = await houseImportService.ImportAsync(stream, new HouseImportColumnMapDto
                {
                    CepColumn = HouseCepColumn,
                    StreetColumn = HouseStreetColumn,
                    NumberColumn = HouseNumberColumn,
                    NeighborhoodColumn = HouseNeighborhoodColumn,
                    CityColumn = HouseCityColumn,
                    StateColumn = HouseStateColumn,
                    CountryColumn = HouseCountryColumn,
                    ComplementColumn = HouseComplementColumn,
                    HasComplementColumn = HouseHasComplementColumn
                });

                HouseImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount);

                if (result.Errors.Count > 0)
                {
                    await DisplayAlertAsync("Importação", string.Join(Environment.NewLine, result.Errors), "Voltar");
                    return;
                }

                await DisplayAlertAsync("Importação concluída", HouseImportSummary, "Voltar");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlertAsync("Erro", "Não foi possível importar a planilha de residências.", "Voltar");
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
}
