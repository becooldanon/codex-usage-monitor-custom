using System.Text.Json;

namespace CodexUsageTray;

internal sealed class MonitorSettings
{
    public string Theme { get; set; } = "Dark";
    public int RefreshMinutes { get; set; } = 5;
    public bool AutoHide { get; set; } = true;
    public int OpacityPercent { get; set; } = 95;
    public int HistoryHours { get; set; } = 1;
    public bool Alert20 { get; set; } = true;
    public bool Alert10 { get; set; } = true;
    public bool Alert5 { get; set; } = true;
    public bool FloatingVisible { get; set; } = true;
    public int FloatingX { get; set; } = -1;
    public int FloatingY { get; set; } = -1;
}

internal sealed class SettingsStore
{
    internal static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsageMonitorCustom");
    private readonly string _path = Path.Combine(Folder, "settings.json");
    internal MonitorSettings Load()
    {
        try
        {
            if (File.Exists(_path)) return JsonSerializer.Deserialize<MonitorSettings>(File.ReadAllText(_path)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        return new();
    }
    internal void Save(MonitorSettings value)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(_path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
}

internal sealed record HistorySample(DateTimeOffset Time, int? FiveHour, int? Weekly);
internal sealed class HistoryStore
{
    private readonly string _path = Path.Combine(SettingsStore.Folder, "history.json");
    private readonly List<HistorySample> _samples;
    internal IReadOnlyList<HistorySample> Samples => _samples;
    internal HistoryStore()
    {
        try { _samples = File.Exists(_path) ? JsonSerializer.Deserialize<List<HistorySample>>(File.ReadAllText(_path)) ?? [] : []; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { _samples = []; }
        Prune();
    }
    private void Prune() => _samples.RemoveAll(s => s.Time < DateTimeOffset.Now.AddDays(-30));
    internal void Add(UsageSnapshot snapshot)
    {
        int? five = snapshot.FiveHour.State == LimitState.Available ? snapshot.FiveHour.RemainingPercent : null;
        int? week = snapshot.Weekly.State == LimitState.Available ? snapshot.Weekly.RemainingPercent : null;
        if (five is null && week is null) return;
        _samples.Add(new HistorySample(snapshot.RefreshedAt, five, week));
        Prune();
        try
        {
            Directory.CreateDirectory(SettingsStore.Folder);
            File.WriteAllText(_path, JsonSerializer.Serialize(_samples));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
    internal string ExportCsv()
    {
        string file = Path.Combine(SettingsStore.Folder, $"history-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        Directory.CreateDirectory(SettingsStore.Folder);
        File.WriteAllLines(file, ["Timestamp,FiveHourRemaining,WeeklyRemaining", .. _samples.Select(s => $"{s.Time:O},{s.FiveHour},{s.Weekly}")]);
        return file;
    }
}