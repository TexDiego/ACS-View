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
    public ICommand OpenImportHistoryCommand => new Command(async () => await Shell.Current.Navigation.PushAsync(new ACS_View.Views.ImportHistoryPage(historyService)));

    public ICommand ImportPatientsCommand => new Command(async () => await ImportPatientsAsync());
    public ICommand CancelImportCommand => new Command(CancelImport);

    private CancellationTokenSource? importCancellationTokenSource;

    private async Task ImportPatientsAsync()
    {
        if (IsImporting) return;
        IsImporting = true;
        ImportProgress = 0;
        ImportProgressText = "Selecionando arquivos…";
        CanStartImport = false;
        try
        {
            var files = await PickPatientSpreadsheetsAsync();
            if (files.Count == 0) return;

            importCancellationTokenSource?.Dispose();
            importCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = importCancellationTokenSource.Token;

            CanCancelImport = true;
            PatientImportSummary = string.Empty;
            ImportProgress = 0;
            ImportProgressText = "Preparando importação...";

            var failures = new List<string>();
            var detected = new List<(FileResult File, bool Patients)>();
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ImportProgressText = $"Identificando {file.FileName}…";
                try
                {
                    await using var input = await file.OpenReadAsync();
                    var kind = await Task.Run(() => ACS_View.UseCases.Services.ImportFileClassifier.Detect(input), cancellationToken);
                    if (kind is null) failures.Add($"{file.FileName}: colunas não reconhecidas para pacientes ou residências.");
                    else detected.Add((file, kind.Value));
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { failures.Add($"{file.FileName}: {ex.GetBaseException().Message}"); }
            }
            detected = detected.OrderBy(item => item.Patients).ToList();
            var houseTotal = new ResidenceImportSummaryDto();
            var houseErrorCount = 0;
            var total = new PatientImportResultDto();
            var readPatientIds = new HashSet<int>();
            for (var index = 0; index < detected.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (file, isPatientFile) = detected[index];
                var fileIndex = index;
                var progress = new Progress<ImportProgressDto>(value =>
                {
                    ImportProgress = (fileIndex + value.Progress) / Math.Max(1, detected.Count);
                    ImportProgressText = $"Arquivo {fileIndex + 1}/{detected.Count} · {file.FileName}: {BuildProgressText(value)} · {(isPatientFile ? FormatPatientCount(value.ProcessedItems) : "Residências")}";
                });
                try
                {
                    await using var stream = await file.OpenReadAsync();
                    if (!isPatientFile)
                    {
                        var houseMap = BuildHouseColumnMap();
                        houseMap.SourceFileName = file.FileName;
                        var houseResult = await Task.Run(() => houseImportService.ImportAsync(stream, houseMap, progress, cancellationToken), cancellationToken);
                        houseTotal.Include(houseResult);
                        houseErrorCount += houseResult.Errors.Count;
                        continue;
                    }
                    var columnMap = BuildPatientColumnMap();
                    columnMap.SourceFileName = file.FileName;
                    // Sequential persistence lets each spreadsheet complement the preceding ones,
                    // including pending family links, without racing identity creation.
                    var result = await Task.Run(() => patientImportService.ImportAsync(stream, columnMap, progress, cancellationToken), cancellationToken);
                    houseTotal.Include(result.Entries);
                    total.ImportedCount += result.ImportedCount;
                    total.UpdatedCount += result.UpdatedCount;
                    total.MergedCount += result.MergedCount;
                    total.IgnoredCount += result.IgnoredCount;
                    total.Errors.AddRange(result.Errors);
                    total.Entries.AddRange(result.Entries);
                    foreach (var entry in result.Entries)
                    {
                        if (entry.EntityType == "Paciente" && entry.RowNumber > 0 && entry.RecordId is > 0)
                        {
                            readPatientIds.Add(entry.RecordId.Value);
                        }
                    }
                    PatientImportSummary = $"{index + 1}/{detected.Count} arquivos · {FormatPatientCount(readPatientIds.Count)} · " + BuildSummary(total.ImportedCount, total.UpdatedCount, total.IgnoredCount);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { failures.Add($"{file.FileName}: {ex.GetBaseException().Message}"); }
            }
            ImportProgress = 1;
            ImportProgressText = "Processamento dos arquivos concluído.";
            PatientImportSummary = $"{detected.Count} arquivos · {FormatPatientCount(readPatientIds.Count)} · " + BuildSummary(total.ImportedCount, total.UpdatedCount, total.IgnoredCount) +
                $". Duplicidades nas planilhas: {total.DuplicateRowCount} linhas repetidas. Residências: {BuildSummary(houseTotal.ImportedCount, houseTotal.UpdatedCount, houseTotal.IgnoredCount)}. Consulte o histórico por arquivo.";
            if (total.Errors.Count + houseErrorCount > 0 || failures.Count > 0)
            {
                await DisplayAlertAsync("Importação com pendências", $"{PatientImportSummary}\nPendências: {total.Errors.Count + houseErrorCount}. Arquivos interrompidos: {failures.Count}.\n" + string.Join("\n", failures), "Voltar");
                return;
            }

            await DisplayAlertAsync("Importação concluída", PatientImportSummary, "Voltar");
        }
        catch (OperationCanceledException)
        {
            ImportProgressText = "Importação cancelada; os dados já gravados foram mantidos.";
            await DisplayAlertAsync("Importação", ImportProgressText + " Consulte o histórico por arquivo.", "Voltar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlertAsync("Erro", BuildImportErrorMessage("Não foi possível concluir a importação.", ex), "Voltar");
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

    private static HouseImportColumnMapDto BuildHouseColumnMap() => new();

    private static PatientImportColumnMapDto BuildPatientColumnMap() => new();

    private void CancelImport()
    {
        if (CanCancelImport)
        {
            importCancellationTokenSource?.Cancel();
        }
    }

    private static async Task<List<FileResult>> PickPatientSpreadsheetsAsync()
    {
        using var interaction = (Microsoft.Maui.Controls.Application.Current as ACS_View.Views.App)?.BeginExternalInteraction();
        return (await FilePicker.Default.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Selecionar planilhas de pacientes e residências",
            FileTypes = GetExcelFileTypes()
        }))?.ToList() ?? [];
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

    private static string FormatPatientCount(int count) =>
        count == 1 ? "1 paciente identificado" : $"{count} pacientes identificados";

    private static string BuildSummary(int importedCount, int updatedCount, int ignoredCount)
    {
        return $"Importados: {importedCount} | Mesclados: {updatedCount} | Ignorados: {ignoredCount}";
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
