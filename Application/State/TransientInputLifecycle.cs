namespace ACS_View.Application.State;

public sealed class TransientInputLifecycle
{
    private int externalInteractionCount;
    private bool resetPending;

    public void OnStopped()
    {
        resetPending |= externalInteractionCount == 0;
    }

    public bool ConsumeReset()
    {
        var shouldReset = resetPending;
        resetPending = false;
        return shouldReset;
    }

    public IDisposable BeginExternalInteraction()
    {
        externalInteractionCount++;
        return new ExternalInteraction(this);
    }

    private sealed class ExternalInteraction(TransientInputLifecycle owner) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            owner.externalInteractionCount--;
        }
    }
}
