using System.Drawing;
using System.IO;

namespace DragonGlareAlpha;

public sealed class AssetLoader : IDisposable
{
    private static readonly Size CharacterSpriteSize = new(16, 24);
    private static readonly Size TileSpriteSize = new(32, 32);

    private readonly string assetsRoot;
    private readonly Dictionary<string, Image> imageCache = new(StringComparer.OrdinalIgnoreCase);

    public AssetLoader()
    {
        assetsRoot = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    }

    public Image LoadCharacter(string name)
    {
        return LoadImage(Path.Combine("Sprites", "Characters"), name, CharacterSpriteSize);
    }

    public Image LoadEnemy(string name)
    {
        return LoadImage(Path.Combine("Sprites", "Enemies"), name);
    }

    public Image LoadTile(string name)
    {
        return LoadImage("Tiles", name, TileSpriteSize);
    }

    public void Dispose()
    {
        foreach (var image in imageCache.Values)
        {
            image.Dispose();
        }

        imageCache.Clear();
    }

    private Image LoadImage(string relativeFolder, string name, Size? expectedSize = null)
    {
        var fileName = NormalizeFileName(name);
        var assetPath = Path.GetFullPath(
            Path.Combine(assetsRoot, relativeFolder, fileName));

        EnsureAssetPath(assetPath);

        if (imageCache.TryGetValue(assetPath, out var cachedImage))
        {
            return cachedImage;
        }

        if (!File.Exists(assetPath))
        {
            throw new FileNotFoundException($"Asset was not found: {assetPath}", assetPath);
        }

        using var stream = File.OpenRead(assetPath);
        using var sourceImage = Image.FromStream(stream);
        var loadedImage = new Bitmap(sourceImage);

        if (expectedSize is Size requiredSize && loadedImage.Size != requiredSize)
        {
            loadedImage.Dispose();
            throw new InvalidDataException(
                $"Asset '{fileName}' must be {requiredSize.Width}x{requiredSize.Height} pixels, " +
                $"but was {sourceImage.Width}x{sourceImage.Height}.");
        }

        imageCache[assetPath] = loadedImage;
        return loadedImage;
    }

    private static string NormalizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Asset name must not be empty.", nameof(name));
        }

        return $"{Path.GetFileNameWithoutExtension(name)}.png";
    }

    private void EnsureAssetPath(string assetPath)
    {
        if (!assetPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Resolved asset path is outside the Assets folder: {assetPath}");
        }
    }
}
