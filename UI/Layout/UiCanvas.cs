using System.Drawing;

namespace DragonGlareAlpha;

internal static class UiCanvas
{
    public const int VirtualWidth = 640;
    public const int VirtualHeight = 480;
    public const int WindowClientWidth = VirtualWidth;
    public const int WindowClientHeight = VirtualHeight;

    public static readonly Size VirtualSize = new(VirtualWidth, VirtualHeight);
    public static readonly Size WindowClientSize = new(WindowClientWidth, WindowClientHeight);
    public static readonly Rectangle FontFallbackWindow = new(8, 8, 624, 44);
}
