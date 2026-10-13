namespace CodexUsageTray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using Mutex singleInstance = new(true, "Local\\CodexUsageMonitorCustom-2026", out bool created);
        if (!created) return;
        ApplicationConfiguration.Initialize();
        Application.Run(new MonitorContext());
    }
}