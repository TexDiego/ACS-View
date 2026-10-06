using ACS_View.Application.Interfaces;
using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Microsoft.Maui.Storage;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using OperationCanceledException = System.OperationCanceledException;

namespace ACS_View.Platforms.Android;

internal sealed class AndroidBiometricVault(ISecureStorage storage) : ILocalBiometricVault
{
    private const string Alias = "ACSView.BiometricKey.v1";
    private const string CiphertextKey = "BiometricCiphertextV1";
    private sealed record Envelope(string Iv, string Ciphertext);

    public Task<bool> IsAvailableAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30)) return Task.FromResult(false);
        var manager = global::Android.App.Application.Context.GetSystemService(Context.BiometricService) as BiometricManager;
        return Task.FromResult(manager?.CanAuthenticate((int)BiometricManagerAuthenticators.BiometricStrong) == BiometricCode.Success);
    }

    public async Task<bool> HasEnrollmentAsync() => !string.IsNullOrEmpty(await storage.GetAsync(CiphertextKey));

    public async Task ProtectAsync(string payload)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30) || !await IsAvailableAsync()) throw Unavailable();
        await RemoveAsync();
        try
        {
            CreateKey();
            using var cipher = CreateCipher();
            cipher.Init(CipherMode.EncryptMode, GetKey());
            await AuthorizeAsync(cipher, "Ativar biometria");
            var encrypted = cipher.DoFinal(Encoding.UTF8.GetBytes(payload))!;
            await storage.SetAsync(CiphertextKey, JsonSerializer.Serialize(new Envelope(
                Convert.ToBase64String(cipher.GetIV()!), Convert.ToBase64String(encrypted))));
        }
        catch (OperationCanceledException) { await RemoveAsync(); throw; }
        catch
        {
            await RemoveAsync();
            throw new InvalidOperationException("Não foi possível ativar a biometria. Confira as configurações do aparelho e tente novamente.");
        }
    }

    public async Task<string> UnlockAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30) || !await IsAvailableAsync()) throw Unavailable();
        try
        {
            var raw = await storage.GetAsync(CiphertextKey) ?? throw Unavailable();
            var data = JsonSerializer.Deserialize<Envelope>(raw) ?? throw Unavailable();
            using var cipher = CreateCipher();
            cipher.Init(CipherMode.DecryptMode, GetKey(), new GCMParameterSpec(128, Convert.FromBase64String(data.Iv)));
            await AuthorizeAsync(cipher, "Entrar no ACS View");
            return Encoding.UTF8.GetString(cipher.DoFinal(Convert.FromBase64String(data.Ciphertext))!);
        }
        catch (OperationCanceledException) { throw; }
        catch (KeyPermanentlyInvalidatedException)
        {
            await RemoveAsync();
            throw new InvalidOperationException("As biometrias do aparelho mudaram. Entre com a senha e ative a biometria novamente.");
        }
        catch
        {
            throw new InvalidOperationException("Não foi possível desbloquear com biometria. Use a senha da conta.");
        }
    }

    public Task RemoveAsync()
    {
        storage.Remove(CiphertextKey);
        using var store = KeyStore.GetInstance("AndroidKeyStore")!;
        store.Load(null);
        if (store.ContainsAlias(Alias)) store.DeleteEntry(Alias);
        return Task.CompletedTask;
    }

    [SupportedOSPlatform("android30.0")]
    private static void CreateKey()
    {
        using var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, "AndroidKeyStore")!;
        using var builder = new KeyGenParameterSpec.Builder(Alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt);
        builder.SetBlockModes(KeyProperties.BlockModeGcm!)
            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone!)
            .SetKeySize(256)
            .SetUserAuthenticationRequired(true)
            .SetUserAuthenticationParameters(0, (int)KeyPropertiesAuthType.BiometricStrong)
            .SetInvalidatedByBiometricEnrollment(true);
        generator.Init(builder.Build()!);
        generator.GenerateKey();
    }

    private static IKey GetKey()
    {
        using var store = KeyStore.GetInstance("AndroidKeyStore")!;
        store.Load(null);
        return store.GetKey(Alias, null) ?? throw Unavailable();
    }
    private static Cipher CreateCipher() => Cipher.GetInstance("AES/GCM/NoPadding")!;
    private static Exception Unavailable() => new InvalidOperationException("Biometria indisponível. Cadastre uma biometria segura nas configurações do Android 11 ou superior, ou entre com a senha.");

    [SupportedOSPlatform("android30.0")]
    private static Task AuthorizeAsync(Cipher cipher, string title) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var activity = Platform.CurrentActivity ?? throw Unavailable();
        using var cancellation = new CancellationSignal();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callback = new AuthCallback(completion);
        using var negative = new NegativeButtonListener(completion, cancellation);
        using var builder = new BiometricPrompt.Builder(activity);
        builder.SetTitle(title)
            .SetSubtitle("Confirme com a biometria cadastrada neste aparelho")
            .SetAllowedAuthenticators((int)BiometricManagerAuthenticators.BiometricStrong)
            .SetNegativeButton("Usar senha", activity.MainExecutor!, negative);
        using var prompt = builder.Build()!;
        // Every encryption/decryption needs its own OS authorization, tied to this Cipher.
        prompt.Authenticate(new BiometricPrompt.CryptoObject(cipher), cancellation, activity.MainExecutor!, callback);
        try
        {
            await completion.Task.WaitAsync(TimeSpan.FromMinutes(2));
        }
        catch (TimeoutException)
        {
            cancellation.Cancel();
            throw new OperationCanceledException("A confirmação biométrica expirou. Use a senha ou tente novamente.");
        }
    });

    [SupportedOSPlatform("android30.0")]
    private sealed class AuthCallback(TaskCompletionSource<bool> completion) : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result)
        {
            if (result?.CryptoObject?.Cipher is not null) completion.TrySetResult(true);
            else completion.TrySetException(Unavailable());
        }
        public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString) =>
            completion.TrySetException(new OperationCanceledException("Biometria não confirmada. Você pode entrar com a senha."));
        // OnAuthenticationFailed is retryable; the OS controls retries and lockout.
    }
    private sealed class NegativeButtonListener(TaskCompletionSource<bool> completion, CancellationSignal cancellation)
        : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        public void OnClick(IDialogInterface? dialog, int which)
        {
            cancellation.Cancel();
            completion.TrySetException(new OperationCanceledException("Use a senha da conta para entrar."));
        }
    }
}
