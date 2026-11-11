using ACS_View.Application.DTOs;
using ACS_View.Application.Health;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
namespace ACS_View.UseCases.Services;
internal sealed class PatientHealthDataService(ISQLiteConditionsRepository conditions,
    IPatientInsulinDependencyRepository insulin, IPregnancyService pregnancy, IGlucoseMonitoringService glucose,
    IVisitsService visits, IDatabaseService database, IPatientInfectiousFollowUpService infectious, IVaccineService vaccines) : IPatientHealthDataService
{
    public Task<PatientInfectiousFollowUp?> GetInfectiousAsync(PatientHealthSession s, string conditionCode) => s.GetAsync(conditionCode, async ct =>
    {
        var value = await infectious.GetAsync(s.PatientId, conditionCode); s.EnsureActive(); return value;
    });
    public Task<ClinicalHealthData> GetClinicalAsync(PatientHealthSession s) => s.GetAsync("clinical", async ct =>
    {
        var c = await conditions.GetConditionsByPatientIdAsync(s.PatientId); s.EnsureActive();
        var i = await insulin.GetByPatientIdAsync(s.PatientId); s.EnsureActive();
        var p = await pregnancy.GetDetailsByPatientIdAsync(s.PatientId); s.EnsureActive();
        return new ClinicalHealthData(c, i is not null, p);
    });
    public Task<DiabetesHealthData> GetDiabetesAsync(PatientHealthSession s) => s.GetAsync("diabetes", async ct =>
    {
        var m = await glucose.GetAsync(s.PatientId); s.EnsureActive();
        var h = await glucose.GetPickupsAsync(s.PatientId); s.EnsureActive();
        return new DiabetesHealthData(m, h);
    });
    public Task<PatientVaccineScheduleDto?> GetVaccinationAsync(PatientHealthSession session) =>
        session.GetAsync("vaccination", async _ =>
        {
            var schedule = await vaccines.GetScheduleForPatientAsync(session.PatientId);
            session.EnsureActive();
            return schedule;
        });

    public Task<VisitsHealthData> GetVisitsAsync(PatientHealthSession s) => s.GetAsync("visits", async ct =>
    {
        var summary = await visits.GetPatientVisitSummaryAsync(s.PatientId, DateTime.Today); s.EnsureActive();
        var records = await database.Connection.Table<Visits>().Where(v => v.UserId == s.OwnerId && v.PatientId == s.PatientId).OrderByDescending(v => v.VisitDate).ToListAsync();
        return new VisitsHealthData(summary, records);
    });
}
