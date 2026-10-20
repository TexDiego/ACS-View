SQLitePCL.Batteries_V2.Init();
var checks = 0;
var directory = Path.Combine(Path.GetTempPath(), "acs-bug-regressions-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
await DashboardPregnancyRegressionTests.Run(directory, (pass, message) => {
    if (!pass) throw new InvalidOperationException(message);
    checks++; Console.WriteLine("PASS: " + message);
});
await HouseNumberFilterTests.Run(directory, (pass, message) => {
    if (!pass) throw new InvalidOperationException(message);
    checks++; Console.WriteLine("PASS: " + message);
});
Console.WriteLine($"{checks} bug regression checks passed.");
