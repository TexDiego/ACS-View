using ACS_View.Application.Interfaces;
using System.Diagnostics;

namespace ACS_View.UseCases.Services;

internal sealed class AppStartupService(
    IDatabaseService databaseService,
    ICidSeeder cidSeeder,
    IPatientConditionSeeder patientConditionSeeder) : IAppStartupService
{
    private readonly SemaphoreSlim _startupLock = new(1, 1);
    private bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _startupLock.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (_initialized)
            {
                return;
            }

            Debug.WriteLine("Inicializando banco e seeds do app.");

            cancellationToken.ThrowIfCancellationRequested();
            await databaseService.InitializeAsync();

            cancellationToken.ThrowIfCancellationRequested();
            await cidSeeder.SeedAsync();

            cancellationToken.ThrowIfCancellationRequested();
            await patientConditionSeeder.SeedAsync();

            _initialized = true;
            Debug.WriteLine($"Inicializacao do app concluida em {stopwatch.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Falha na inicializacao do app: {ex.Message}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _startupLock.Release();
        }
    }
}
