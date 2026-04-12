using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawField(Graphics g)
    {
        DrawFieldScene(g);

        var helpRect = GetFieldHelpWindow();
        var helpInnerRect = Rectangle.Inflate(helpRect, -18, -14);
        DrawWindow(g, helpRect);
        DrawText(g, GetText("fieldHelpLine1"), new Rectangle(helpInnerRect.X, helpInnerRect.Y, helpInnerRect.Width, 18), smallFont);
        DrawText(g, GetText("fieldHelpLine2"), new Rectangle(helpInnerRect.X, helpInnerRect.Y + 28, helpInnerRect.Width, 18), smallFont);
        DrawText(g, GetText("fieldHelpLine3"), new Rectangle(helpInnerRect.X, helpInnerRect.Y + 56, helpInnerRect.Width, 18), smallFont);

        if (isFieldStatusVisible)
        {
            DrawWindow(g, new Rectangle(446, 8, 186, 116));
            DrawText(g, $"{GetDisplayPlayerName()}  Lv.{player.Level}", new Rectangle(458, 22, 160, 24), smallFont);
            DrawText(g, $"HP {player.CurrentHp}/{player.MaxHp}", new Rectangle(458, 48, 160, 24), smallFont);
            DrawText(g, $"MP {player.CurrentMp}/{player.MaxMp}", new Rectangle(458, 72, 160, 24), smallFont);
            DrawText(g, $"G {player.Gold}", new Rectangle(458, 96, 160, 24), smallFont);

            DrawWindow(g, new Rectangle(446, 132, 186, 148));
            DrawText(g, $"ATK {GetTotalAttack()}  DEF {GetTotalDefense()}", new Rectangle(458, 146, 160, 24), smallFont);
            DrawText(g, $"EXP {GetExperienceSummary()}", new Rectangle(458, 176, 160, 24), smallFont);
            DrawText(g, $"ぶき {GetEquippedWeaponName()}", new Rectangle(458, 206, 160, 24), smallFont);
            DrawText(g, $"ぼうぐ {GetEquippedArmorName()}", new Rectangle(458, 234, 160, 24), smallFont);
        }

        if (isFieldDialogOpen)
        {
            DrawWindow(g, FieldLayout.DialogWindow);
            DrawText(g, GetCurrentFieldDialogPage(), new Rectangle(72, 346, 494, 68), smallFont, wrap: true);
            DrawText(
                g,
                activeFieldDialogPageIndex < activeFieldDialogPages.Count - 1
                    ? (selectedLanguage == UiLanguage.Japanese ? "Z: つぎへ" : "Z: NEXT")
                    : (selectedLanguage == UiLanguage.Japanese ? "Z / ESC: とじる" : "Z / ESC: CLOSE"),
                new Rectangle(72, 414, 494, 20),
                smallFont,
                StringAlignment.Far);
        }
    }

    private void DrawFieldScene(Graphics g)
    {
        DrawMenuBackdrop(g);

        var viewport = GetFieldViewport();
        var cameraOrigin = GetFieldCameraOrigin();
        var movementOffset = GetFieldMovementAnimationOffset();
        var cameraAnimationOffset = GetFieldCameraAnimationOffset(cameraOrigin, movementOffset);
        var playerAnimationOffset = GetPlayerAnimationOffset(cameraOrigin, movementOffset);
        var visibleWidthTiles = GetFieldViewportWidthTiles();
        var visibleHeightTiles = GetFieldViewportHeightTiles();

        var clipState = g.Save();
        g.SetClip(viewport);

        using (var voidBrush = new SolidBrush(GetTileColor(MapFactory.WallTile)))
        {
            g.FillRectangle(voidBrush, viewport);
        }

        for (var y = 0; y < visibleHeightTiles; y++)
        {
            for (var x = 0; x < visibleWidthTiles; x++)
            {
                var worldTile = new Point(cameraOrigin.X + x, cameraOrigin.Y + y);
                var tileRect = GetFieldTileRectangle(viewport, cameraOrigin, worldTile, cameraAnimationOffset);
                using var tileBrush = new SolidBrush(GetTileColor(GetTileIdAtWorldPosition(worldTile)));
                g.FillRectangle(tileBrush, tileRect);
            }
        }

        foreach (var fieldEvent in GetCurrentFieldEvents())
        {
            var sprite = GetNpcSprite(fieldEvent.SpriteAssetName);
            DrawWorldTileEntity(g, fieldEvent.TilePosition, viewport, cameraOrigin, cameraAnimationOffset, fieldEvent.DisplayColor, sprite);
        }

        DrawPlayerTileEntity(g, viewport, cameraOrigin, playerAnimationOffset);

        g.Restore(clipState);
        DrawFieldViewportFrame(g, viewport);
    }

    private Color GetTileColor(int tileId)
    {
        return tileId switch
        {
            MapFactory.WallTile when currentFieldMap == FieldMapId.Castle => Color.FromArgb(58, 14, 24),
            MapFactory.WallTile => Color.FromArgb(8, 30, 90),
            MapFactory.CastleBlockTile => Color.FromArgb(120, 28, 38),
            MapFactory.CastleGateTile => Color.FromArgb(116, 58, 30),
            MapFactory.FieldGateTile => Color.FromArgb(24, 56, 40),
            MapFactory.CastleFloorTile => Color.FromArgb(108, 42, 52),
            MapFactory.GrassTile => Color.FromArgb(24, 74, 36),
            MapFactory.DecorationBlueTile when currentFieldMap == FieldMapId.Castle => Color.FromArgb(76, 20, 34),
            MapFactory.DecorationBlueTile => Color.FromArgb(8, 30, 90),
            _ => Color.FromArgb(5, 5, 5)
        };
    }

    private void DrawWorldTileEntity(
        Graphics g,
        Point tile,
        Rectangle viewport,
        Point cameraOrigin,
        Point offset,
        Color color,
        Image? sprite = null)
    {
        var rect = Rectangle.Inflate(
            GetFieldTileRectangle(viewport, cameraOrigin, tile, offset),
            -4,
            -4);

        if (sprite is not null)
        {
            g.DrawImage(sprite, rect);
            return;
        }

        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);
    }

    private void DrawPlayerTileEntity(Graphics g, Rectangle viewport, Point cameraOrigin, Point animationOffset)
    {
        var tileRect = GetFieldTileRectangle(viewport, cameraOrigin, player.TilePosition, new Point(-animationOffset.X, -animationOffset.Y));
        var heroSprite = GetHeroSprite();

        if (heroSprite is not null)
        {
            var rect = new Rectangle(
                tileRect.X + ((TileSize - heroSprite.Width) / 2),
                tileRect.Bottom - heroSprite.Height,
                heroSprite.Width,
                heroSprite.Height);
            g.DrawImage(heroSprite, rect);
            return;
        }

        var fallbackRect = Rectangle.Inflate(tileRect, -2, -2);
        using var brush = new SolidBrush(Color.White);
        g.FillRectangle(brush, fallbackRect);
    }

    private void DrawFieldViewportFrame(Graphics g, Rectangle rect)
    {
        using var shadowPen = new Pen(Color.FromArgb(88, 0, 0, 0), 4);
        using var glowPen = new Pen(Color.FromArgb(0, 72, 255), 4);
        using var outerPen = new Pen(Color.FromArgb(0, 120, 255), 2);
        using var innerPen = new Pen(Color.FromArgb(132, 206, 255), 1);

        g.DrawRectangle(shadowPen, rect.X + 6, rect.Y + 6, rect.Width, rect.Height);
        g.DrawRectangle(glowPen, rect);
        g.DrawRectangle(outerPen, rect);
        g.DrawRectangle(innerPen, Rectangle.Inflate(rect, -5, -5));
    }
}
