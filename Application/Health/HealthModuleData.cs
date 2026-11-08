using ACS_View.Application.DTOs;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Entities.Health;
namespace ACS_View.Application.Health;
public sealed record PatientHealthContext(int PatientId, int OwnerId, string Name);
public sealed record HealthModuleOption(string Key, string Title);
public sealed record ClinicalHealthData(IReadOnlyList<PatientConditions> Conditions, bool Insulin, PregnancyDetailsDto? Pregnancy);
public sealed record DiabetesHealthData(PatientGlucoseMonitoring? Monitoring, IReadOnlyList<PatientStripPickup> Pickups);
public sealed record VisitsHealthData(PatientVisitSummaryDto Summary, IReadOnlyList<Visits> Records);

