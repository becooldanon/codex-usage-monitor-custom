namespace CodexUsageTray;

internal sealed class Dashboard : Form
{
    private readonly Label _heading = new() { Text = "CODEX  /  USAGE", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Palette.Foreground, AutoSize = true };
    private readonly Label _details = new() { Text = "Connecting...", ForeColor = Palette.Secondary, Font = new Font("Segoe UI", 9), AutoSize = true };
    private readonly WindowCard _five = new("5 HOURS");
    private readonly WindowCard _week = new("7 DAYS");
    private readonly HistoryGraph _chart = new();
    internal bool IsInteracting => ClientRectangle.Contains(PointToClient(Cursor.Position));
    internal void SetHistoryHours(int hours) => _chart.SetHistoryHours(hours);
    internal void ApplyTheme()
    {
        // TransparencyKey removes only the background while retaining opaque text and graph lines.
        BackColor = Palette.Background;
        TransparencyKey = Palette.IsTransparent ? Color.Magenta : Color.Empty;
        _heading.ForeColor = Palette.Foreground;
        _details.ForeColor = Palette.Secondary;
        _five.BackColor = Palette.Background;
        _week.BackColor = Palette.Background;
        _chart.BackColor = Palette.Background;
        foreach (Control control in Controls)
        {
            if (control is Label label && label.Text == "×") label.ForeColor = Palette.Secondary;
            control.Invalidate();
        }
        Invalidate(true);
    }
    private bool _dragging;
    private Point _dragOrigin;
    internal event Action? PositionChanged;
    internal event Action? CloseRequested;

    internal Dashboard()
    {
        Text = "Codex Usage Monitor Custom";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Palette.Background;
        ClientSize = new Size(378, 345);
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;

        _heading.Location = new Point(18, 15);
        _details.Location = new Point(18, 38);
        var close = new Label { Text = "×", ForeColor = Palette.Secondary, Font = new Font("Segoe UI", 17), AutoSize = true, Location = new Point(344, 7), Cursor = Cursors.Hand };
        close.Click += (_, _) => CloseRequested?.Invoke();
        Controls.AddRange([_heading, _details, close]);
        _five.Location = new Point(17, 73);
        _week.Location = new Point(193, 73);
        Controls.AddRange([_five, _week]);
        _chart.Location = new Point(16, 171);
        _chart.Size = new Size(345, 153);
        Controls.Add(_chart);
        foreach (Control header in new Control[] { this, _heading, _details })
        {
            header.MouseDown += (_, args) => { if (args.Button == MouseButtons.Left) { _dragging = true; _dragOrigin = Cursor.Position; } };
            header.MouseMove += (_, _) => { if (_dragging) { Point position = Cursor.Position; Location = new Point(Location.X + position.X - _dragOrigin.X, Location.Y + position.Y - _dragOrigin.Y); _dragOrigin = position; } };
            header.MouseUp += (_, _) => { if (_dragging) { _dragging = false; PositionChanged?.Invoke(); } };
        }
    }
    internal void Update(UsageSnapshot snapshot, IReadOnlyList<HistorySample> history, bool refreshing)
    {
        _five.Update(snapshot.FiveHour);
        _week.Update(snapshot.Weekly);
        _details.Text = snapshot.ErrorMessage is not null ? "Error: " + Truncate(snapshot.ErrorMessage, 42)
            : refreshing ? "Refreshing usage..." : "Updated " + snapshot.RefreshedAt.LocalDateTime.ToString("HH:mm:ss");
        _chart.SetData(history);
    }
    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Palette.Border, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

internal sealed class WindowCard : Control
{
    private readonly string _title;
    private LimitReading _reading = new(LimitState.Unavailable, null, null);
    internal WindowCard(string title) { _title = title; Size = new Size(168, 86); DoubleBuffered = true; }
    internal void Update(LimitReading reading) { _reading = reading; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Palette.Panel);
        g.FillRoundedRectangle(brush, new Rectangle(0, 0, Width - 1, Height - 1), 9);
        TextRenderer.DrawText(g, _title, new Font("Segoe UI", 8, FontStyle.Bold), new Rectangle(12, 9, 142, 17), Palette.Secondary);
        int? number = _reading.State == LimitState.Available ? _reading.RemainingPercent : null;
        string text = number is int n ? $"{n}%" : "N/A";
        TextRenderer.DrawText(g, text, new Font("Segoe UI", 19, FontStyle.Bold), new Rectangle(11, 26, 146, 35), Palette.IsTransparent ? Color.Black : Palette.For(number));
        using var track = new SolidBrush(Palette.Track);
        g.FillRoundedRectangle(track, new Rectangle(12, 67, 142, 6), 3);
        if (number is int value && value > 0)
        {
            using var fill = new SolidBrush(Palette.For(value));
            g.FillRoundedRectangle(fill, new Rectangle(12, 67, Math.Max(3, (int)(142 * Math.Clamp(value, 0, 100) / 100.0)), 6), 3);
        }
    }
}

internal sealed class HistoryGraph : Control
{
    private IReadOnlyList<HistorySample> _points = [];
    private int _hours = 1;
    internal void SetHistoryHours(int hours) { _hours = hours; Invalidate(); }
    internal HistoryGraph() { DoubleBuffered = true; }
    internal void SetData(IReadOnlyList<HistorySample> points) { _points = points; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var back = new SolidBrush(Palette.Panel);
        g.FillRoundedRectangle(back, new Rectangle(0, 0, Width - 1, Height - 1), 9);
        using var titleFont = new Font("Segoe UI", 9, FontStyle.Bold);
        TextRenderer.DrawText(g, _hours switch { 1 => "LAST HOUR", 6 => "LAST 6 HOURS", 24 => "LAST 24 HOURS", 168 => "LAST 7 DAYS", _ => "LAST 30 DAYS" }, titleFont, new Rectangle(12, 8, 190, 22), Palette.Foreground);
        using var smallFont = new Font("Segoe UI", 8);
        TextRenderer.DrawText(g, "5h", smallFont, new Rectangle(252, 10, 28, 18), Palette.IsTransparent ? Color.Black : Color.FromArgb(72, 185, 240));
        TextRenderer.DrawText(g, "7d", smallFont, new Rectangle(292, 10, 28, 18), Palette.IsTransparent ? Color.Black : Color.FromArgb(166, 117, 239));
        var area = new Rectangle(34, 36, Width - 51, Height - 55);
        using var guide = new Pen(Palette.Guide);
        foreach (int p in new[] { 0, 50, 100 })
        {
            float y = area.Bottom - p * area.Height / 100f;
            g.DrawLine(guide, area.Left, y, area.Right, y);
            TextRenderer.DrawText(g, p.ToString(), smallFont, new Rectangle(5, (int)y - 8, 27, 17), Palette.Secondary);
        }
        var recent = _points.Where(p => p.Time >= DateTimeOffset.Now.AddHours(-_hours)).OrderBy(p => p.Time).ToArray();
        if (recent.Length == 0) { TextRenderer.DrawText(g, "No history yet", smallFont, area, Palette.Secondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; }
        DateTimeOffset from = DateTimeOffset.Now.AddHours(-_hours);
        DateTimeOffset to = DateTimeOffset.Now;
        double seconds = Math.Max(1, (to - from).TotalSeconds);
        void Draw(Func<HistorySample, int?> select, Color color)
        {
            var segments = new List<PointF>();
            using var pen = new Pen(Palette.IsTransparent ? Color.Black : color, 2f);
            foreach (var sample in recent)
            {
                int? percent = select(sample);
                if (percent is null) { if (segments.Count > 1) g.DrawLines(pen, segments.ToArray()); segments.Clear(); continue; }
                segments.Add(new PointF(area.Left + (float)((sample.Time - from).TotalSeconds / seconds * area.Width), area.Bottom - (float)(Math.Clamp(percent.Value, 0, 100) * area.Height / 100d)));
            }
            if (segments.Count > 1) g.DrawLines(pen, segments.ToArray());
            else if (segments.Count == 1) { using var dot = new SolidBrush(Palette.IsTransparent ? Color.Black : color); g.FillEllipse(dot, segments[0].X - 2, segments[0].Y - 2, 4, 4); }
        }
        Draw(s => s.FiveHour, Palette.IsTransparent ? Color.Black : Color.FromArgb(72, 185, 240));
        Draw(s => s.Weekly, Palette.IsTransparent ? Color.Black : Color.FromArgb(166, 117, 239));
    }
}