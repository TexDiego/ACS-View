using ACS_View.Application.Interfaces;
using System.Runtime.InteropServices;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Credentials.UI;

namespace ACS_View.Platforms.Windows;

// KeyCredential is the documented packaged WinUI 3 API. Unlike UserConsentVerifier,
// RequestSignAsync proves possession of this application's account-specific key.
internal sealed class WindowsHelloService : IWindowsHelloService
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint length, System.Text.StringBuilder? name);
    public async Task<WindowsHelloAvailability> GetAvailabilityAsync()
    {
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (availability != UserConsentVerifierAvailability.Available) return availability switch
            {
                UserConsentVerifierAvailability.NotConfiguredForUser => WindowsHelloAvailability.NotConfiguredForUser,
                UserConsentVerifierAvailability.DeviceNotPresent => WindowsHelloAvailability.DeviceNotPresent,
                UserConsentVerifierAvailability.DisabledByPolicy => WindowsHelloAvailability.DisabledByPolicy,
                UserConsentVerifierAvailability.DeviceBusy => WindowsHelloAvailability.DeviceBusy,
                _ => WindowsHelloAvailability.Unavailable
            };
            uint length = 0;
            if (GetCurrentPackageFullName(ref length, null) == 15700) return WindowsHelloAvailability.PackageRequired;
            return await KeyCredentialManager.IsSupportedAsync() ? WindowsHelloAvailability.Available : WindowsHelloAvailability.NotConfiguredForUser;
        }
        catch { return WindowsHelloAvailability.Unavailable; }
    }
    public Task<byte[]> CreateAsync(string credentialName) => OnWindowThreadAsync(async () =>
    {
        await EnsureAvailableAsync();
        var result = await KeyCredentialManager.RequestCreateAsync(credentialName, KeyCredentialCreationOption.FailIfExists);
        EnsureSuccess(result.Status);
        return PublicKey(result.Credential);
    });
    public async Task<byte[]> GetPublicKeyAsync(string credentialName)
    {
        var result = await KeyCredentialManager.OpenAsync(credentialName);
        EnsureSuccess(result.Status);
        return PublicKey(result.Credential);
    }
    public Task<byte[]> SignAsync(string credentialName, byte[] challenge) => OnWindowThreadAsync(async () =>
    {
        await EnsureAvailableAsync();
        var opened = await KeyCredentialManager.OpenAsync(credentialName);
        EnsureSuccess(opened.Status);
        // Every RequestSignAsync requires Hello verification; opening a key does not authorize login.
        var result = await opened.Credential.RequestSignAsync(CryptographicBuffer.CreateFromByteArray(challenge));
        EnsureSuccess(result.Status);
        CryptographicBuffer.CopyToByteArray(result.Result, out var signature);
        return signature;
    });
    public async Task DeleteAsync(string credentialName) => await KeyCredentialManager.DeleteAsync(credentialName);
    private static byte[] PublicKey(KeyCredential credential)
    {
        CryptographicBuffer.CopyToByteArray(credential.RetrievePublicKey(CryptographicPublicKeyBlobType.X509SubjectPublicKeyInfo), out var key);
        return key;
    }
    private async Task EnsureAvailableAsync()
    {
        var status = await GetAvailabilityAsync();
        if (status != WindowsHelloAvailability.Available) throw new InvalidOperationException(new WindowsHelloStatus(status, false).Message);
    }
    private static void EnsureSuccess(KeyCredentialStatus status)
    {
        if (status == KeyCredentialStatus.Success) return;
        if (status == KeyCredentialStatus.UserCanceled) throw new OperationCanceledException();
        throw new InvalidOperationException(status switch
        {
            KeyCredentialStatus.NotFound => "Credencial Windows Hello removida. Entre com a senha e ative-a novamente.",
            KeyCredentialStatus.UserPrefersPassword => "Use a senha do ACS View para continuar.",
            KeyCredentialStatus.SecurityDeviceLocked => "Tentativas do Windows Hello esgotadas ou dispositivo bloqueado. Use a senha.",
            _ => "Não foi possível utilizar a credencial Windows Hello. Use a senha."
        });
    }
    private static Task<byte[]> OnWindowThreadAsync(Func<Task<byte[]>> action) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window is null || WinRT.Interop.WindowNative.GetWindowHandle(window) == IntPtr.Zero)
            throw new OperationCanceledException("A janela foi fechada.");
        // KeyCredential exposes no HWND overload. Use the supported packaged model;
        // do not replace it with a generic consent dialog or UWP-only window interop.
        window.Activate();
        var result = await action();
        // Closing/replacing the owning MAUI window while the native prompt is open
        // must not finish an authentication in a detached view.
        if (!Microsoft.Maui.Controls.Application.Current!.Windows.Any(w => ReferenceEquals(w.Handler?.PlatformView, window)))
            throw new OperationCanceledException("A janela foi fechada.");
        return result;
    });
}
