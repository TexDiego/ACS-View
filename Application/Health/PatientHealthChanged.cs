namespace ACS_View.Application.Health;
public sealed record PatientHealthChanged(int PatientId, int OwnerId, string Module);
