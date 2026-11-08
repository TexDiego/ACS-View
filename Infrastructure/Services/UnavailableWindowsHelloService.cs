using ACS_View.Application.Interfaces;
namespace ACS_View.Infrastructure.Services;
internal sealed class UnavailableWindowsHelloService : IWindowsHelloService
{
    public Task<WindowsHelloAvailability> GetAvailabilityAsync() => Task.FromResult(WindowsHelloAvailability.Unavailable);
    public Task<byte[]> CreateAsync(string name) => throw new InvalidOperationException("Windows Hello indisponível. Use a senha.");
    public Task<byte[]> GetPublicKeyAsync(string name) => throw new InvalidOperationException("Credencial indisponível. Use a senha.");
    public Task<byte[]> SignAsync(string name, byte[] challenge) => throw new InvalidOperationException("Windows Hello indisponível. Use a senha.");
    public Task DeleteAsync(string name) => Task.CompletedTask;
}
