using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using ACS_View.Domain.Entities;
using ACS_View.Domain.Enums;
using ACS_View.Domain.ValueObjects;
using SQLite;

namespace ACS_View.UseCases.Services;

internal sealed class CareNotificationService(
    IDatabaseService databaseService,
    ICurrentUserContext currentUserContext) : ICareNotificationService
{
    private readonly SQLiteAsyncConnection _connection = databaseService.Connection;

    public async Task RefreshPregnancyNotificationsAsync()
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var today = DateTime.Today;
        var generatedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pregnancies = await _connection.Table<PatientPregnancy>()
            .Where(pregnancy => pregnancy.UserId == userId)
            .ToListAsync();
        var patients = await _connection.Table<Patient>()
            .Where(patient => patient.UserId == userId)
            .ToListAsync();
        var patientById = patients.ToDictionary(patient => patient.Id);

        foreach (var pregnancy in pregnancies)
        {
            if (!patientById.TryGetValue(pregnancy.PatientId, out var patient))
            {
                continue;
            }

            if (pregnancy.Status == PregnancyStatus.Active)
            {
                if (pregnancy.LastMenstrualPeriod is null && pregnancy.ExpectedBirthDate is null)
                {
                    generatedKeys.Add(await UpsertAsync(new CareNotification
                    {
                        UserId = userId,
                        PatientId = pregnancy.PatientId,
                        Category = CareNotificationCategory.Pregnancy,
                        Type = CareNotificationType.MissingPregnancyData,
                        Priority = CareNotificationPriority.Medium,
                        Title = "Dados gestacionais incompletos",
                        Message = $"{patient.Name} não possui DUM nem DPP informadas.",
                        UniqueKey = BuildKey("pregnancy", "missing-data", pregnancy),
                        SourceRule = "pregnancy-missing-data"
                    }));
                }

                if (PregnancyCalculator.IsDueDateSoon(pregnancy, today, 7))
                {
                    var days = (pregnancy.ExpectedBirthDate!.Value.Date - today).Days;
                    generatedKeys.Add(await UpsertAsync(new CareNotification
                    {
                        UserId = userId,
                        PatientId = pregnancy.PatientId,
                        Category = CareNotificationCategory.Pregnancy,
                        Type = CareNotificationType.DueDateSoon,
                        Priority = CareNotificationPriority.Medium,
                        Title = "DPP próxima",
                        Message = $"{patient.Name} tem DPP em {days} dia(s).",
                        DueDate = pregnancy.ExpectedBirthDate,
                        UniqueKey = BuildKey("pregnancy", "due-date-soon", pregnancy),
                        SourceRule = "pregnancy-due-date-soon"
                    }));
                }

                if (PregnancyCalculator.IsDueDateOverdue(pregnancy, today))
                {
                    generatedKeys.Add(await UpsertAsync(new CareNotification
                    {
                        UserId = userId,
                        PatientId = pregnancy.PatientId,
                        Category = CareNotificationCategory.Pregnancy,
                        Type = CareNotificationType.PossibleBirth,
                        Priority = CareNotificationPriority.High,
                        Title = "Verificar possível parto",
                        Message = $"{patient.Name} tinha DPP em {pregnancy.ExpectedBirthDate:dd/MM/yyyy}. Confirme se houve parto e atualize a gestação.",
                        DueDate = pregnancy.ExpectedBirthDate,
                        UniqueKey = BuildKey("pregnancy", "possible-birth", pregnancy),
                        SourceRule = "pregnancy-possible-birth"
                    }));
                }

                var gestationalAge = PregnancyCalculator.CalculateGestationalAge(pregnancy, today);
                if (gestationalAge?.Weeks >= 20)
                {
                    generatedKeys.Add(await UpsertAsync(new CareNotification
                    {
                        UserId = userId,
                        PatientId = pregnancy.PatientId,
                        Category = CareNotificationCategory.Pregnancy,
                        Type = CareNotificationType.PregnancyVaccineSuggestion,
                        Priority = CareNotificationPriority.Low,
                        Title = "Vacina recomendada",
                        Message = $"Verifique a situação vacinal de {patient.Name} conforme o período gestacional.",
                        UniqueKey = BuildKey("pregnancy", "vaccine-suggestion", pregnancy),
                        SourceRule = "pregnancy-vaccine-suggestion"
                    }));
                }
            }

            if (PregnancyCalculator.IsPuerperal(pregnancy, today))
            {
                var postpartumDays = PregnancyCalculator.CalculatePostpartumDays(pregnancy, today) ?? 0;
                var endDate = PregnancyCalculator.CalculatePuerperiumEndDate(pregnancy);
                generatedKeys.Add(await UpsertAsync(new CareNotification
                {
                    UserId = userId,
                    PatientId = pregnancy.PatientId,
                    Category = CareNotificationCategory.Puerperium,
                    Type = CareNotificationType.PuerperiumStarted,
                    Priority = CareNotificationPriority.High,
                    Title = "Puerpério iniciado",
                    Message = $"{patient.Name} está com {postpartumDays} dia(s) pós-parto. Acompanhar puerpério até {endDate:dd/MM/yyyy}.",
                    DueDate = endDate,
                    UniqueKey = BuildKey("puerperium", "started", pregnancy),
                    SourceRule = "puerperium-started"
                }));
            }
        }

        await ResolveStalePregnancyNotificationsAsync(userId, generatedKeys);
    }

    public async Task<IReadOnlyList<CareNotificationDto>> GetActiveAsync(int take = 10)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        var today = DateTime.Today;
        var notifications = await _connection.Table<CareNotification>()
            .Where(notification =>
                notification.UserId == userId &&
                (notification.Status == CareNotificationStatus.Active ||
                 notification.Status == CareNotificationStatus.Snoozed))
            .ToListAsync();
        var active = notifications
            .Where(notification => notification.Status == CareNotificationStatus.Active ||
                                   notification.SnoozedUntil is null ||
                                   notification.SnoozedUntil.Value.Date <= today)
            .OrderByDescending(notification => notification.Priority)
            .ThenBy(notification => notification.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(notification => notification.GeneratedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToList();
        var patients = await _connection.Table<Patient>()
            .Where(patient => patient.UserId == userId)
            .ToListAsync();
        var patientById = patients.ToDictionary(patient => patient.Id);

        return active.Select(notification => new CareNotificationDto
        {
            Notification = notification,
            PatientName = notification.PatientId.HasValue &&
                          patientById.TryGetValue(notification.PatientId.Value, out var patient)
                ? patient.Name
                : string.Empty,
            PriorityText = GetPriorityText(notification.Priority),
            CategoryText = GetCategoryText(notification.Category),
            DueText = notification.DueDate is null ? string.Empty : notification.DueDate.Value.ToString("dd/MM/yyyy")
        }).ToList();
    }

    public async Task<int> CountActiveAsync()
    {
        return (await GetActiveAsync(100)).Count;
    }

    public Task ResolveAsync(int notificationId)
    {
        return ChangeStatusAsync(notificationId, CareNotificationStatus.Resolved);
    }

    public Task DismissAsync(int notificationId)
    {
        return ChangeStatusAsync(notificationId, CareNotificationStatus.Dismissed);
    }

    public async Task SnoozeAsync(int notificationId, DateTime snoozedUntil)
    {
        var notification = await GetByIdForCurrentUserAsync(notificationId);
        if (notification is null)
        {
            return;
        }

        notification.Status = CareNotificationStatus.Snoozed;
        notification.SnoozedUntil = snoozedUntil.Date;
        await _connection.UpdateAsync(notification);
    }

    private async Task<string> UpsertAsync(CareNotification notification)
    {
        var existing = await _connection.Table<CareNotification>()
            .FirstOrDefaultAsync(item => item.UserId == notification.UserId &&
                                         item.UniqueKey == notification.UniqueKey);
        if (existing is null)
        {
            notification.GeneratedAt = DateTime.UtcNow;
            await _connection.InsertAsync(notification);
            return notification.UniqueKey;
        }

        if (existing.Status is CareNotificationStatus.Resolved or CareNotificationStatus.Dismissed)
        {
            return existing.UniqueKey;
        }

        existing.Title = notification.Title;
        existing.Message = notification.Message;
        existing.Priority = notification.Priority;
        existing.DueDate = notification.DueDate;
        existing.Category = notification.Category;
        existing.Type = notification.Type;
        existing.SourceRule = notification.SourceRule;
        existing.MetadataJson = notification.MetadataJson;
        if (existing.Status == CareNotificationStatus.Snoozed &&
            existing.SnoozedUntil is not null &&
            existing.SnoozedUntil.Value.Date <= DateTime.Today)
        {
            existing.Status = CareNotificationStatus.Active;
            existing.SnoozedUntil = null;
        }

        await _connection.UpdateAsync(existing);
        return existing.UniqueKey;
    }

    private async Task ResolveStalePregnancyNotificationsAsync(int userId, HashSet<string> activeKeys)
    {
        var notifications = await _connection.Table<CareNotification>()
            .Where(notification =>
                notification.UserId == userId &&
                (notification.Category == CareNotificationCategory.Pregnancy ||
                 notification.Category == CareNotificationCategory.Puerperium) &&
                (notification.Status == CareNotificationStatus.Active ||
                 notification.Status == CareNotificationStatus.Snoozed))
            .ToListAsync();

        foreach (var notification in notifications.Where(notification => !activeKeys.Contains(notification.UniqueKey)))
        {
            notification.Status = CareNotificationStatus.Resolved;
            notification.ResolvedAt = DateTime.UtcNow;
            notification.SnoozedUntil = null;
            await _connection.UpdateAsync(notification);
        }
    }

    private async Task ChangeStatusAsync(int notificationId, CareNotificationStatus status)
    {
        var notification = await GetByIdForCurrentUserAsync(notificationId);
        if (notification is null)
        {
            return;
        }

        notification.Status = status;
        notification.SnoozedUntil = null;
        if (status == CareNotificationStatus.Resolved)
        {
            notification.ResolvedAt = DateTime.UtcNow;
        }
        else if (status == CareNotificationStatus.Dismissed)
        {
            notification.DismissedAt = DateTime.UtcNow;
        }

        await _connection.UpdateAsync(notification);
    }

    private Task<CareNotification?> GetByIdForCurrentUserAsync(int notificationId)
    {
        var userId = currentUserContext.RequireCurrentUserId();
        return _connection.Table<CareNotification>()
            .FirstOrDefaultAsync(notification => notification.Id == notificationId && notification.UserId == userId);
    }

    private static string BuildKey(string category, string rule, PatientPregnancy pregnancy)
    {
        return $"{category}:{rule}:{pregnancy.PatientId}:{pregnancy.Id}";
    }

    private static string GetPriorityText(CareNotificationPriority priority)
    {
        return priority switch
        {
            CareNotificationPriority.High => "Alta",
            CareNotificationPriority.Medium => "Média",
            CareNotificationPriority.Low => "Baixa",
            _ => priority.ToString()
        };
    }

    private static string GetCategoryText(CareNotificationCategory category)
    {
        return category switch
        {
            CareNotificationCategory.Pregnancy => "Gestação",
            CareNotificationCategory.Puerperium => "Puerpério",
            CareNotificationCategory.Vaccination => "Vacinação",
            CareNotificationCategory.Visit => "Visitas",
            CareNotificationCategory.Registration => "Cadastro",
            CareNotificationCategory.HealthCondition => "Saúde",
            CareNotificationCategory.SocialVulnerability => "Vulnerabilidade",
            _ => category.ToString()
        };
    }
}
