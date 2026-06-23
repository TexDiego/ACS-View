using ACS_View.Application.Interfaces;

namespace ACS_View.Infrastructure.Services;

internal sealed class MauiMainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action action)
    {
        MainThread.BeginInvokeOnMainThread(action);
    }

    public void Dispatch(Func<Task> action)
    {
        MainThread.BeginInvokeOnMainThread(async () => await action());
    }
}
