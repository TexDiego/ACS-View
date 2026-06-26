using ACS_View.Application.DTOs;
using ACS_View.Application.Interfaces;
using System.Text.Json;

namespace ACS_View.UseCases.Services;

internal sealed class DashboardMetricPreferencesService(ICurrentUserContext currentUserContext) : IDashboardMetricPreferencesService
{
    private const string PreferenceKeyPrefix = "DashboardMetricPreferences";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<DashboardMetricPreferencesDto> GetAsync()
    {
        var json = Preferences.Get(GetPreferenceKey(), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(new DashboardMetricPreferencesDto());
        }

        try
        {
            return Task.FromResult(Normalize(
                JsonSerializer.Deserialize<DashboardMetricPreferencesDto>(json, JsonOptions)
                ?? new DashboardMetricPreferencesDto()));
        }
        catch (JsonException)
        {
            return Task.FromResult(new DashboardMetricPreferencesDto());
        }
    }

    public Task SaveAsync(DashboardMetricPreferencesDto preferences)
    {
        Preferences.Set(GetPreferenceKey(), JsonSerializer.Serialize(preferences, JsonOptions));
        return Task.CompletedTask;
    }

    private string GetPreferenceKey()
    {
        return $"{PreferenceKeyPrefix}.{currentUserContext.RequireCurrentUserId()}";
    }

    private static DashboardMetricPreferencesDto Normalize(DashboardMetricPreferencesDto preferences)
    {
        preferences.GeneralOrder ??= [];
        preferences.HealthOrder ??= [];
        preferences.RemovedGeneralRootFilterKeys ??= [];
        preferences.RemovedHealthRootFilterKeys ??= [];
        preferences.Combinations ??= [];

        return preferences;
    }
}
