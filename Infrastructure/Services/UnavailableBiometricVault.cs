using ACS_View.Application.Interfaces;

namespace ACS_View.Infrastructure.Services;

internal sealed class UnavailableBiometricVault : ILocalBiometricVault
{
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);
    public Task<bool> HasEnrollmentAsync() => Task.FromResult(false);
    public Task ProtectAsync(string payload) => throw new InvalidOperationException("Biometria indisponível nesta plataforma. Use a senha da conta.");
    public Task<string> UnlockAsync() => throw new InvalidOperationException("Biometria indisponível nesta plataforma. Use a senha da conta.");
    public Task RemoveAsync() => Task.CompletedTask;
}
