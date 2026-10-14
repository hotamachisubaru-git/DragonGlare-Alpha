using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using DrawingColor = System.Drawing.Color;
using NativeColor = UnityEngine.Color;

namespace DragonGlareUnity.Drawing
{
    // These adapters keep the original 640 x 480 menu layout on Unity's IMGUI.
    // They do not invoke GDI, create native Windows handles, or own Unity assets.
    public abstract class Brush : IDisposable
    {
        public DrawingColor Color { get; protected set; }
        public virtual void Dispose() { }
    }

    public sealed class SolidBrush : Brush
    {
        public SolidBrush(DrawingColor color) { Color = color; }
    }

    public sealed class Pen : IDisposable
    {
        public DrawingColor Color { get; set; }
        public float Width { get; set; }
        public Pen(DrawingColor color, float width = 1f)
        {
            Color = color;
            Width = Math.Max(0f, width);
        }
        public void Dispose() { }
    }

    public sealed class Font : IDisposable
    {
        public static UnityEngine.Font? NativeFont { get; set; }
        private readonly UnityEngine.Font? font;
        private GUIStyle? style;
        private UnityEngine.Font? cachedNativeFont;
        public float Size { get; }
        public int Height => (int)Math.Ceiling(Size * (16f / 14f));

        public Font(float size = 14f) { Size = Math.Max(1f, size); }
        public Font(UnityEngine.Font nativeFont, float size = 14f) : this(size)
        {
            font = nativeFont;
        }
        public Font(string familyName, float size) : this(size) { }

        internal GUIStyle Style
        {
            get
            {
                var currentFont = font != null ? font : NativeFont;
                if (style == null || cachedNativeFont != currentFont)
                {
                    cachedNativeFont = currentFont;
                    style = new GUIStyle
                    {
                        font = currentFont != null ? currentFont : GUI.skin.font,
                        fontSize = (int)Math.Round(Size),
                        fontStyle = UnityEngine.FontStyle.Normal,
                        alignment = TextAnchor.UpperLeft,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0),
                        clipping = TextClipping.Overflow,
                        wordWrap = false,
                        richText = false,
                        stretchWidth = false,
                        stretchHeight = false
                    };
                    style.normal.textColor = NativeColor.white;
                }
                return style;
            }
        }

        public void Dispose() { style = null; }
    }

    public class Image : IDisposable
    {
        public Texture NativeTexture { get; }
        public int Width => NativeTexture.width;
        public int Height => NativeTexture.height;
        public Image(Texture texture)
        {
            NativeTexture = texture != null ? texture : throw new ArgumentNullException(nameof(texture));
        }
        public virtual void Dispose() { }
    }

    public enum StringAlignment { Near, Center, Far }
    [Flags]
    public enum StringFormatFlags
    {
        DirectionRightToLeft = 1,
        DirectionVertical = 2,
        FitBlackBox = 4,
        DisplayFormatControl = 32,
        NoFontFallback = 1024,
        MeasureTrailingSpaces = 2048,
        NoWrap = 4096,
        LineLimit = 8192,
        NoClip = 16384
    }
    public sealed class StringFormat : IDisposable
    {
        public static StringFormat GenericTypographic => new StringFormat();
        public StringFormatFlags FormatFlags { get; set; }
        public StringAlignment Alignment { get; set; }
        public StringAlignment LineAlignment { get; set; }
        public StringFormat() { }
        public StringFormat(StringFormat other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            FormatFlags = other.FormatFlags;
            Alignment = other.Alignment;
            LineAlignment = other.LineAlignment;
        }
        public void Dispose() { }
    }

    public enum GraphicsUnit { World, Display, Pixel, Point, Inch, Document, Millimeter }
    public enum InterpolationMode
    {
        Invalid = -1, Default, Low, High, Bilinear, Bicubic,
        NearestNeighbor, HighQualityBilinear, HighQualityBicubic
    }
    public enum PixelOffsetMode { Invalid = -1, Default, HighSpeed, HighQuality, None, Half }
    public enum SmoothingMode { Invalid = -1, Default, HighSpeed, HighQuality, None, AntiAlias }
    public enum TextRenderingHint
    {
        SystemDefault, SingleBitPerPixelGridFit, SingleBitPerPixel,
        AntiAliasGridFit, AntiAlias, ClearTypeGridFit
    }

    public sealed class GraphicsState
    {
        internal readonly Graphics Owner;
        internal readonly int ClipDepth;
        internal readonly InterpolationMode Interpolation;
        internal readonly PixelOffsetMode PixelOffset;
        internal readonly SmoothingMode Smoothing;
        internal readonly TextRenderingHint TextHint;
        internal bool Restored;

        internal GraphicsState(Graphics owner, int clipDepth)
        {
            Owner = owner;
            ClipDepth = clipDepth;
            Interpolation = owner.InterpolationMode;
            PixelOffset = owner.PixelOffsetMode;
            Smoothing = owner.SmoothingMode;
            TextHint = owner.TextRenderingHint;
        }
    }

    public sealed class Graphics : IDisposable
    {
        private readonly Stack<Vector2> clipOrigins = new Stack<Vector2>();
        private Vector2 origin;
        private readonly float canvasWidth;
        private readonly float canvasHeight;
        private bool disposed;
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.NearestNeighbor;
        public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.None;
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.None;
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SingleBitPerPixel;

        public Graphics(float width = 640f, float height = 480f)
        {
            canvasWidth = Math.Max(1f, width);
            canvasHeight = Math.Max(1f, height);
        }

        public GraphicsState Save()
        {
            EnsureActive();
            return new GraphicsState(this, clipOrigins.Count);
        }

        public void Restore(GraphicsState state)
        {
            EnsureActive();
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Owner != this || state.Restored || state.ClipDepth > clipOrigins.Count)
                throw new InvalidOperationException("The drawing state does not belong to this active clipping scope.");
            while (clipOrigins.Count > state.ClipDepth)
            {
                GUI.EndGroup();
                origin = clipOrigins.Pop();
            }
            InterpolationMode = state.Interpolation;
            PixelOffsetMode = state.PixelOffset;
            SmoothingMode = state.Smoothing;
            TextRenderingHint = state.TextHint;
            state.Restored = true;
        }

        public void SetClip(Rectangle bounds)
        {
            EnsureActive();
            // Original callers use absolute virtual-canvas coordinates. Unity
            // groups change the local origin, so remember and subtract it for
            // every draw operation, including groups nested inside a text box.
            GUI.BeginGroup(LocalRect(bounds.X, bounds.Y, Math.Max(0, bounds.Width), Math.Max(0, bounds.Height)));
            clipOrigins.Push(origin);
            origin = new Vector2(bounds.X, bounds.Y);
        }

        public void Clear(DrawingColor color)
        {
            Fill(color, 0f, 0f, canvasWidth, canvasHeight);
        }

        public void FillRectangle(Brush brush, Rectangle rectangle) =>
            FillRectangle(brush, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        public void FillRectangle(Brush brush, float x, float y, float width, float height) =>
            Fill(brush.Color, x, y, width, height);

        public void DrawRectangle(Pen pen, Rectangle rectangle) =>
            DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (width < 0f || height < 0f) return;
            DrawLine(pen, x, y, x + width, y);
            DrawLine(pen, x + width, y, x + width, y + height);
            DrawLine(pen, x + width, y + height, x, y + height);
            DrawLine(pen, x, y + height, x, y);
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            EnsureActive();
            if (!IsRepaint || pen.Width <= 0f) return;
            var start = new Vector2(x1 - origin.x, y1 - origin.y);
            var delta = new Vector2(x2 - x1, y2 - y1);
            var matrix = GUI.matrix;
            var color = GUI.color;
            try
            {
                GUI.color = ConvertColor(pen.Color);
                GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
                GUI.DrawTexture(new Rect(start.x, start.y - pen.Width * 0.5f, Math.Max(pen.Width, delta.magnitude), pen.Width),
                    Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            }
            finally
            {
                GUI.matrix = matrix;
                GUI.color = color;
            }
        }

        public void DrawEllipse(Pen pen, Rectangle rectangle) =>
            DrawEllipse(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        public void DrawEllipse(Pen pen, float x, float y, float width, float height) =>
            DrawArc(pen, x, y, width, height, 0f, 360f);
        public void DrawArc(Pen pen, Rectangle rectangle, float startAngle, float sweepAngle) =>
            DrawArc(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, startAngle, sweepAngle);
        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            EnsureActive();
            if (!IsRepaint || width <= 0f || height <= 0f || sweepAngle == 0f) return;
            sweepAngle = Mathf.Clamp(sweepAngle, -360f, 360f);
            var steps = Math.Max(8, (int)Math.Ceiling(Math.Abs(sweepAngle) / 4f));
            var previous = EllipsePoint(x, y, width, height, startAngle);
            for (var step = 1; step <= steps; step++)
            {
                var point = EllipsePoint(x, y, width, height, startAngle + sweepAngle * step / steps);
                DrawLine(pen, previous.x, previous.y, point.x, point.y);
                previous = point;
            }
        }

        public void FillEllipse(Brush brush, Rectangle rectangle) =>
            FillEllipse(brush, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            EnsureActive();
            if (!IsRepaint || width <= 0f || height <= 0f) return;
            var halfHeight = height * 0.5f;
            var centerX = x + width * 0.5f;
            var centerY = y + halfHeight;
            for (var row = (int)Math.Floor(y); row < Math.Ceiling(y + height); row++)
            {
                var top = Math.Max(y, row);
                var bottom = Math.Min(y + height, row + 1f);
                var normalizedY = ((top + bottom) * 0.5f - centerY) / halfHeight;
                var halfWidth = width * 0.5f * Mathf.Sqrt(Math.Max(0f, 1f - normalizedY * normalizedY));
                Fill(brush.Color, centerX - halfWidth, top, halfWidth * 2f, bottom - top);
            }
        }

        public void FillPie(Brush brush, Rectangle rectangle, float startAngle, float sweepAngle) =>
            FillPie(brush, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, startAngle, sweepAngle);
        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            EnsureActive();
            if (!IsRepaint || width <= 0f || height <= 0f || sweepAngle == 0f) return;
            if (Math.Abs(sweepAngle) >= 360f)
            {
                FillEllipse(brush, x, y, width, height);
                return;
            }
            var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweepAngle) / 4f));
            var polygon = new Vector2[steps + 2];
            polygon[0] = new Vector2(x + width * 0.5f, y + height * 0.5f);
            for (var step = 0; step <= steps; step++)
                polygon[step + 1] = EllipsePoint(x, y, width, height, startAngle + sweepAngle * step / steps);
            var intersections = new List<float>();
            for (var row = (int)Math.Floor(y); row < Math.Ceiling(y + height); row++)
            {
                var scanY = row + 0.5f;
                intersections.Clear();
                for (var vertex = 0; vertex < polygon.Length; vertex++)
                {
                    var a = polygon[vertex];
                    var b = polygon[(vertex + 1) % polygon.Length];
                    if ((a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY))
                        intersections.Add(a.x + (scanY - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                intersections.Sort();
                for (var i = 0; i + 1 < intersections.Count; i += 2)
                    Fill(brush.Color, intersections[i], row, intersections[i + 1] - intersections[i], 1f);
            }
        }

        public SizeF MeasureString(string text, Font font, PointF originPoint, StringFormat format)
        {
            EnsureActive();
            var measured = font.Style.CalcSize(new GUIContent(text ?? string.Empty));
            return new SizeF(measured.x, Math.Max(font.Height, measured.y));
        }

        public void DrawString(string text, Font font, Brush brush, float x, float y, StringFormat format)
        {
            EnsureActive();
            if (!IsRepaint || string.IsNullOrEmpty(text)) return;
            var style = font.Style;
            var measured = style.CalcSize(new GUIContent(text));
            var color = GUI.color;
            var textColor = style.normal.textColor;
            try
            {
                GUI.color = NativeColor.white;
                style.normal.textColor = ConvertColor(brush.Color);
                GUI.Label(LocalRect(x, y, measured.x + 2f, Math.Max(font.Height, measured.y)), text, style);
            }
            finally
            {
                style.normal.textColor = textColor;
                GUI.color = color;
            }
        }

        public void DrawImage(Image image, Rectangle destination) =>
            DrawImage(image, destination, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            DrawTextureRegion(image.NativeTexture, LocalRect(x, y, width, height),
                new Rect(0f, 0f, 1f, 1f));
        }
        public void DrawImage(Image image, Rectangle destination, int sourceX, int sourceY,
            int sourceWidth, int sourceHeight, GraphicsUnit unit) =>
            DrawImage(image, destination, new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight), unit);
        public void DrawImage(Image image, Rectangle destination, Rectangle source, GraphicsUnit unit)
        {
            if (unit != GraphicsUnit.Pixel)
                throw new NotSupportedException("Unity image sources use pixel coordinates.");
            // Drawing sources use a top-left origin; Unity texture UVs start
            // at the bottom-left. The Y inversion preserves original crops.
            var uv = new Rect(source.X / (float)image.Width,
                1f - source.Bottom / (float)image.Height,
                source.Width / (float)image.Width, source.Height / (float)image.Height);
            DrawTextureRegion(image.NativeTexture,
                LocalRect(destination.X, destination.Y, destination.Width, destination.Height), uv);
        }
        public void DrawTexture(Texture texture, Rectangle destination) =>
            DrawTextureRegion(texture, LocalRect(destination.X, destination.Y, destination.Width, destination.Height),
                new Rect(0f, 0f, 1f, 1f));

        private void DrawTextureRegion(Texture texture, Rect destination, Rect uv)
        {
            EnsureActive();
            if (!IsRepaint || texture == null || destination.width <= 0f || destination.height <= 0f) return;
            var color = GUI.color;
            var filter = texture.filterMode;
            try
            {
                GUI.color = NativeColor.white;
                texture.filterMode = InterpolationMode == InterpolationMode.NearestNeighbor
                    ? FilterMode.Point : FilterMode.Bilinear;
                GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
            }
            finally
            {
                texture.filterMode = filter;
                GUI.color = color;
            }
        }

        private void Fill(DrawingColor color, float x, float y, float width, float height)
        {
            EnsureActive();
            if (!IsRepaint || width <= 0f || height <= 0f) return;
            var previous = GUI.color;
            try
            {
                GUI.color = ConvertColor(color);
                GUI.DrawTexture(LocalRect(x, y, width, height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            }
            finally { GUI.color = previous; }
        }

        private Rect LocalRect(float x, float y, float width, float height) =>
            new Rect(x - origin.x, y - origin.y, width, height);
        private static NativeColor ConvertColor(DrawingColor color) =>
            new NativeColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        private static bool IsRepaint => Event.current != null && Event.current.type == EventType.Repaint;
        private static Vector2 EllipsePoint(float x, float y, float width, float height, float angle)
        {
            var radians = angle * Mathf.Deg2Rad;
            return new Vector2(x + width * 0.5f + Mathf.Cos(radians) * width * 0.5f,
                y + height * 0.5f + Mathf.Sin(radians) * height * 0.5f);
        }
        private void EnsureActive()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Graphics));
        }
        public void Dispose()
        {
            if (disposed) return;
            while (clipOrigins.Count > 0)
            {
                GUI.EndGroup();
                origin = clipOrigins.Pop();
            }
            disposed = true;
        }
    }
}
