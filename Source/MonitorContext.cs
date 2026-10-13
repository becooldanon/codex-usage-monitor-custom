using Microsoft.Win32;

namespace CodexUsageTray;

internal sealed class MonitorContext : ApplicationContext
{
    private readonly CodexRateLimitReader _reader = new();
    private readonly SettingsStore _store = new();
    private readonly HistoryStore _history = new();
    private readonly MonitorSettings _settings;
    private readonly NotifyIcon _trayIcon = new();
    private readonly Dashboard _dashboard = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly HashSet<string> _alertsSeen = [];
    private readonly string _alertPath = Path.Combine(SettingsStore.Folder, "alerts.json");
    private Icon? _rendered;
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 1000 };
    private DateTime _shownAt = DateTime.MinValue;
    private HotkeyWindow? _hotkey;
    private UsageSnapshot _snapshot = UsageSnapshot.Initial();
    private bool _refreshing;
    private string? _lastRefreshError;
    private readonly ToolStripMenuItem _floatMenu = new("Show floating widget") { CheckOnClick = true };
    private readonly ToolStripMenuItem _startupMenu = new("Start with Windows") { CheckOnClick = true };
    private const string StartupName = "CodexUsageMonitorCustom";

    internal MonitorContext()
    {
        _settings = _store.Load();
        Palette.SetTheme(_settings.Theme);
        _dashboard.ApplyTheme();
        ApplyWindowOpacity();
        _dashboard.SetHistoryHours(_settings.HistoryHours is 1 or 6 or 24 or 168 or 720 ? _settings.HistoryHours : 1);
        _hideTimer.Tick += (_, _) => { if (_settings.AutoHide && !_settings.FloatingVisible && _dashboard.Visible && ! _dashboard.IsInteracting && (DateTime.Now - _shownAt).TotalSeconds >= 5) _dashboard.Hide(); };
        _hideTimer.Start();
        _hotkey = new HotkeyWindow(() => { if (_dashboard.Visible) _dashboard.Hide(); else ShowDashboard(); });
        try { if (File.Exists(_alertPath)) foreach (string key in System.Text.Json.JsonSerializer.Deserialize<string[]>(File.ReadAllText(_alertPath)) ?? []) _alertsSeen.Add(key); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { }
        _dashboard.PositionChanged += () => { _settings.FloatingX = _dashboard.Left; _settings.FloatingY = _dashboard.Top; Save(); };
        _dashboard.CloseRequested += () => { _settings.FloatingVisible = false; _floatMenu.Checked = false; _dashboard.Hide(); Save(); };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show dashboard", null, (_, _) => ShowDashboard());
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync());
        var autoHide = new ToolStripMenuItem("Auto-hide after 5 seconds") { CheckOnClick = true, Checked = _settings.AutoHide };
        autoHide.CheckedChanged += (_, _) => { _settings.AutoHide = autoHide.Checked; Save(); };
        menu.Items.Add(autoHide);
        var themeMenu = new ToolStripMenuItem("Theme");
        foreach (string theme in new[] { "Light", "Dark", "Transparent" })
        {
            string selectedTheme = theme;
            var item = new ToolStripMenuItem(theme) { Checked = Palette.Theme == theme };
            item.Click += (_, _) =>
            {
                _settings.Theme = selectedTheme;
                Palette.SetTheme(selectedTheme);
                _dashboard.ApplyTheme();
                ApplyWindowOpacity();
                foreach (ToolStripMenuItem other in themeMenu.DropDownItems.OfType<ToolStripMenuItem>())
                    other.Checked = ReferenceEquals(other, item);
                SetIcons();
                Save();
            };
            themeMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(themeMenu);
        var opacityMenu = new ToolStripMenuItem("Window opacity");
        foreach (int percent in new[] { 100, 95, 85, 75, 60 })
        {
            int selected = percent;
            var item = new ToolStripMenuItem($"{percent}%") { Checked = _settings.OpacityPercent == percent };
            item.Click += (_, _) => { _settings.OpacityPercent = selected; ApplyWindowOpacity(); foreach (ToolStripMenuItem other in opacityMenu.DropDownItems.OfType<ToolStripMenuItem>()) other.Checked = ReferenceEquals(other, item); Save(); };
            opacityMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(opacityMenu);
        var historyMenu = new ToolStripMenuItem("History range");
        foreach (var range in new[] { ("Last hour", 1), ("6 hours", 6), ("24 hours", 24), ("7 days", 168), ("30 days", 720) })
        {
            int hours = range.Item2;
            var item = new ToolStripMenuItem(range.Item1) { Checked = _settings.HistoryHours == hours };
            item.Click += (_, _) => { _settings.HistoryHours = hours; _dashboard.SetHistoryHours(hours); foreach (ToolStripMenuItem other in historyMenu.DropDownItems.OfType<ToolStripMenuItem>()) other.Checked = ReferenceEquals(other, item); Save(); };
            historyMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(historyMenu);
        menu.Items.Add("Keyboard shortcut: Ctrl+Alt+C", null, (_, _) => MessageBox.Show("Press Ctrl + Alt + C to show or hide the dashboard.", "Codex Usage Monitor"));
        var intervalMenu = new ToolStripMenuItem("Refresh interval");
        foreach (int minutes in new[] { 1, 2, 5, 10, 15, 30 })
        {
            int chosen = minutes;
            var item = new ToolStripMenuItem($"{minutes} minute(s)") { Checked = _settings.RefreshMinutes == minutes };
            item.Click += (_, _) =>
            {
                _settings.RefreshMinutes = chosen;
                _timer.Interval = chosen * 60_000;
                foreach (ToolStripMenuItem option in intervalMenu.DropDownItems.OfType<ToolStripMenuItem>()) option.Checked = ReferenceEquals(option, item);
                Save();
            };
            intervalMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(intervalMenu);
        var alertsMenu = new ToolStripMenuItem("Low-limit alerts");
        foreach (int level in new[] { 20, 10, 5 })
        {
            int chosen = level;
            var item = new ToolStripMenuItem($"{level}% remaining") { CheckOnClick = true, Checked = IsEnabled(level) };
            item.CheckedChanged += (_, _) => { SetEnabled(chosen, item.Checked); Save(); };
            alertsMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(alertsMenu);
        _floatMenu.Checked = _settings.FloatingVisible;
        _floatMenu.CheckedChanged += (_, _) => { _settings.FloatingVisible = _floatMenu.Checked; if (_floatMenu.Checked) ShowDashboard(); else _dashboard.Hide(); Save(); };
        menu.Items.Add(_floatMenu);
        _startupMenu.Checked = IsStartupEnabled();
        _startupMenu.CheckedChanged += (_, _) => SetStartup(_startupMenu.Checked);
        menu.Items.Add(_startupMenu);
        menu.Items.Add("Export history to CSV", null, (_, _) =>
        {
            try { string file = _history.ExportCsv(); MessageBox.Show($"Exported to:\n{file}", "Codex Usage Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit custom monitor", null, (_, _) => ExitThread());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowDashboard(); };
        SetIcons();
        _timer.Interval = Math.Clamp(_settings.RefreshMinutes, 1, 60) * 60_000;
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
        if (_settings.FloatingVisible) ShowDashboard();
        _ = RefreshAsync();
    }
    // In transparent mode, color-key transparency removes the background only.
    // Keep labels and graph lines fully opaque, and restore the normal opacity when switching themes.
    private void ApplyWindowOpacity() => _dashboard.Opacity =
        Palette.Theme == "Transparent" ? 1d
        : Math.Clamp(_settings.OpacityPercent, 40, 100) / 100d;

    private void ShowDashboard()
    {
        if (_settings.FloatingX >= 0 && _settings.FloatingY >= 0)
        {
            var bounds = Screen.AllScreens.Select(s => s.WorkingArea).FirstOrDefault(s => s.Contains(new Point(_settings.FloatingX, _settings.FloatingY)));
            _dashboard.Location = bounds.Width > 0 ? new Point(Math.Clamp(_settings.FloatingX, bounds.Left, bounds.Right - _dashboard.Width), Math.Clamp(_settings.FloatingY, bounds.Top, bounds.Bottom - _dashboard.Height)) : DefaultLocation();
        }
        else _dashboard.Location = DefaultLocation();
        _dashboard.Update(_snapshot, _history.Samples, _refreshing);
        _dashboard.Show();
        _shownAt = DateTime.Now;
        _dashboard.BringToFront();
    }
    private Point DefaultLocation()
    {
        Rectangle area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        return new Point(area.Right - _dashboard.Width - 22, area.Bottom - _dashboard.Height - 22);
    }
    private async Task RefreshAsync()
    {
        if (_refreshing || _shutdown.IsCancellationRequested) return;
        _refreshing = true;
        _dashboard.Update(_snapshot, _history.Samples, true);
        try
        {
            _snapshot = await _reader.FetchAsync(_shutdown.Token);
            _lastRefreshError = null;
            if (_snapshot.ErrorMessage is not null) WriteDiagnostic(_snapshot.ErrorMessage);
            _history.Add(_snapshot);
            SetIcons();
            CheckAlerts("5h", _snapshot.FiveHour, _trayIcon);
            CheckAlerts("7d", _snapshot.Weekly, _trayIcon);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { return; }
        catch (Exception ex)
        {
            WriteDiagnostic(ex.ToString());
            _lastRefreshError = ex.Message;
            // Preserve the last successful reading instead of resetting the dashboard to N/A.
        }
        finally
        {
            _refreshing = false;
            if (!_shutdown.IsCancellationRequested)
            {
                _dashboard.Update(_snapshot, _history.Samples, false);
                if (_lastRefreshError is not null) _dashboard.ShowRefreshError(_lastRefreshError);
            }
        }
    }
    private static void WriteDiagnostic(string message)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.Folder);
            File.AppendAllText(Path.Combine(SettingsStore.Folder, "diagnostics.log"),
                $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    private void SetIcons()
    {
        int? five = _snapshot.FiveHour.State == LimitState.Available ? _snapshot.FiveHour.RemainingPercent : null;
        int? week = _snapshot.Weekly.State == LimitState.Available ? _snapshot.Weekly.RemainingPercent : null;
        int? remaining = new int?[] { five, week }.Min();
        Icon next = TrayNumbers.Render("", remaining);
        Icon? old = _rendered;
        _rendered = next;
        _trayIcon.Icon = next;
        _trayIcon.Text = $"Codex 5h: {Format(five)} | 7d: {Format(week)}";
        _trayIcon.Visible = true;
        old?.Dispose();
    }
    private static string Format(int? value) => value.HasValue ? $"{value}% left" : "N/A";
    private void CheckAlerts(string window, LimitReading reading, NotifyIcon icon)
    {
        if (reading.State != LimitState.Available || reading.RemainingPercent is not int remaining) return;
        foreach (int threshold in new[] { 20, 10, 5 })
        {
            if (!IsEnabled(threshold) || remaining > threshold) continue;
            string key = $"{window}:{reading.ResetsAt?.ToUnixTimeSeconds() ?? 0}:{threshold}";
            if (!_alertsSeen.Add(key)) continue;
            icon.ShowBalloonTip(4000, $"Codex {window} limit: {remaining}%", $"Available allowance is below {threshold}% until reset.", ToolTipIcon.Warning);
        }
        try
        {
            Directory.CreateDirectory(SettingsStore.Folder);
            if (_alertsSeen.Count > 200) _alertsSeen.Clear();
            File.WriteAllText(_alertPath, System.Text.Json.JsonSerializer.Serialize(_alertsSeen));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
    private bool IsEnabled(int threshold) => threshold switch { 20 => _settings.Alert20, 10 => _settings.Alert10, 5 => _settings.Alert5, _ => false };
    private void SetEnabled(int threshold, bool value)
    {
        switch (threshold) { case 20: _settings.Alert20 = value; break; case 10: _settings.Alert10 = value; break; case 5: _settings.Alert5 = value; break; }
    }
    private void Save() { try { _store.Save(_settings); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); } }
    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        return key?.GetValue(StartupName) is string;
    }
    private void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (enable) key.SetValue(StartupName, $"\"{Environment.ProcessPath}\""); else key.DeleteValue(StartupName, false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Startup registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    protected override void ExitThreadCore()
    {
        _timer.Stop(); _shutdown.Cancel();
        _trayIcon.Visible = false;
        _hideTimer.Stop(); _hideTimer.Dispose(); _hotkey?.Dispose();
        _dashboard.Dispose(); _timer.Dispose(); _trayIcon.Dispose();
        _rendered?.Dispose(); _shutdown.Dispose();
        base.ExitThreadCore();
    }
}