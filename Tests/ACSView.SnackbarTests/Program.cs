using ACS_View.Application.Interfaces;
using ACS_View.UseCases.Services;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
    Console.WriteLine("PASS: " + message);
}

var presenter = new ControlledPresenter();
var service = new SnackbarService(presenter);
const string error = "Não há número de WhatsApp válido disponível para este paciente.";
var first = service.ShowAsync(error, SnackbarStyle.Error);
Check(presenter.Calls == 1 && !first.IsCompleted, "First request stays active until the surface closes");
var repeats = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => service.ShowAsync(error))));
Check(repeats.All(shown => !shown) && presenter.Calls == 1, "100 concurrent identical requests are discarded");
Check(!await service.ShowAsync("Another message") && presenter.Calls == 1, "Different messages cannot overlap and are not queued");
presenter.Close();
Check(await first, "Dismissal completes the original request");
Check(presenter.Calls == 1, "Closing the first surface does not replay discarded requests");
var second = service.ShowAsync(error);
Check(presenter.Calls == 2, "The same message may be requested again after dismissal");
presenter.Close();
await second;

var initialCalls = presenter.Calls;
Check(!await service.ShowAsync(" ") && presenter.Calls == initialCalls, "Empty messages are ignored");
using var alreadyCanceled = new CancellationTokenSource();
alreadyCanceled.Cancel();
Check(!await service.ShowAsync(error, cancellationToken: alreadyCanceled.Token) && presenter.Calls == initialCalls, "Canceled requests never reach the presenter");
using var cancellation = new CancellationTokenSource();
var pending = service.ShowAsync(error, cancellationToken: cancellation.Token);
cancellation.Cancel();
Check(!await pending, "Cancellation closes the pending request");
var afterCancellation = service.ShowAsync(error);
Check(presenter.Calls == initialCalls + 2, "Cancellation releases the active slot");
presenter.Close();
await afterCancellation;

presenter.FailNext = true;
Check(!await service.ShowAsync(error), "Presentation failures are contained");
var afterFailure = service.ShowAsync(error);
Check(!afterFailure.IsCompleted, "Presentation failure releases the active slot");
presenter.Close();
await afterFailure;

var racingPresenter = new ControlledPresenter();
var racingService = new SnackbarService(racingPresenter);
var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var attempts = 0;
var race = Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
{
    var request = racingService.ShowAsync(error);
    if (Interlocked.Increment(ref attempts) == 100) dispatched.TrySetResult();
    return await request;
})).ToArray();
await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(5));
await racingPresenter.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
Check(racingPresenter.Calls == 1, "Simultaneous initial requests create only one surface");
racingPresenter.Close();
Check((await Task.WhenAll(race)).Count(shown => shown) == 1, "Exactly one racing request was displayed");
Console.WriteLine($"{checks} snackbar checks passed.");

sealed class ControlledPresenter : ISnackbarPresenter
{
    private TaskCompletionSource closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int calls;
    public int Calls => Volatile.Read(ref calls);
    public bool FailNext { get; set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task ShowUntilDismissedAsync(string message, SnackbarStyle style, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);
        if (FailNext) { FailNext = false; throw new InvalidOperationException("Synthetic display failure"); }
        closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Started.TrySetResult();
        await closed.Task.WaitAsync(cancellationToken);
    }
    public void Close() => closed.TrySetResult();
}
