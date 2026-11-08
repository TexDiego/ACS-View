using ACS_View.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace ACS_View.Application.Security;
internal static class LocalDatabaseIdentity
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FileIdentity
    {
        public uint Attributes, CreationLow, CreationHigh, AccessLow, AccessHigh, WriteLow, WriteHigh;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileIdentity identity);
    private static string PhysicalIdentity(string path)
    {
        if (!OperatingSystem.IsWindows()) return File.GetCreationTimeUtc(path).Ticks.ToString();
        using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (!GetFileInformationByHandle(file, out var identity)) throw new IOException("Não foi possível verificar a identidade física do banco.");
        return $"{identity.Volume:X8}:{identity.IndexHigh:X8}{identity.IndexLow:X8}:{identity.CreationHigh:X8}{identity.CreationLow:X8}";
    }
    public static async Task<string> GetAsync(IDatabaseService database)
    {
        await database.InitializeAsync();
        var connection = database.Connection;
        await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS LocalAuthIdentity (Id INTEGER PRIMARY KEY CHECK(Id=1), Identity TEXT NOT NULL)");
        await connection.ExecuteAsync("INSERT OR IGNORE INTO LocalAuthIdentity (Id, Identity) VALUES (1, ?)", Guid.NewGuid().ToString("N"));
        var identity = await connection.ExecuteScalarAsync<string>("SELECT Identity FROM LocalAuthIdentity WHERE Id=1");
        // File ID and volume detect physical replacement even when a copied DB carries its GUID.
        var path = Path.GetFullPath(connection.DatabasePath);
        var physical = PhysicalIdentity(path);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{identity}|{path}|{physical}")));
    }
}
