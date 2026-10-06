namespace ACS_View.Application.Interfaces;

public interface ILocalBiometricVault
{
    Task<bool> IsAvailableAsync();
    Task<bool> HasEnrollmentAsync();
    Task ProtectAsync(string payload);
    Task<string> UnlockAsync();
    Task RemoveAsync();
}

public sealed record BiometricStatus(bool Available, bool Enabled);
