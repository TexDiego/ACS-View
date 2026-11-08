using ACS_View.Application.Interfaces;
using System.Collections.Concurrent;
namespace ACS_View.Application.Security;
internal static class LocalAuthenticationGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    public static SemaphoreSlim For(IDatabaseService db) => Gates.GetOrAdd(Path.GetFullPath(db.Connection.DatabasePath), _ => new(1, 1));
}
