using System.Drawing.Drawing2D;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawWindow(Graphics g, Rectangle rect)
    {
        using var background = new SolidBrush(Color.Black);
        using var shadowBrush = new SolidBrush(Color.FromArgb(96, 0, 0, 0));
        using var glowPen = new Pen(Color.FromArgb(0, 72, 255), 6);
        using var outerPen = new Pen(Color.FromArgb(0, 120, 255), 3);
        using var innerPen = new Pen(Color.FromArgb(132, 206, 255), 1);

        g.FillRectangle(shadowBrush, rect.X + 6, rect.Y + 6, rect.Width, rect.Height);
        g.FillRectangle(background, rect);
        g.DrawRectangle(glowPen, rect);
        g.DrawRectangle(outerPen, rect);
        var innerRect = Rectangle.Inflate(rect, -7, -7);
        g.DrawRectangle(innerPen, innerRect);
    }

    private void DrawOption(Graphics g, bool selected, int x, int y, string text)
    {
        if (selected)
        {
            DrawSelectionMarker(g, x - 28, y + 10);
        }

        DrawText(g, text, x, y);
    }

    private void DrawText(Graphics g, string text, int x, int y, Font? fontOverride = null)
    {
        var font = fontOverride ?? uiFont;
        using var brush = new SolidBrush(Color.White);
        var lines = NormalizeTextLines(text);
        var textOffsetY = Math.Max(0f, (UiTypography.LineHeight - font.Height) / 2f);
        for (var index = 0; index < lines.Count; index++)
        {
            DrawTextLine(g, lines[index], font, brush, x, y + (index * UiTypography.LineHeight) + textOffsetY);
        }
    }

    private void DrawText(
        Graphics g,
        string text,
        Rectangle bounds,
        Font? fontOverride = null,
        StringAlignment alignment = StringAlignment.Near,
        StringAlignment lineAlignment = StringAlignment.Near,
        bool wrap = false)
    {
        var font = fontOverride ?? uiFont;
        var maxLines = Math.Max(1, bounds.Height / UiTypography.LineHeight);
        var lines = LayoutTextLines(g, text, font, bounds.Width, maxLines, wrap);
        var totalHeight = lines.Count * UiTypography.LineHeight;
        var startY = lineAlignment switch
        {
            StringAlignment.Center => bounds.Y + Math.Max(0, (bounds.Height - totalHeight) / 2),
            StringAlignment.Far => bounds.Bottom - totalHeight,
            _ => bounds.Y
        };

        var clipState = g.Save();
        g.SetClip(bounds);
        using var brush = new SolidBrush(Color.White);
        var textOffsetY = Math.Max(0f, (UiTypography.LineHeight - font.Height) / 2f);

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineWidth = MeasureTextWidth(g, line, font);
            var startX = alignment switch
            {
                StringAlignment.Center => bounds.X + Math.Max(0, (bounds.Width - lineWidth) / 2),
                StringAlignment.Far => bounds.Right - lineWidth,
                _ => bounds.X
            };
            DrawTextLine(g, line, font, brush, startX, startY + (index * UiTypography.LineHeight) + textOffsetY);
        }

        g.Restore(clipState);
    }

    private static List<string> NormalizeTextLines(string text)
    {
        return text.Replace("\r\n", "\n").Split('\n').ToList();
    }

    private static List<string> LayoutTextLines(Graphics g, string text, Font font, int maxWidth, int maxLines, bool wrap)
    {
        var output = new List<string>();
        foreach (var rawLine in NormalizeTextLines(text))
        {
            if (!wrap)
            {
                output.Add(TrimLineToWidth(g, rawLine, font, maxWidth));
                if (output.Count >= maxLines)
                {
                    break;
                }

                continue;
            }

            if (rawLine.Length == 0)
            {
                output.Add(string.Empty);
                if (output.Count >= maxLines)
                {
                    break;
                }

                continue;
            }

            var currentLine = string.Empty;
            for (var index = 0; index < rawLine.Length; index++)
            {
                var candidate = $"{currentLine}{rawLine[index]}";
                if (!string.IsNullOrEmpty(currentLine) && MeasureTextWidth(g, candidate, font) > maxWidth)
                {
                    output.Add(currentLine);
                    currentLine = rawLine[index].ToString();
                }
                else
                {
                    currentLine = candidate;
                }

                if (output.Count >= maxLines)
                {
                    break;
                }
            }

            if (output.Count >= maxLines)
            {
                break;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                output.Add(currentLine);
            }

            if (output.Count >= maxLines)
            {
                break;
            }
        }

        if (output.Count == 0)
        {
            output.Add(string.Empty);
        }

        return output;
    }

    private static string TrimLineToWidth(Graphics g, string text, Font font, int maxWidth)
    {
        if (MeasureTextWidth(g, text, font) <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "...";
        if (MeasureTextWidth(g, ellipsis, font) > maxWidth)
        {
            return string.Empty;
        }

        for (var length = text.Length - 1; length > 0; length--)
        {
            var candidate = $"{text[..length]}{ellipsis}";
            if (MeasureTextWidth(g, candidate, font) <= maxWidth)
            {
                return candidate;
            }
        }

        return ellipsis;
    }

    private static readonly StringFormat TextDrawFormat = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
    };

    private static readonly StringFormat TextMeasureFormat = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip | StringFormatFlags.MeasureTrailingSpaces
    };

    private static int MeasureTextWidth(Graphics g, string text, Font font)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (int)Math.Ceiling(g.MeasureString(text, font, PointF.Empty, TextMeasureFormat).Width);
    }

    private static void DrawTextLine(Graphics g, string text, Font font, Brush brush, float x, float y)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        g.DrawString(text, font, brush, x, y, TextDrawFormat);
    }

    private void DrawMenuBackdrop(Graphics g)
    {
        using var gradient = new LinearGradientBrush(
            new Rectangle(0, 0, UiCanvas.VirtualWidth, UiCanvas.VirtualHeight),
            Color.Black,
            Color.FromArgb(0, 10, 22),
            90f);
        using var scanlinePen = new Pen(Color.FromArgb(24, 38, 80));
        using var sideGlowBrush = new SolidBrush(Color.FromArgb(14, 0, 80, 255));

        g.FillRectangle(gradient, 0, 0, UiCanvas.VirtualWidth, UiCanvas.VirtualHeight);

        for (var y = 0; y < UiCanvas.VirtualHeight; y += 4)
        {
            g.DrawLine(scanlinePen, 0, y, UiCanvas.VirtualWidth, y);
        }

        g.FillRectangle(sideGlowBrush, 0, 0, 18, UiCanvas.VirtualHeight);
        g.FillRectangle(sideGlowBrush, UiCanvas.VirtualWidth - 18, 0, 18, UiCanvas.VirtualHeight);
    }

    private void DrawTitleText(Graphics g, string text, int x, int y)
    {
        using var shadowBrush = new SolidBrush(Color.FromArgb(44, 106, 255));
        using var mainBrush = new SolidBrush(Color.FromArgb(238, 244, 255));
        var textOffsetY = Math.Max(0f, (UiTypography.LineHeight - uiFont.Height) / 2f);

        var lines = NormalizeTextLines(text);
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var lineY = y + (lineIndex * UiTypography.LineHeight) + textOffsetY;
            DrawTextLine(g, lines[lineIndex], uiFont, shadowBrush, x + 4, lineY + 4);
            DrawTextLine(g, lines[lineIndex], uiFont, mainBrush, x, lineY);
        }
    }

    private void DrawSelectionMarker(Graphics g, int x, int y)
    {
        if ((frameCounter / 18) % 2 == 1)
        {
            return;
        }

        using var shadowBrush = new SolidBrush(Color.FromArgb(0, 56, 180));
        using var baseBrush = new SolidBrush(Color.FromArgb(0, 120, 255));
        using var shineBrush = new SolidBrush(Color.FromArgb(180, 226, 255));

        g.FillRectangle(shadowBrush, x + 2, y + 2, 12, 12);
        g.FillRectangle(baseBrush, x, y, 12, 12);
        g.FillRectangle(shineBrush, x + 3, y + 3, 4, 4);
    }
}
