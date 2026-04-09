using System.Drawing.Drawing2D;
using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawModeSelect(Graphics g)
    {
        DrawMenuBackdrop(g);

        DrawWindow(g, new Rectangle(118, 236, 408, 172));
        DrawText(g, "モードをせんたくしてください", 154, 268, smallFont);
        DrawOption(g, modeCursor == 0, 154, 307, "はじめから NEW GAME");
        DrawOption(g, modeCursor == 1, 154, 342, "つづきから LOAD GAME");

        if (!string.IsNullOrWhiteSpace(menuNotice))
        {
            DrawWindow(g, new Rectangle(84, 24, 472, 72));
            DrawText(g, menuNotice, 104, 48, smallFont);
        }
    }

    private void DrawLanguageSelection(Graphics g)
    {
        DrawWindow(g, new Rectangle(84, 64, 260, 128));
        DrawOption(g, languageCursor == 0, 104, 94, "にほんご");
        DrawOption(g, languageCursor == 1, 104, 134, "ENGLISH");

        DrawWindow(g, new Rectangle(116, 270, 410, 180));
        DrawText(g, "げんごをえらんでください", 140, 310);
        DrawText(g, "CHOOSE A LANGUAGE", 140, 350);
    }

    private void DrawNameInput(Graphics g)
    {
        var table = GameContent.GetNameTable(selectedLanguage);
        const int originX = 44;
        const int originY = 52;
        const int cellWidth = 56;
        const int cellHeight = 44;

        for (var row = 0; row < table.Length; row++)
        {
            for (var column = 0; column < table[row].Length; column++)
            {
                var textX = originX + (column * cellWidth);
                var textY = originY + (row * cellHeight);
                if (row == nameCursorRow && column == nameCursorColumn)
                {
                    DrawText(g, "▶", textX - 24, textY);
                }

                DrawText(g, table[row][column], textX, textY);
            }
        }

        DrawWindow(g, new Rectangle(116, 270, 410, 180));
        DrawText(g, "なまえをきめてください", 140, 300);
        DrawText(g, "CHOOSE A NAME", 140, 338);
        DrawText(g, playerName.Length == 0 ? "..." : playerName.ToString(), 140, 384);

        DrawText(g, selectedLanguage == UiLanguage.Japanese ? "ESC: もどる" : "ESC: BACK", 14, 442);
    }

    private void DrawSaveSlotSelection(Graphics g)
    {
        DrawMenuBackdrop(g);

        var titleRect = new Rectangle(98, 24, 444, 64);
        DrawWindow(g, titleRect);
        DrawText(
            g,
            saveSlotSelectionMode == SaveSlotSelectionMode.Save
                ? "ぼうけんのしょを えらんでください"
                : "よみこむ ぼうけんのしょを えらんでください",
            new Rectangle(126, 40, 388, 22),
            smallFont);
        DrawText(
            g,
            saveSlotSelectionMode == SaveSlotSelectionMode.Save
                ? "CHOOSE A SAVE SLOT"
                : "CHOOSE A FILE TO LOAD",
            new Rectangle(126, 62, 388, 18),
            smallFont);

        for (var index = 0; index < SaveService.SlotCount; index++)
        {
            var slotNumber = index + 1;
            var summary = saveSlotSummaries.ElementAtOrDefault(index) ?? new SaveSlotSummary
            {
                SlotNumber = slotNumber,
                State = SaveSlotState.Empty
            };

            var slotRect = new Rectangle(98, 108 + (index * 86), 444, 76);
            DrawWindow(g, slotRect);
            if (saveSlotCursor == index)
            {
                DrawSelectionMarker(g, slotRect.X + 14, slotRect.Y + 24);
            }

            DrawText(g, $"ぼうけんのしょ {slotNumber}", slotRect.X + 38, slotRect.Y + 10, smallFont);

            switch (summary.State)
            {
                case SaveSlotState.Occupied:
                    DrawText(g, $"{summary.Name}   LV {summary.Level}   G {summary.Gold}", slotRect.X + 38, slotRect.Y + 30, smallFont);
                    DrawText(
                        g,
                        $"{GetMapDisplayName(summary.CurrentFieldMap)}  {summary.SavedAtLocal:yyyy/MM/dd HH:mm}",
                        new Rectangle(slotRect.X + 38, slotRect.Y + 50, 380, 16),
                        smallFont);
                    break;
                case SaveSlotState.Corrupted:
                    DrawText(g, "BROKEN DATA / よみこめません", slotRect.X + 38, slotRect.Y + 32, smallFont);
                    break;
                default:
                    DrawText(g, "NO DATA / まだ きろくがありません", slotRect.X + 38, slotRect.Y + 32, smallFont);
                    break;
            }
        }

        var helpRect = new Rectangle(116, 408, 408, 40);
        DrawWindow(g, helpRect);
        DrawText(
            g,
            saveSlotSelectionMode == SaveSlotSelectionMode.Save
                ? "ENTER: きろく  ESC: なまえにもどる"
                : "ENTER: よみこむ  ESC: モードにもどる",
            new Rectangle(136, 420, 368, 18),
            smallFont);

        if (!string.IsNullOrWhiteSpace(menuNotice))
        {
            var noticeRect = new Rectangle(110, 366, 420, 30);
            DrawWindow(g, noticeRect);
            DrawText(g, menuNotice, Rectangle.Inflate(noticeRect, -18, -10), smallFont);
        }
    }

    private void DrawField(Graphics g)
    {
        DrawFieldScene(g);

        DrawWindow(g, new Rectangle(8, 8, 430, 84));
        DrawText(g, GetText("fieldHelpLine1"), 20, 26, smallFont);
        DrawText(g, GetText("fieldHelpLine2"), 20, 56, smallFont);

        if (isFieldStatusVisible)
        {
            DrawWindow(g, new Rectangle(446, 8, 186, 116));
            DrawText(g, $"{GetDisplayPlayerName()}  Lv.{player.Level}", new Rectangle(458, 22, 160, 24), smallFont);
            DrawText(g, $"HP {player.CurrentHp}/{player.MaxHp}", new Rectangle(458, 48, 160, 24), smallFont);
            DrawText(g, $"MP {player.CurrentMp}/{player.MaxMp}", new Rectangle(458, 72, 160, 24), smallFont);
            DrawText(g, $"G {player.Gold}", new Rectangle(458, 96, 160, 24), smallFont);

            DrawWindow(g, new Rectangle(446, 132, 186, 124));
            DrawText(g, $"ATK {GetTotalAttack()}  DEF {GetTotalDefense()}", new Rectangle(458, 146, 160, 24), smallFont);
            DrawText(g, $"EXP {GetExperienceSummary()}", new Rectangle(458, 176, 160, 24), smallFont);
            DrawText(g, "そうび", new Rectangle(458, 206, 160, 24), smallFont);
            DrawText(g, GetEquippedWeaponName(), new Rectangle(458, 230, 160, 24), smallFont);
        }

        if (isNpcDialogOpen)
        {
            DrawWindow(g, new Rectangle(46, 320, 548, 138));
            DrawText(g, GetText("npcLine1"), 72, 354, smallFont);
            DrawText(g, GetText("npcLine2"), 72, 392, smallFont);
        }
    }

    private void DrawBattle(Graphics g)
    {
        DrawMenuBackdrop(g);

        var statusRect = new Rectangle(44, 26, 220, 150);
        DrawWindow(g, statusRect);
        DrawText(g, $"{GetDisplayPlayerName()}  Lv.{player.Level}", new Rectangle(statusRect.X + 22, statusRect.Y + 16, 176, 24), smallFont);
        DrawText(g, $"HP {player.CurrentHp}/{player.MaxHp}", new Rectangle(statusRect.X + 22, statusRect.Y + 42, 176, 24), smallFont);
        DrawText(g, $"MP {player.CurrentMp}/{player.MaxMp}", new Rectangle(statusRect.X + 22, statusRect.Y + 68, 176, 24), smallFont);
        DrawText(g, $"ATK {GetTotalAttack()}  DEF {GetTotalDefense()}", new Rectangle(statusRect.X + 22, statusRect.Y + 94, 176, 24), smallFont);
        DrawText(g, GetEquippedWeaponName(), new Rectangle(statusRect.X + 22, statusRect.Y + 120, 176, 24), smallFont);

        var enemyInfoRect = new Rectangle(292, 26, 276, 76);
        DrawWindow(g, enemyInfoRect);
        DrawText(g, currentEncounter?.Enemy.Name ?? "まもの", new Rectangle(enemyInfoRect.X + 18, enemyInfoRect.Y + 14, 236, 24), smallFont);
        if (currentEncounter is not null)
        {
            DrawText(g, $"HP {currentEncounter.CurrentHp}/{currentEncounter.Enemy.MaxHp}", new Rectangle(enemyInfoRect.X + 18, enemyInfoRect.Y + 38, 122, 24), smallFont);
            DrawText(g, $"EXP {currentEncounter.Enemy.ExperienceReward}", new Rectangle(enemyInfoRect.X + 146, enemyInfoRect.Y + 38, 108, 24), smallFont, StringAlignment.Far);
        }

        var actionHeaderRect = new Rectangle(292, 112, 276, 40);
        DrawWindow(g, actionHeaderRect);
        DrawText(g, "こうどう", Rectangle.Inflate(actionHeaderRect, -12, -3), smallFont, StringAlignment.Center, StringAlignment.Center);

        var actionListRect = new Rectangle(292, 152, 276, 92);
        DrawWindow(g, actionListRect);
        var commandGridRect = Rectangle.Inflate(actionListRect, -16, -10);
        var commandCellWidth = commandGridRect.Width / 2;
        var commandCellHeight = commandGridRect.Height / 2;
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 2; column++)
            {
                var cellRect = new Rectangle(
                    commandGridRect.X + (column * commandCellWidth),
                    commandGridRect.Y + (row * commandCellHeight),
                    commandCellWidth,
                    commandCellHeight);
                if (battleFlowState == BattleFlowState.CommandSelection && battleCursorRow == row && battleCursorColumn == column)
                {
                    DrawSelectionMarker(g, cellRect.X + 2, cellRect.Y + (cellRect.Height / 2) - 6);
                }

                DrawText(
                    g,
                    GameContent.BattleCommandLabels[row, column],
                    new Rectangle(cellRect.X + 28, cellRect.Y - 1, cellRect.Width - 30, cellRect.Height),
                    smallFont,
                    StringAlignment.Near,
                    StringAlignment.Center);
            }
        }

        DrawBattleEnemy(g, new Point(194, 272));

        var messageRect = new Rectangle(84, 258, 472, 186);
        DrawWindow(g, messageRect);
        DrawText(g, battleMessage, Rectangle.Inflate(messageRect, -24, -24), smallFont, wrap: true);

        if (battleFlowState != BattleFlowState.CommandSelection)
        {
            DrawText(g, "ENTER: フィールドへ", new Rectangle(336, 414, 210, 20), smallFont, StringAlignment.Far);
        }
    }

    private void DrawEncounterTransition(Graphics g)
    {
        DrawFieldScene(g);

        var progress = 1f - (encounterTransitionFrames / (float)EncounterTransitionDuration);
        var stripeProgress = Math.Clamp((progress - 0.08f) / 0.68f, 0f, 1f);
        var finalFlash = Math.Clamp((progress - 0.72f) / 0.28f, 0f, 1f);
        var flashPulse = progress <= 0.36f
            ? 1f - Math.Abs((progress / 0.36f * 2f) - 1f)
            : 0f;

        if (flashPulse > 0f)
        {
            using var pulseBrush = new SolidBrush(Color.FromArgb((int)(flashPulse * 170f), Color.White));
            g.FillRectangle(pulseBrush, 0, 0, VirtualWidth, VirtualHeight);
        }

        const int stripeCount = 12;
        var stripeHeight = (int)Math.Ceiling(VirtualHeight / (float)stripeCount);
        var filledHeight = Math.Max(1, (int)Math.Ceiling(stripeHeight * stripeProgress));
        using var stripeBrush = new SolidBrush(Color.FromArgb(240, 252, 252, 255));
        for (var index = 0; index < stripeCount; index++)
        {
            var y = index * stripeHeight;
            var inset = (int)((index % 2 == 0 ? 26f : 12f) * (1f - stripeProgress));
            g.FillRectangle(stripeBrush, inset, y, VirtualWidth - (inset * 2), filledHeight);
        }

        if (pendingEncounter is not null && progress >= 0.36f)
        {
            var messageRect = new Rectangle(124, 386, 392, 42);
            using var shadowBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            g.FillRectangle(shadowBrush, messageRect.X + 4, messageRect.Y + 4, messageRect.Width, messageRect.Height);
            DrawWindow(g, messageRect);
            DrawText(
                g,
                $"{pendingEncounter.Enemy.Name}の けはいがする…",
                Rectangle.Inflate(messageRect, -18, -10),
                smallFont,
                StringAlignment.Center,
                StringAlignment.Center);
        }

        if (finalFlash > 0f)
        {
            using var flashBrush = new SolidBrush(Color.FromArgb((int)(finalFlash * 255f), Color.White));
            g.FillRectangle(flashBrush, 0, 0, VirtualWidth, VirtualHeight);
        }
    }

    private void DrawShopBuy(Graphics g)
    {
        DrawFieldScene(g);

        DrawWindow(g, new Rectangle(32, 34, 242, 126));
        if (shopPhase == ShopPhase.Welcome)
        {
            DrawOption(g, shopPromptCursor == 0, 94, 72, "はい");
            DrawOption(g, shopPromptCursor == 1, 94, 112, "いいえ");
        }
        else
        {
            DrawText(g, "↑↓: しょうひん", new Rectangle(54, 66, 188, 24), smallFont);
            DrawText(g, "ENTER: こうにゅう", new Rectangle(54, 94, 188, 24), smallFont);
            DrawText(g, "ESC: もどる", new Rectangle(54, 122, 188, 24), smallFont);
        }

        DrawWindow(g, new Rectangle(292, 20, 316, 274));
        DrawText(g, "しょうひん", new Rectangle(312, 34, 120, 24), smallFont);
        DrawText(g, "ATK", new Rectangle(446, 34, 46, 24), smallFont, StringAlignment.Center);
        DrawText(g, "G", new Rectangle(500, 34, 56, 24), smallFont, StringAlignment.Center);
        DrawText(g, "OWN", new Rectangle(554, 34, 40, 24), smallFont, StringAlignment.Center);

        for (var i = 0; i < GameContent.ShopCatalog.Length; i++)
        {
            var item = GameContent.ShopCatalog[i];
            var rowY = 68 + (i * 30);
            if (shopPhase == ShopPhase.BuyList && shopItemCursor == i)
            {
                DrawSelectionMarker(g, 304, rowY + 7);
            }

            DrawText(g, item.Name, new Rectangle(328, rowY, 118, 24), smallFont);
            DrawText(g, $"+{item.AttackBonus}", new Rectangle(448, rowY, 40, 24), smallFont, StringAlignment.Center);
            DrawText(g, item.Price.ToString(), new Rectangle(500, rowY, 56, 24), smallFont, StringAlignment.Center);
            DrawText(g, player.GetItemCount(item.Id).ToString(), new Rectangle(554, rowY, 40, 24), smallFont, StringAlignment.Center);
        }

        var quitY = 68 + (GameContent.ShopCatalog.Length * 30);
        if (shopPhase == ShopPhase.BuyList && shopItemCursor == GameContent.ShopCatalog.Length)
        {
            DrawSelectionMarker(g, 304, quitY + 7);
        }

        DrawText(g, "やめる", new Rectangle(328, quitY, 118, 24), smallFont);
        DrawText(g, $"G {player.Gold}", new Rectangle(472, 254, 118, 24), smallFont, StringAlignment.Far);

        DrawWindow(g, new Rectangle(32, 178, 242, 108));
        DrawText(g, $"そうび: {GetEquippedWeaponName()}", new Rectangle(52, 196, 190, 24), smallFont);
        DrawText(g, $"ATK {GetTotalAttack()}  DEF {GetTotalDefense()}", new Rectangle(52, 224, 190, 24), smallFont);
        DrawText(g, $"LV {player.Level}  EXP {GetExperienceSummary()}", new Rectangle(52, 252, 190, 24), smallFont);

        DrawWindow(g, new Rectangle(70, 304, 498, 140));
        DrawText(g, shopMessage, Rectangle.Inflate(new Rectangle(70, 304, 498, 140), -24, -24), smallFont, wrap: true);
    }

    private void DrawFieldScene(Graphics g)
    {
        for (var y = 0; y < map.GetLength(0); y++)
        {
            for (var x = 0; x < map.GetLength(1); x++)
            {
                var tileRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                using var tileBrush = new SolidBrush(GetTileColor(map[y, x]));
                g.FillRectangle(tileBrush, tileRect);
            }
        }

        if (HasNpcOnCurrentMap())
        {
            DrawTileEntity(g, NpcTile, Color.Cyan);
        }

        DrawTileEntity(g, player.TilePosition, Color.White);
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

    private void DrawBattleEnemy(Graphics g, Point center)
    {
        var bobOffset = (int)Math.Round(Math.Sin(frameCounter / 7d) * 3);
        center = new Point(center.X, center.Y + bobOffset);

        using var bodyBrush = new SolidBrush(Color.FromArgb(220, 230, 242));
        using var eyeBrush = new SolidBrush(Color.Black);
        using var outlinePen = new Pen(Color.FromArgb(90, 150, 220), 2);
        using var hornPen = new Pen(Color.FromArgb(220, 230, 242), 3);
        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));

        g.FillEllipse(shadowBrush, center.X - 24, center.Y + 20, 48, 10);

        var body = new Rectangle(center.X - 38, center.Y - 18, 76, 56);
        g.FillEllipse(bodyBrush, body);
        g.DrawEllipse(outlinePen, body);

        g.DrawLine(hornPen, center.X - 18, center.Y - 18, center.X - 30, center.Y - 34);
        g.DrawLine(hornPen, center.X + 18, center.Y - 18, center.X + 30, center.Y - 34);

        g.FillEllipse(eyeBrush, center.X - 16, center.Y, 6, 6);
        g.FillEllipse(eyeBrush, center.X + 10, center.Y, 6, 6);
        g.DrawLine(outlinePen, center.X - 12, center.Y + 18, center.X + 12, center.Y + 18);
    }

    private void DrawTileEntity(Graphics g, Point tile, Color color)
    {
        var rect = new Rectangle(tile.X * TileSize + 4, tile.Y * TileSize + 4, TileSize - 8, TileSize - 8);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);
    }

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
        using var brush = new SolidBrush(Color.White);
        g.DrawString(text, fontOverride ?? uiFont, brush, x, y);
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
        using var brush = new SolidBrush(Color.White);
        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = lineAlignment,
            Trimming = wrap ? StringTrimming.None : StringTrimming.EllipsisCharacter
        };

        if (!wrap)
        {
            format.FormatFlags |= StringFormatFlags.NoWrap;
        }

        g.DrawString(text, fontOverride ?? uiFont, brush, bounds, format);
    }

    private void DrawMenuBackdrop(Graphics g)
    {
        using var gradient = new LinearGradientBrush(
            new Rectangle(0, 0, VirtualWidth, VirtualHeight),
            Color.Black,
            Color.FromArgb(0, 10, 22),
            90f);
        using var scanlinePen = new Pen(Color.FromArgb(24, 38, 80));
        using var sideGlowBrush = new SolidBrush(Color.FromArgb(14, 0, 80, 255));

        g.FillRectangle(gradient, 0, 0, VirtualWidth, VirtualHeight);

        for (var y = 0; y < VirtualHeight; y += 4)
        {
            g.DrawLine(scanlinePen, 0, y, VirtualWidth, y);
        }

        g.FillRectangle(sideGlowBrush, 0, 0, 18, VirtualHeight);
        g.FillRectangle(sideGlowBrush, VirtualWidth - 18, 0, 18, VirtualHeight);
    }

    private void DrawTitleText(Graphics g, string text, int x, int y)
    {
        using var shadowBrush = new SolidBrush(Color.FromArgb(44, 106, 255));
        using var mainBrush = new SolidBrush(Color.FromArgb(238, 244, 255));

        g.DrawString(text, uiFont, shadowBrush, x + 4, y + 4);
        g.DrawString(text, uiFont, mainBrush, x, y);
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
