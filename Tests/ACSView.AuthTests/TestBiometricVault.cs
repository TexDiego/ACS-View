using ACS_View.Application.Interfaces;

// OS crypto/biometric adapter is substituted; the actual AuthService and SQLite are exercised.
internal sealed class TestBiometricVault : ILocalBiometricVault
{
    public bool Available = true;
    public bool CancelEnrollment;
    public bool CancelUnlock;
    public string? Payload;
    public Task<bool> IsAvailableAsync() => Task.FromResult(Available);
    public Task<bool> HasEnrollmentAsync() => Task.FromResult(Payload is not null);
    public Task ProtectAsync(string payload)
    {
        if (CancelEnrollment) throw new OperationCanceledException();
        if (!Available) throw new InvalidOperationException("Unavailable");
        Payload = payload;
        return Task.CompletedTask;
    }
    public Task<string> UnlockAsync()
    {
        if (CancelUnlock) throw new OperationCanceledException();
        if (!Available || Payload is null) throw new InvalidOperationException("Unavailable");
        return Task.FromResult(Payload);
    }
    public Task RemoveAsync() { Payload = null; return Task.CompletedTask; }
}
