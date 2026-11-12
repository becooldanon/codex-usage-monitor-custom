using System.Runtime.InteropServices;

namespace CodexUsageTray;

internal static class Palette
{
    internal static Color For(int? value) => value switch
    {
        null => Color.FromArgb(140, 149, 164),
        <= 5 => Color.FromArgb(239, 68, 68),
        <= 10 => Color.FromArgb(249, 115, 22),
        <= 20 => Color.FromArgb(245, 158, 11),
        _ => IsLight ? Color.FromArgb(7, 132, 92) : Color.FromArgb(52, 211, 153)
    };
    internal static string Theme { get; private set; } = "Dark";
    internal static void SetTheme(string theme) => Theme = theme is "Light" or "Dark" ? theme : "Dark";
    internal static bool IsLight => Theme == "Light";
    internal static Color Background => IsLight ? Color.FromArgb(244, 247, 252) : Color.Black;
    internal static Color Panel => IsLight ? Color.White : Color.FromArgb(18, 18, 18);
    internal static Color Foreground => IsLight ? Color.FromArgb(25, 34, 51) : Color.FromArgb(245, 245, 245);
    internal static Color Secondary => IsLight ? Color.FromArgb(85, 100, 122) : Color.FromArgb(190, 190, 190);
    internal static Color Track => IsLight ? Color.FromArgb(214, 222, 234) : Color.FromArgb(62, 62, 62);
    internal static Color Guide => IsLight ? Color.FromArgb(217, 225, 236) : Color.FromArgb(52, 52, 52);
    internal static Color Border => IsLight ? Color.FromArgb(195, 209, 224) : Color.FromArgb(65, 65, 65);
}

internal static class TrayNumbers
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    // The single notification icon contains two independent remaining percentages:
    // top = 5-hour allowance (green), bottom = weekly allowance (blue).
    internal static Icon Render(int? fiveHour, int? weekly)
    {
        using Bitmap bitmap = new(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var background = new SolidBrush(Color.FromArgb(15, 20, 28));
        graphics.FillRoundedRectangle(background, new Rectangle(0, 0, 31, 31), 5);

        using var separator = new Pen(Color.FromArgb(85, 98, 112), 1);
        graphics.DrawLine(separator, 3, 16, 28, 16);

        DrawReading(graphics, fiveHour, 0, Color.FromArgb(66, 225, 131));
        DrawReading(graphics, weekly, 17, Color.FromArgb(71, 167, 255));

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally { DestroyIcon(handle); }
    }

    private static void DrawReading(Graphics graphics, int? percent, int top, Color color)
    {
        string value = percent is int n ? Math.Clamp(n, 0, 100).ToString() : "--";
        // Fit 100 and smaller values without clipping the 32px tray canvas.
        float size = value.Length == 3 ? 13f : 16f;
        using Font font = new("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(
            graphics, value, font, new Rectangle(1, top, 30, 15), percent.HasValue ? color : Color.Gray,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
    }
}

internal static class GraphicsExtensions
{
    internal static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rectangle, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}