using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ACS_View.ViewModels;

public partial class ImportDataViewModel(
    IPatientImportService patientImportService,
    IHouseImportService houseImportService,
    IImportHistoryService historyService) : BaseViewModel
{
    [ObservableProperty] private string patientImportSummary = string.Empty;
    [ObservableProperty] private bool isImporting;
    [ObservableProperty] private bool canStartImport = true;
    [ObservableProperty] private bool canCancelImport;
    [ObservableProperty] private double importProgress;
    [ObservableProperty] private string importProgressText = string.Empty;
    [ObservableProperty] private string houseImportSummary = string.Empty;
    [ObservableProperty] private bool isHouseImporting;
    [ObservableProperty] private bool canStartHouseImport = true;
    [ObservableProperty] private bool canCancelHouseImport;
    [ObservableProperty] private double houseImportProgress;
    [ObservableProperty] private string houseImportProgressText = string.Empty;

    public ICommand OpenImportHistoryCommand => new Command(async () => await Shell.Current.Navigation.PushAsync(new ACS_View.Views.ImportHistoryPage(historyService)));

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
                ImportProgressText = BuildProgressText(value);
            });

            await using var stream = await file.OpenReadAsync();
            var columnMap = BuildPatientColumnMap();
            columnMap.SourceFileName = file.FileName;
            var result = await Task.Run(
                () => patientImportService.ImportAsync(stream, columnMap, progress, cancellationToken),
                cancellationToken);

            PatientImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount) + $" | Mesclados: {result.MergedCount}. Consulte o histórico para detalhes.";

            if (result.Errors.Count > 0)
            {
                await DisplayAlertAsync("Importação", $"{PatientImportSummary}\nPendências: {result.Errors.Count}. Abra o histórico para conferir cada linha.", "Voltar");
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
                HouseImportProgressText = BuildProgressText(value);
            });

            await using var stream = await file.OpenReadAsync();
            var columnMap = BuildHouseColumnMap();
            columnMap.SourceFileName = file.FileName;
            var result = await Task.Run(
                () => houseImportService.ImportAsync(stream, columnMap, progress, cancellationToken),
                cancellationToken);

            HouseImportSummary = BuildSummary(result.ImportedCount, result.UpdatedCount, result.IgnoredCount);

            if (result.Errors.Count > 0)
            {
                await DisplayAlertAsync("Importação", $"{HouseImportSummary}\nPendências: {result.Errors.Count}. Abra o histórico para conferir cada linha.", "Voltar");
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

    private static HouseImportColumnMapDto BuildHouseColumnMap() => new();

    private static PatientImportColumnMapDto BuildPatientColumnMap() => new();

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

    private static async Task<FileResult?> PickSpreadsheetAsync(string title)
    {
        // Opening the system picker is part of this screen's workflow, not an app exit.
        using var interaction = (Microsoft.Maui.Controls.Application.Current as ACS_View.Views.App)
            ?.BeginExternalInteraction();
        return await FilePicker.Default.PickAsync(new PickOptions
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
            { DevicePlatform.WinUI, [".xlsx", ".xlsm"] },
            { DevicePlatform.macOS, ["org.openxmlformats.spreadsheetml.sheet"] }
        });
    }

    private static string BuildSummary(int importedCount, int updatedCount, int ignoredCount)
    {
        return $"Importados: {importedCount} | Atualizados: {updatedCount} | Ignorados: {ignoredCount}";
    }

    private static string BuildProgressText(ImportProgressDto progress)
    {
        var percentage = Math.Clamp(progress.Progress, 0, 1) * 100;
        var percentageText = $"{Math.Round(percentage):0}%";

        return string.IsNullOrWhiteSpace(progress.CurrentStep)
            ? percentageText
            : $"{progress.CurrentStep} ({percentageText})";
    }

    private static string BuildImportErrorMessage(string baseMessage, Exception exception)
    {
        var reason = exception.GetBaseException().Message;
        return string.IsNullOrWhiteSpace(reason)
            ? baseMessage
            : $"{baseMessage}\n\nMotivo: {reason}";
    }
}
