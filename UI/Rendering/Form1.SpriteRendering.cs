using System.IO;
using System.Drawing.Drawing2D;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void LoadFieldSprites()
    {
        if (LoadHeroSpriteSheet("hero_4.png"))
        {
            return;
        }

        LoadHeroSprite(PlayerFacingDirection.Left, "hero_left.png", "hero.png");
        LoadHeroSprite(PlayerFacingDirection.Right, "hero_right.png", "hero_left.png", "hero.png");
        LoadHeroSprite(PlayerFacingDirection.Up, "hero_up.png", "hero_left.png", "hero.png");
        LoadHeroSprite(PlayerFacingDirection.Down, "hero_down.png", "hero_left.png", "hero.png");
    }

    private Image? GetNpcSprite(string? spriteAssetName)
    {
        if (string.IsNullOrWhiteSpace(spriteAssetName))
        {
            return null;
        }

        if (npcSprites.TryGetValue(spriteAssetName, out var cachedSprite))
        {
            return cachedSprite;
        }

        var sprite = LoadSprite(Path.Combine("Sprites", "NPC"), spriteAssetName);
        if (sprite is not null)
        {
            npcSprites[spriteAssetName] = sprite;
        }

        return sprite;
    }

    private static Image? LoadSprite(string assetSubdirectory, string fileName)
    {
        var path = ResolveAssetPath(assetSubdirectory, fileName);
        if (path is null)
        {
            return null;
        }

        try
        {
            return Image.FromFile(path);
        }
        catch
        {
            return null;
        }
    }

    private void LoadHeroSprite(PlayerFacingDirection direction, params string[] fileNames)
    {
        var sprite = LoadPreparedHeroSprite(fileNames);
        if (sprite is null)
        {
            return;
        }

        heroSprites[direction] = sprite;
    }

    private bool LoadHeroSpriteSheet(string fileName)
    {
        var source = LoadSprite(Path.Combine("Sprites", "Characters"), fileName);
        if (source is not Bitmap spriteSheet)
        {
            source?.Dispose();
            return false;
        }

        try
        {
            var cellWidth = spriteSheet.Width / 2;
            var cellHeight = spriteSheet.Height / 2;
            if (cellWidth <= 0 || cellHeight <= 0)
            {
                return false;
            }

            // hero_4.png layout (actual observed layout):
            // top-left = left, top-right = right, bottom-left = up, bottom-right = down
            // This mapping fixes the orientation bug where left/right were swapped.
            LoadHeroSpriteFromSheet(spriteSheet, PlayerFacingDirection.Left, new Rectangle(0, 0, cellWidth, cellHeight));
            LoadHeroSpriteFromSheet(spriteSheet, PlayerFacingDirection.Right, new Rectangle(cellWidth, 0, cellWidth, cellHeight));
            LoadHeroSpriteFromSheet(spriteSheet, PlayerFacingDirection.Up, new Rectangle(0, cellHeight, cellWidth, cellHeight));
            LoadHeroSpriteFromSheet(spriteSheet, PlayerFacingDirection.Down, new Rectangle(cellWidth, cellHeight, cellWidth, cellHeight));
            return heroSprites.Count > 0;
        }
        finally
        {
            spriteSheet.Dispose();
        }
    }

    private void LoadHeroSpriteFromSheet(Bitmap spriteSheet, PlayerFacingDirection direction, Rectangle sourceRegion)
    {
        using var cellBitmap = spriteSheet.Clone(sourceRegion, spriteSheet.PixelFormat);
        var crop = FindLargestOpaqueRegionBounds(cellBitmap, alphaThreshold: 8);
        if (crop.Width <= 0 || crop.Height <= 0)
        {
            return;
        }

        using var cropped = cellBitmap.Clone(crop, cellBitmap.PixelFormat);
        heroSprites[direction] = ResizeSprite(cropped, targetHeight: TileSize + 16);
    }

    private Image? GetHeroSprite()
    {
        if (heroSprites.TryGetValue(playerFacingDirection, out var facingSprite))
        {
            return facingSprite;
        }

        if (heroSprites.TryGetValue(PlayerFacingDirection.Down, out var downSprite))
        {
            return downSprite;
        }

        if (heroSprites.TryGetValue(PlayerFacingDirection.Left, out var leftSprite))
        {
            return leftSprite;
        }

        return heroSprites.Values.FirstOrDefault();
    }

    private static Image? LoadPreparedHeroSprite(params string[] fileNames)
    {
        Image? source = null;
        foreach (var fileName in fileNames)
        {
            source = LoadSprite(Path.Combine("Sprites", "Characters"), fileName);
            if (source is not null)
            {
                break;
            }
        }

        if (source is not Bitmap sourceBitmap)
        {
            source?.Dispose();
            return source;
        }

        try
        {
            var crop = FindOpaqueBounds(sourceBitmap, alphaThreshold: 8);
            if (crop.Width <= 0 || crop.Height <= 0)
            {
                return new Bitmap(sourceBitmap);
            }

            using var cropped = sourceBitmap.Clone(crop, sourceBitmap.PixelFormat);
            return ResizeSprite(cropped, targetHeight: TileSize + 16);
        }
        finally
        {
            sourceBitmap.Dispose();
        }
    }

    private static Rectangle FindOpaqueBounds(Bitmap bitmap, byte alphaThreshold)
    {
        var minX = bitmap.Width;
        var minY = bitmap.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A < alphaThreshold)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX || maxY < minY
            ? Rectangle.Empty
            : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    private static Rectangle FindLargestOpaqueRegionBounds(Bitmap bitmap, byte alphaThreshold)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var visited = new bool[width * height];
        var queue = new Queue<Point>();
        var bestBounds = Rectangle.Empty;
        var bestPixelCount = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width) + x;
                if (visited[index] || bitmap.GetPixel(x, y).A < alphaThreshold)
                {
                    continue;
                }

                visited[index] = true;
                queue.Enqueue(new Point(x, y));

                var minX = x;
                var minY = y;
                var maxX = x;
                var maxY = y;
                var pixelCount = 0;

                while (queue.Count > 0)
                {
                    var point = queue.Dequeue();
                    pixelCount++;
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);

                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (var offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                continue;
                            }

                            var nextX = point.X + offsetX;
                            var nextY = point.Y + offsetY;
                            if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                            {
                                continue;
                            }

                            var nextIndex = (nextY * width) + nextX;
                            if (visited[nextIndex] || bitmap.GetPixel(nextX, nextY).A < alphaThreshold)
                            {
                                continue;
                            }

                            visited[nextIndex] = true;
                            queue.Enqueue(new Point(nextX, nextY));
                        }
                    }
                }

                if (pixelCount <= bestPixelCount)
                {
                    continue;
                }

                bestPixelCount = pixelCount;
                bestBounds = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
            }
        }

        return bestBounds;
    }

    private static Bitmap ResizeSprite(Image sprite, int targetHeight)
    {
        var scale = targetHeight / (float)sprite.Height;
        var targetWidth = Math.Max(1, (int)Math.Round(sprite.Width * scale));
        var targetSize = new Size(targetWidth, targetHeight);
        var resized = new Bitmap(targetSize.Width, targetSize.Height);

        using var graphics = Graphics.FromImage(resized);
        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.DrawImage(sprite, new Rectangle(Point.Empty, targetSize));

        return resized;
    }

    private void DisposeFieldSprites()
    {
        foreach (var sprite in heroSprites.Values)
        {
            sprite.Dispose();
        }

        heroSprites.Clear();

        foreach (var sprite in npcSprites.Values)
        {
            sprite.Dispose();
        }

        npcSprites.Clear();
    }
}
