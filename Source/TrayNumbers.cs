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
    internal static void SetTheme(string theme) => Theme = theme is "Light" or "Dark" or "Transparent" ? theme : "Dark";
    internal static bool IsLight => Theme == "Light";
    internal static Color Background => IsLight ? Color.FromArgb(244, 247, 252) : Color.FromArgb(18, 24, 36);
    internal static Color Panel => IsLight ? Color.White : Color.FromArgb(30, 40, 56);
    internal static Color Foreground => IsLight ? Color.FromArgb(25, 34, 51) : Color.FromArgb(234, 239, 247);
    internal static Color Secondary => IsLight ? Color.FromArgb(85, 100, 122) : Color.FromArgb(166, 179, 199);
    internal static Color Track => IsLight ? Color.FromArgb(214, 222, 234) : Color.FromArgb(62, 76, 93);
    internal static Color Guide => IsLight ? Color.FromArgb(217, 225, 236) : Color.FromArgb(58, 70, 87);
    internal static Color Border => IsLight ? Color.FromArgb(195, 209, 224) : Color.FromArgb(69, 84, 105);
}

internal static class TrayNumbers
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    internal static Icon Render(string title, int? percentage)
    {
        using Bitmap bitmap = new(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var background = new SolidBrush(Palette.Background);
        graphics.FillRoundedRectangle(background, new Rectangle(0, 0, 31, 31), 6);
        string number = percentage is int value ? value.ToString() : "--";
        using Font font = new("Segoe UI", number.Length == 3 ? 13f : 17f, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(graphics, number, font, new Rectangle(0, 2, 32, 24), Palette.For(percentage),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        using Font tagFont = new("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(graphics, title, tagFont, new Rectangle(0, 23, 32, 8), Palette.Secondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        IntPtr handle = bitmap.GetHicon();
        try { using Icon temp = Icon.FromHandle(handle); return (Icon)temp.Clone(); }
        finally { DestroyIcon(handle); }
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