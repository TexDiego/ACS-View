namespace ACS_View.Application.Interfaces;

public enum WindowsHelloAvailability { Available, NotConfiguredForUser, DeviceNotPresent, DisabledByPolicy, DeviceBusy, RetriesExhausted, Unavailable, PackageRequired }
public sealed record WindowsHelloStatus(WindowsHelloAvailability Availability, bool Enabled)
{
    public string Message => Enabled && Availability == WindowsHelloAvailability.Available ? "Ativado para esta conta." : Availability switch
    {
        WindowsHelloAvailability.Available => "Disponível e desativado.",
        WindowsHelloAvailability.NotConfiguredForUser => "Windows Hello não configurado. Configure um PIN nas opções de entrada do Windows para habilitar o acesso rápido.",
        WindowsHelloAvailability.DisabledByPolicy => "Windows Hello bloqueado por política do sistema.",
        WindowsHelloAvailability.DeviceBusy => "Windows Hello ocupado. Tente novamente ou use a senha.",
        WindowsHelloAvailability.RetriesExhausted => "Tentativas do Windows Hello esgotadas. Use a senha.",
        WindowsHelloAvailability.PackageRequired => "Windows Hello requer a instalação empacotada (MSIX) do ACS View. Nesta versão portátil, use a senha.",
        _ => "Windows Hello indisponível neste dispositivo. Use a senha."
    };
}

// Native boundary: never returns a reusable login token. Public keys use X.509 SPKI.
public interface IWindowsHelloService
{
    Task<WindowsHelloAvailability> GetAvailabilityAsync();
    Task<byte[]> CreateAsync(string credentialName);
    Task<byte[]> GetPublicKeyAsync(string credentialName);
    Task<byte[]> SignAsync(string credentialName, byte[] challenge);
    Task DeleteAsync(string credentialName);
}
