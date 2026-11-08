namespace ACS_View.Application.Health;

public enum HealthLoadState { NotLoaded, Loading, Loaded, Error }

/// <summary>One popup, one patient and one login. SQLite work is bounded to one module at a time.</summary>
public sealed class PatientHealthSession(int patientId, int ownerId, Func<bool> isCurrent, string patientName = "") : IDisposable
{
    private sealed class Entry
    {
        public Task<object>? Pending;
        public HealthLoadState State;
        public int Generation;
        public CancellationTokenSource? Cancellation;
    }
    private readonly Dictionary<string, Entry> entries = [];
    private readonly HashSet<Task<object>> operations = [];
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object sync = new();
    private bool disposed;
    public PatientHealthContext Context { get; private set; } = new(patientId, ownerId, patientName);
    public int PatientId => Context.PatientId;
    public int OwnerId => Context.OwnerId;
    public void UpdateContext(PatientHealthContext context)
    {
        EnsureActive();
        if(context.PatientId != PatientId || context.OwnerId != OwnerId) throw new InvalidOperationException("O contexto pertence a outra sessão.");
        Context = context;
    }
    public CancellationToken Token => lifetime.Token;
    public void EnsureActive()
    {
        if(disposed) throw new OperationCanceledException("Sessão encerrada.");
        lifetime.Token.ThrowIfCancellationRequested();
        if (!isCurrent()) throw new OperationCanceledException("Sessão alterada.", lifetime.Token);
    }
    public HealthLoadState State(string key) { lock(sync) return entries.TryGetValue(key, out var e) ? e.State : HealthLoadState.NotLoaded; }
    public Task<T> GetAsync<T>(string key, Func<CancellationToken, Task<T>> load)
    {
        lock(sync)
        {
            EnsureActive();
            if (!entries.TryGetValue(key, out var e)) entries[key] = e = new();
            if(e.Pending is null)
            {
                e.State = HealthLoadState.Loading;
                e.Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                e.Pending = LoadAsync(e, load, e.Generation, e.Cancellation.Token);
                operations.Add(e.Pending);
                _ = ObserveCompletionAsync(e.Pending);
            }
            return Cast<T>(e.Pending);
        }
    }
    private async Task ObserveCompletionAsync(Task<object> task)
    {
        try { await task.ConfigureAwait(false); } catch { /* caller receives the same failure */ }
        lock(sync) operations.Remove(task);
    }
    private static async Task<T> Cast<T>(Task<object> task) => (T)await task.ConfigureAwait(false);
    private async Task<object> LoadAsync<T>(Entry entry, Func<CancellationToken, Task<T>> load, int generation, CancellationToken token)
    {
        await Task.Yield(); // publish the shared Task before any synchronous completion
        try
        {
            await gate.WaitAsync(token).ConfigureAwait(false);
            T result;
            try { token.ThrowIfCancellationRequested(); EnsureActive(); result = await load(token).ConfigureAwait(false); token.ThrowIfCancellationRequested(); EnsureActive(); }
            finally { gate.Release(); }
            lock(sync)
            {
                if(entry.Generation != generation) throw new OperationCanceledException("Dados invalidados.");
                entry.State = HealthLoadState.Loaded;
            }
            return result!;
        }
        catch
        {
            lock(sync) { if(entry.Generation == generation && !disposed) entry.State = HealthLoadState.Error; }
            throw;
        }
    }
    public void Invalidate(string key)
    {
        lock(sync)
        {
            if(entries.TryGetValue(key, out var entry))
            {
                entry.Generation++; entry.Cancellation?.Cancel(); entry.Cancellation?.Dispose(); entry.Cancellation = null;
                entry.Pending = null; entry.State = HealthLoadState.NotLoaded;
            }
        }
    }
    public void Dispose()
    {
        Task<object>[] pending;
        lock(sync)
        {
            if(disposed) return;
            disposed = true; lifetime.Cancel();
            pending = operations.ToArray();
            foreach(var entry in entries.Values) entry.Cancellation?.Dispose();
            entries.Clear();
        }
        _ = ReleaseAsync(pending);
    }
    private async Task ReleaseAsync(Task<object>[] pending)
    {
        try { await Task.WhenAll(pending).ConfigureAwait(false); } catch { /* failures were delivered to module callers */ }
        gate.Dispose(); lifetime.Dispose();
    }
}
