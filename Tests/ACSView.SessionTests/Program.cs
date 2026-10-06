using ACS_View.Application.State;

var tests = new (string Name, Action Run)[]
{
    ("Startup does not request a reset", () =>
    {
        Assert(!new TransientInputLifecycle().ConsumeReset());
    }),
    ("Leaving the app resets inputs once", () =>
    {
        var lifecycle = new TransientInputLifecycle();
        lifecycle.OnStopped();
        Assert(lifecycle.ConsumeReset());
        Assert(!lifecycle.ConsumeReset());
        lifecycle.OnStopped();
        Assert(lifecycle.ConsumeReset());
    }),
    ("Picker resume before completion preserves the workflow", () =>
    {
        var lifecycle = new TransientInputLifecycle();
        using var picker = lifecycle.BeginExternalInteraction();
        lifecycle.OnStopped();
        Assert(!lifecycle.ConsumeReset());
    }),
    ("Picker completion before resume preserves the workflow", () =>
    {
        var lifecycle = new TransientInputLifecycle();
        using (lifecycle.BeginExternalInteraction()) lifecycle.OnStopped();
        Assert(!lifecycle.ConsumeReset());
        lifecycle.OnStopped();
        Assert(lifecycle.ConsumeReset());
    }),
    ("Nested picker scopes and repeated disposal are balanced", () =>
    {
        var lifecycle = new TransientInputLifecycle();
        var outer = lifecycle.BeginExternalInteraction();
        var inner = lifecycle.BeginExternalInteraction();
        inner.Dispose();
        inner.Dispose();
        lifecycle.OnStopped();
        Assert(!lifecycle.ConsumeReset());
        outer.Dispose();
        lifecycle.OnStopped();
        Assert(lifecycle.ConsumeReset());
    }),
    ("External interaction cannot erase a pending app exit", () =>
    {
        var lifecycle = new TransientInputLifecycle();
        lifecycle.OnStopped();
        using var picker = lifecycle.BeginExternalInteraction();
        lifecycle.OnStopped();
        Assert(lifecycle.ConsumeReset());
    })
};

foreach (var (name, run) in tests)
{
    run();
    Console.WriteLine($"PASS: {name}");
}
Console.WriteLine($"{tests.Length} session lifecycle checks passed.");

static void Assert(bool condition)
{
    if (!condition) throw new InvalidOperationException("Unexpected reset policy result.");
}
