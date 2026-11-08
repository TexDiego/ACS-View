using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using ACS_View.ViewModels.HealthViewModels;

internal static class HealthModuleTests
{
    public static async Task Run()
    {
        var checks = 0;
        void Check(bool value, string text) { if(!value) throw new Exception(text); checks++; Console.WriteLine("PASS: " + text); }
        var editor = new GlucoseMonitoringEditorViewModel();
        Check(editor.Build().ChecksPerDay is null && !editor.HasValues, "Health editor: optional empty form preserves unknowns");
        editor.PickupInterval = "Quinzenal"; editor.ChecksPerDay = "4"; editor.StripsPerPickup = "60";
        var routine = editor.Build(); Check(routine.PickupIntervalDays == 15 && routine.ChecksPerDay == 4, "Health editor: declared frequency normalized to days");
        editor.Load(routine); Check(editor.PickupInterval == "Quinzenal" && editor.ChecksPerDay == "4", "Health editor: persisted form round trip");
        editor.ChecksPerDay = "-1"; var invalid = false; try { editor.Build(); } catch(ArgumentException) { invalid = true; }
        Check(invalid, "Health editor: invalid input rejected before patient save");
        using var session = new PatientHealthSession(7, 1, () => true);
        var data = new Data();
        var preload = await data.GetDiabetesAsync(session);
        using var vm = new DiabetesViewModel(session, data, new Glucose(), "Diabetes");
        await vm.LoadAsync(); await vm.LoadAsync();
        Check(data.DiabetesReads == 1 && vm.Pickups.Count == 5 && vm.Estimate.Contains("120"), "Health module: cached preload and compact pickup history");
        vm.ShowAllHistory = true; Check(vm.Pickups.Count == 7, "Health module: expanded history retains every pickup");
        using var visits = new VisitsViewModel(session, data); await visits.LoadAsync();
        Check(visits.Months.Count == 2 && visits.Months[0].Details.Contains("Realizada") && visits.Months[0].Counts.Contains("Ausentes: 1"), "Health visits: monthly status totals and original records retained");
        Check(visits.NextVisit == "15/11/2026" && visits.LastVisit == "20/10/2026" && visits.Indicators.Single().Count == "1 / 2"
            && visits.Indicators.Single().Progress == .5 && visits.Indicators.Single().Explanation.Contains("Synthetic source"),
            "Health visits: dates, indicator progress and original criteria projected separately");
        Check(!visits.ShowCriteria && !visits.ShowHistoryRecords && visits.SelectedMonth?.Month == "10/2026", "Health visits: compact view starts with newest month and collapsed details");
        visits.ToggleCriteriaCommand.Execute(null); visits.ToggleHistoryRecordsCommand.Execute(null);
        visits.SelectedMonth = visits.Months[1];
        Check(visits.ShowCriteria && !visits.ShowHistoryRecords && visits.SelectedMonth.Details.Contains("Recusada"), "Health visits: month navigation retains history and collapses previous records");
        await visits.LoadAsync();
        Check(visits.SelectedMonth?.Month == "09/2026" && data.VisitReads == 1, "Health visits: refresh preserves selected month and reuses cached data");
        using var pregnancySession = new PatientHealthSession(10, 1, () => true);
        using var pregnancy = new PregnancyHealthViewModel(pregnancySession, data);
        await pregnancy.LoadAsync();
        Check(pregnancy.HasNoData && !pregnancy.ShowRisk && !pregnancy.ShowRecords, "Pregnancy: missing data keeps a compact empty state");
        data.Pregnancy = new PregnancyDetailsDto
        {
            Pregnancy = new PatientPregnancy { ManualRisk = PregnancyRisk.HighRisk, Notes = "Preserved notes" },
            GestationalAge = new GestationalAge(24, 2), Trimester = 2
        };
        pregnancySession.Invalidate("clinical"); await pregnancy.LoadAsync();
        Check(pregnancy.Age == "24 semanas e 2 dias" && pregnancy.ReferenceDate == "Não informada" && pregnancy.ManualRisk == "Alto risco",
            "Pregnancy: summary preserves age and manual risk without inventing a date");
        pregnancy.ToggleRiskCommand.Execute(null); pregnancy.ToggleRecordsCommand.Execute(null);
        Check(pregnancy.ShowRisk && pregnancy.ShowRecords && pregnancy.Notes == "Preserved notes" && pregnancy.RecordedDetails.Contains("Não informado"),
            "Pregnancy: collapsed sections retain notes and unspecified child count");
        using var failing = new PatientHealthSession(8, 1, () => true); data.Fail = true;
        using var retry = new DiabetesViewModel(failing, data, new Glucose(), "Diabetes"); await retry.LoadAsync();
        Check(retry.HasError && !retry.IsLoading, "Health module: individual error exposes retry state");
        data.Fail = false; await retry.RetryCommand.ExecuteAsync(null);
        Check(!retry.HasError && retry.Pickups.Count == 5, "Health module: retry clears failure and loads data");
        using var closing = new PatientHealthSession(9, 1, () => true); data.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = new DiabetesViewModel(closing, data, new Glucose(), "Diabetes");
        var pending = disposed.LoadAsync(); disposed.Dispose(); var updates = 0; disposed.PropertyChanged += (_,_) => updates++;
        data.Pause.SetResult(); await pending;
        Check(updates == 0, "Health module: disposed view model receives no late binding updates");
        Console.WriteLine($"{checks} health module and editor checks passed.");
    }
    private sealed class Glucose : IGlucoseMonitoringService
    {
        public Task<PatientGlucoseMonitoring?> GetAsync(int id) => throw new NotSupportedException();
        public Task<List<PatientStripPickup>> GetPickupsAsync(int id) => throw new NotSupportedException();
        public Task SaveAsync(int id, PatientGlucoseMonitoring m) => throw new NotSupportedException();
        public Task AddPickupAsync(int id, DateTime date, int quantity, string notes) => throw new NotSupportedException();
    }
    private sealed class Data : IPatientHealthDataService
    {
        public PregnancyDetailsDto? Pregnancy;
        public int DiabetesReads; public int VisitReads; public bool Fail; public TaskCompletionSource? Pause;
        public Task<PatientInfectiousFollowUp?> GetInfectiousAsync(PatientHealthSession s, string code) => s.GetAsync<PatientInfectiousFollowUp?>(code, _ => Task.FromResult<PatientInfectiousFollowUp?>(null));
        public Task<ClinicalHealthData> GetClinicalAsync(PatientHealthSession s) => s.GetAsync("clinical", _ => Task.FromResult(new ClinicalHealthData([], false, Pregnancy)));
        public Task<DiabetesHealthData> GetDiabetesAsync(PatientHealthSession s) => s.GetAsync("diabetes", async ct =>
        {
            DiabetesReads++; if(Pause is not null) await Pause.Task;
            if(Fail) throw new IOException("Synthetic failure");
            return new DiabetesHealthData(new PatientGlucoseMonitoring { ChecksPerDay = 4 }, Enumerable.Range(1,7).Select(i => new PatientStripPickup { Quantity = i, PickupDate = DateTime.Today.AddDays(-i) }).ToList());
        });
        public Task<VisitsHealthData> GetVisitsAsync(PatientHealthSession s) => s.GetAsync("visits", _ =>
        {
            VisitReads++;
            return Task.FromResult(new VisitsHealthData(new PatientVisitSummaryDto
            {
                ReferenceMonth = new DateTime(2026,11,1), LastCompletedVisit = new DateTime(2026,10,20),
                Evaluations = [new VisitRuleEvaluation(VisitCareLineType.Diabetes, 2, 1, new DateTime(2026,11,15),
                    new DateTime(2026,12,1), VisitFollowUpStatus.Recommended, "One completed in the reference period", "Synthetic source", false)]
            }, [
                new Visits { VisitDate = new DateTime(2026,10,20), Status = "Completed", Notes = "Original" },
                new Visits { VisitDate = new DateTime(2026,10,25), Status = "Absent" },
                new Visits { VisitDate = new DateTime(2026,9,20), Status = "Refused" }
            ]));
        });
    }
}
