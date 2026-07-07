namespace ACS_View.Domain.Enums;

public enum CareNotificationType
{
    PossibleBirth = 1,
    PuerperiumStarted = 2,
    PregnancyVaccineSuggestion = 3,
    MissingPregnancyData = 4,
    DueDateSoon = 5,
    DueDateOverdue = 6,
    RiskAttention = 7,

    ChildVaccineOverdue = 100,
    VisitPending = 200,
    IncompleteRegistration = 300
}
