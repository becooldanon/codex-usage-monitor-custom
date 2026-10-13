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
        _ => Color.FromArgb(52, 211, 153)
    };
    internal static readonly Color Background = Color.FromArgb(18, 24, 36);
    internal static readonly Color Panel = Color.FromArgb(30, 40, 56);
    internal static readonly Color Foreground = Color.FromArgb(234, 239, 247);
    internal static readonly Color Secondary = Color.FromArgb(166, 179, 199);
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