using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawBattle(Graphics g)
    {
        DrawBattleBackdrop(g);

        var statusRect = new Rectangle(22, 18, 148, 126);
        DrawWindow(g, statusRect);
        DrawBattleStatusWindow(g, statusRect);

        if (ShouldDrawBattleEnemySprite())
        {
            DrawBattleEnemy(g, new Point(320, 226));
        }

        DrawBattleLowerUi(g);
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
            g.FillRectangle(pulseBrush, 0, 0, UiCanvas.VirtualWidth, UiCanvas.VirtualHeight);
        }

        const int stripeCount = 12;
        var stripeHeight = (int)Math.Ceiling(UiCanvas.VirtualHeight / (float)stripeCount);
        var filledHeight = Math.Max(1, (int)Math.Ceiling(stripeHeight * stripeProgress));
        using var stripeBrush = new SolidBrush(Color.FromArgb(240, 252, 252, 255));
        for (var index = 0; index < stripeCount; index++)
        {
            var y = index * stripeHeight;
            var inset = (int)((index % 2 == 0 ? 26f : 12f) * (1f - stripeProgress));
            g.FillRectangle(stripeBrush, inset, y, UiCanvas.VirtualWidth - (inset * 2), filledHeight);
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
            g.FillRectangle(flashBrush, 0, 0, UiCanvas.VirtualWidth, UiCanvas.VirtualHeight);
        }
    }

    private void DrawBattleEnemy(Graphics g, Point center)
    {
        var bobOffset = (int)Math.Round(Math.Sin(frameCounter / 7d) * 3);
        center = new Point(center.X, center.Y + bobOffset);

        if (string.Equals(currentEncounter?.Enemy.Id, "moss_toad", StringComparison.Ordinal))
        {
            DrawMossToadEnemy(g, center);
            return;
        }

        DrawDefaultBattleEnemy(g, center);
    }

    private bool ShouldDrawBattleEnemySprite()
    {
        if (currentEncounter is null || currentEncounter.CurrentHp <= 0)
        {
            return false;
        }

        if (enemyHitFlashFramesRemaining <= 0)
        {
            return true;
        }

        return ((enemyHitFlashFramesRemaining - 1) / 2) % 2 == 0;
    }

    private void DrawBattleStatusWindow(Graphics g, Rectangle rect)
    {
        var contentRect = Rectangle.Inflate(rect, -16, -12);
        var lineHeight = 20;
        var lineWidth = contentRect.Width;

        DrawText(g, GetDisplayPlayerName(), new Rectangle(contentRect.X, contentRect.Y, lineWidth, 20), smallFont);
        DrawText(g, $"Lv.{player.Level}", new Rectangle(contentRect.X, contentRect.Y, lineWidth, 20), smallFont, StringAlignment.Far);
        DrawText(g, $"HP {player.CurrentHp}/{player.MaxHp}", new Rectangle(contentRect.X, contentRect.Y + lineHeight, lineWidth, 20), smallFont);
        DrawText(g, $"MP {player.CurrentMp}/{player.MaxMp}", new Rectangle(contentRect.X, contentRect.Y + (lineHeight * 2), lineWidth, 20), smallFont);
        DrawText(g, $"ATK {GetTotalAttack()}", new Rectangle(contentRect.X, contentRect.Y + (lineHeight * 3), lineWidth, 20), smallFont);
        DrawText(g, $"DEF {GetTotalDefense()}", new Rectangle(contentRect.X, contentRect.Y + (lineHeight * 4), lineWidth, 20), smallFont);
    }

    private void DrawBattleLowerUi(Graphics g)
    {
        if (battleFlowState == BattleFlowState.CommandSelection)
        {
            var messageStripRect = new Rectangle(20, 296, 600, 84);
            DrawWindow(g, messageStripRect);
            DrawText(
                g,
                battleMessage,
                new Rectangle(messageStripRect.X + 18, messageStripRect.Y + 10, messageStripRect.Width - 36, messageStripRect.Height - 20),
                smallFont,
                wrap: true);

            var commandRect = new Rectangle(20, 384, 254, 78);
            var enemyPanelRect = new Rectangle(282, 384, 338, 78);
            DrawWindow(g, commandRect);
            DrawWindow(g, enemyPanelRect);
            DrawBattleCommandWindow(g, commandRect);
            DrawBattleTargetWindow(g, enemyPanelRect);
            return;
        }

        var messageRect = new Rectangle(20, 328, 600, 134);
        DrawWindow(g, messageRect);
        DrawText(
            g,
            battleMessage,
            new Rectangle(messageRect.X + 24, messageRect.Y + 18, messageRect.Width - 48, messageRect.Height - 52),
            smallFont,
            wrap: true);
        DrawText(
            g,
            selectedLanguage == UiLanguage.English ? "ENTER / Z / X: NEXT" : "ENTER / Z / X: つぎへ",
            new Rectangle(messageRect.X + 24, messageRect.Bottom - 30, messageRect.Width - 48, 20),
            smallFont,
            StringAlignment.Far);
    }

    private void DrawBattleCommandWindow(Graphics g, Rectangle rect)
    {
        var titleRect = new Rectangle(rect.X + 16, rect.Y + 8, rect.Width - 32, 20);
        DrawText(g, GetDisplayPlayerName(), titleRect, smallFont, StringAlignment.Center);
        DrawBattleWindowSeparator(g, rect.X + 16, rect.Y + 28, rect.Width - 32);

        var commandGridRect = new Rectangle(rect.X + 18, rect.Y + 34, rect.Width - 36, rect.Height - 40);
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
                if (battleCursorRow == row && battleCursorColumn == column)
                {
                    DrawBattleSelectionPointer(g, cellRect.X + 2, cellRect.Y + (cellRect.Height / 2) - 7);
                }

                DrawText(
                    g,
                    GameContent.BattleCommandLabels[row, column],
                    new Rectangle(cellRect.X + 22, cellRect.Y - 1, cellRect.Width - 22, cellRect.Height),
                    smallFont,
                    StringAlignment.Near,
                    StringAlignment.Center);
            }
        }
    }

    private void DrawBattleTargetWindow(Graphics g, Rectangle rect)
    {
        var targetName = currentEncounter?.Enemy.Name ?? "まもの";
        var countLabel = selectedLanguage == UiLanguage.English ? "x1" : "1匹";
        var contentRect = Rectangle.Inflate(rect, -18, -12);

        DrawText(g, targetName, new Rectangle(contentRect.X, contentRect.Y, contentRect.Width - 60, 20), smallFont);
        DrawText(g, countLabel, new Rectangle(contentRect.Right - 64, contentRect.Y, 64, 20), smallFont, StringAlignment.Far);
        DrawBattleWindowSeparator(g, contentRect.X, contentRect.Y + 20, contentRect.Width);

        if (currentEncounter is null)
        {
            return;
        }

        DrawText(
            g,
            $"HP {currentEncounter.CurrentHp}/{currentEncounter.Enemy.MaxHp}",
            new Rectangle(contentRect.X, contentRect.Y + 28, contentRect.Width, 20),
            smallFont);
    }

    private static void DrawBattleWindowSeparator(Graphics g, int x, int y, int width)
    {
        using var shadowPen = new Pen(Color.FromArgb(24, 24, 40));
        using var linePen = new Pen(Color.FromArgb(132, 206, 255));
        g.DrawLine(shadowPen, x, y + 1, x + width, y + 1);
        g.DrawLine(linePen, x, y, x + width, y);
    }

    private void DrawBattleSelectionPointer(Graphics g, int x, int y)
    {
        if ((frameCounter / 18) % 2 == 1)
        {
            return;
        }

        using var shadowBrush = new SolidBrush(Color.FromArgb(44, 22, 30));
        using var baseBrush = new SolidBrush(Color.White);
        g.FillRectangle(shadowBrush, x + 2, y + 2, 14, 12);
        g.FillRectangle(baseBrush, x, y + 4, 6, 4);
        g.FillRectangle(baseBrush, x + 4, y + 2, 4, 8);
        g.FillRectangle(baseBrush, x + 8, y, 4, 12);
        g.FillRectangle(baseBrush, x + 12, y + 2, 2, 8);
    }

    private void DrawBattleBackdrop(Graphics g)
    {
        DrawBattleStoneWall(g, new Rectangle(0, 0, UiCanvas.VirtualWidth, 244));
        DrawBattlePlatform(g, new Rectangle(0, 244, UiCanvas.VirtualWidth, 68));
        DrawBattleCarpet(g, new Rectangle(0, 312, UiCanvas.VirtualWidth, UiCanvas.VirtualHeight - 312));
    }

    private static void DrawBattleStoneWall(Graphics g, Rectangle rect)
    {
        using var mortarBrush = new SolidBrush(Color.FromArgb(28, 24, 42));
        using var brickBrushA = new SolidBrush(Color.FromArgb(90, 82, 112));
        using var brickBrushB = new SolidBrush(Color.FromArgb(74, 67, 94));
        using var brickBrushC = new SolidBrush(Color.FromArgb(60, 54, 80));
        using var trimBrush = new SolidBrush(Color.FromArgb(178, 108, 44));
        using var trimDarkBrush = new SolidBrush(Color.FromArgb(94, 54, 24));

        g.FillRectangle(mortarBrush, rect);

        const int brickWidth = 54;
        const int brickHeight = 20;
        for (var row = 0; row <= rect.Height / brickHeight; row++)
        {
            var y = rect.Y + (row * brickHeight);
            var startX = rect.X - ((row % 2) * (brickWidth / 2));
            var brush = (row % 3) switch
            {
                0 => brickBrushA,
                1 => brickBrushB,
                _ => brickBrushC
            };

            for (var x = startX; x < rect.Right; x += brickWidth)
            {
                g.FillRectangle(brush, x + 1, y + 1, brickWidth - 2, brickHeight - 2);
            }
        }

        DrawBattlePillar(g, new Rectangle(86, 28, 34, 176));
        DrawBattlePillar(g, new Rectangle(520, 28, 34, 176));
        DrawBattleArchWindow(g, new Rectangle(166, 52, 82, 84));
        DrawBattleArchWindow(g, new Rectangle(390, 52, 82, 84));

        g.FillRectangle(trimDarkBrush, rect.X, rect.Bottom - 56, rect.Width, 10);
        g.FillRectangle(trimBrush, rect.X, rect.Bottom - 52, rect.Width, 4);
        g.FillRectangle(trimBrush, rect.X, rect.Bottom - 40, rect.Width, 2);
        for (var x = rect.X + 24; x < rect.Right - 24; x += 24)
        {
            g.FillRectangle(trimBrush, x, rect.Bottom - 50, 10, 2);
            g.FillRectangle(trimBrush, x + 6, rect.Bottom - 46, 10, 2);
        }
    }

    private static void DrawBattleArchWindow(Graphics g, Rectangle rect)
    {
        using var frameBrush = new SolidBrush(Color.FromArgb(116, 106, 126));
        using var frameDarkBrush = new SolidBrush(Color.FromArgb(52, 48, 64));
        using var glassBrush = new SolidBrush(Color.FromArgb(44, 60, 116));
        using var barPen = new Pen(Color.FromArgb(180, 108, 42), 4);

        g.FillRectangle(frameDarkBrush, rect.X - 4, rect.Y + 10, rect.Width + 8, rect.Height + 10);
        g.FillRectangle(frameBrush, rect.X, rect.Y + 14, rect.Width, rect.Height);
        g.FillEllipse(frameBrush, rect.X, rect.Y, rect.Width, rect.Width);

        var innerRect = Rectangle.Inflate(rect, -10, -10);
        g.FillRectangle(glassBrush, innerRect.X, innerRect.Y + 16, innerRect.Width, innerRect.Height - 6);
        g.FillEllipse(glassBrush, innerRect.X, innerRect.Y - 2, innerRect.Width, innerRect.Width);

        g.DrawLine(barPen, innerRect.X + 8, innerRect.Bottom - 14, innerRect.Right - 8, innerRect.Y + 26);
        g.DrawLine(barPen, innerRect.X + 14, innerRect.Y + 30, innerRect.Right - 14, innerRect.Bottom - 18);
    }

    private static void DrawBattlePillar(Graphics g, Rectangle rect)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(70, 66, 84));
        using var edgeBrush = new SolidBrush(Color.FromArgb(102, 96, 122));
        using var trimBrush = new SolidBrush(Color.FromArgb(180, 108, 42));

        g.FillRectangle(bodyBrush, rect);
        g.FillRectangle(edgeBrush, rect.X + 4, rect.Y, 6, rect.Height);
        g.FillRectangle(edgeBrush, rect.Right - 10, rect.Y, 6, rect.Height);
        g.FillRectangle(trimBrush, rect.X - 6, rect.Y + 22, rect.Width + 12, 4);
        g.FillRectangle(trimBrush, rect.X - 6, rect.Bottom - 34, rect.Width + 12, 4);
        g.FillRectangle(edgeBrush, rect.X - 10, rect.Bottom - 20, rect.Width + 20, 16);
    }

    private static void DrawBattlePlatform(Graphics g, Rectangle rect)
    {
        using var baseBrush = new SolidBrush(Color.FromArgb(56, 26, 48));
        using var stripeBrush = new SolidBrush(Color.FromArgb(72, 34, 60));
        using var highlightBrush = new SolidBrush(Color.FromArgb(170, 108, 42));
        using var shadowBrush = new SolidBrush(Color.FromArgb(40, 12, 26));

        g.FillRectangle(baseBrush, rect);
        for (var y = rect.Y + 6; y < rect.Bottom; y += 10)
        {
            g.FillRectangle(stripeBrush, rect.X, y, rect.Width, 2);
        }

        g.FillRectangle(shadowBrush, rect.X, rect.Y, rect.Width, 14);
        g.FillRectangle(highlightBrush, rect.X, rect.Bottom - 12, rect.Width, 4);
        g.FillRectangle(highlightBrush, rect.X, rect.Bottom - 4, rect.Width, 2);
    }

    private static void DrawBattleCarpet(Graphics g, Rectangle rect)
    {
        using var carpetBrush = new SolidBrush(Color.FromArgb(124, 28, 64));
        using var carpetShadeBrush = new SolidBrush(Color.FromArgb(94, 18, 50));
        using var trimBrush = new SolidBrush(Color.FromArgb(214, 156, 54));
        using var trimShadeBrush = new SolidBrush(Color.FromArgb(120, 72, 20));

        g.FillRectangle(carpetBrush, rect);

        for (var y = rect.Y + 10; y < rect.Bottom; y += 10)
        {
            g.FillRectangle(carpetShadeBrush, rect.X, y, rect.Width, 2);
        }

        var topTrimRect = new Rectangle(rect.X, rect.Y + 6, rect.Width, 22);
        g.FillRectangle(trimShadeBrush, topTrimRect);
        g.FillRectangle(trimBrush, topTrimRect.X, topTrimRect.Y + 4, topTrimRect.Width, 2);
        g.FillRectangle(trimBrush, topTrimRect.X, topTrimRect.Bottom - 6, topTrimRect.Width, 2);

        for (var x = rect.X + 12; x < rect.Right - 20; x += 24)
        {
            g.FillRectangle(trimBrush, x + 6, topTrimRect.Y + 8, 6, 6);
            g.FillRectangle(trimShadeBrush, x + 8, topTrimRect.Y + 10, 2, 2);
            g.FillRectangle(trimBrush, x + 2, topTrimRect.Y + 10, 2, 2);
            g.FillRectangle(trimBrush, x + 14, topTrimRect.Y + 10, 2, 2);
            g.FillRectangle(trimBrush, x + 8, topTrimRect.Y + 6, 2, 2);
            g.FillRectangle(trimBrush, x + 8, topTrimRect.Y + 14, 2, 2);
        }
    }

    private static void DrawDefaultBattleEnemy(Graphics g, Point center)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(216, 228, 240));
        using var outlinePen = new Pen(Color.FromArgb(88, 136, 204), 2);
        using var hornPen = new Pen(Color.FromArgb(216, 228, 240), 4);
        using var eyeBrush = new SolidBrush(Color.Black);
        using var shadowBrush = new SolidBrush(Color.FromArgb(88, 0, 0, 0));

        g.FillEllipse(shadowBrush, center.X - 42, center.Y + 30, 84, 16);

        var body = new Rectangle(center.X - 58, center.Y - 14, 116, 78);
        g.FillEllipse(bodyBrush, body);
        g.DrawEllipse(outlinePen, body);

        g.DrawLine(hornPen, center.X - 22, center.Y - 10, center.X - 40, center.Y - 36);
        g.DrawLine(hornPen, center.X + 22, center.Y - 10, center.X + 40, center.Y - 36);
        g.FillEllipse(eyeBrush, center.X - 22, center.Y + 12, 8, 8);
        g.FillEllipse(eyeBrush, center.X + 14, center.Y + 12, 8, 8);
        g.DrawLine(outlinePen, center.X - 18, center.Y + 42, center.X + 18, center.Y + 42);
    }

    private static void DrawMossToadEnemy(Graphics g, Point center)
    {
        using var shadowBrush = new SolidBrush(Color.FromArgb(88, 0, 0, 0));
        using var armBrush = new SolidBrush(Color.FromArgb(126, 156, 38));
        using var bodyBrush = new SolidBrush(Color.FromArgb(144, 176, 42));
        using var bellyBrush = new SolidBrush(Color.FromArgb(172, 72, 126));
        using var outlinePen = new Pen(Color.FromArgb(76, 94, 18), 3);
        using var tongueBrush = new SolidBrush(Color.FromArgb(238, 74, 118));
        using var mouthBrush = new SolidBrush(Color.FromArgb(64, 18, 26));
        using var eyeBrush = new SolidBrush(Color.FromArgb(248, 242, 228));
        using var pupilBrush = new SolidBrush(Color.Black);

        g.FillEllipse(shadowBrush, center.X - 58, center.Y + 38, 116, 18);

        g.FillEllipse(armBrush, center.X - 74, center.Y + 10, 40, 18);
        g.FillEllipse(armBrush, center.X + 32, center.Y + 10, 40, 18);
        g.FillEllipse(armBrush, center.X - 54, center.Y + 42, 28, 18);
        g.FillEllipse(armBrush, center.X + 28, center.Y + 42, 28, 18);

        var body = new Rectangle(center.X - 54, center.Y - 2, 108, 70);
        g.FillEllipse(bodyBrush, body);
        g.DrawEllipse(outlinePen, body);

        var head = new Rectangle(center.X - 36, center.Y - 36, 72, 52);
        g.FillEllipse(bodyBrush, head);
        g.DrawEllipse(outlinePen, head);

        var mouth = new Rectangle(center.X - 28, center.Y - 10, 56, 30);
        g.FillEllipse(mouthBrush, mouth);
        g.FillEllipse(tongueBrush, center.X - 2, center.Y + 6, 20, 26);
        g.FillEllipse(bellyBrush, center.X - 34, center.Y + 26, 68, 34);

        g.FillEllipse(eyeBrush, center.X - 22, center.Y - 28, 16, 16);
        g.FillEllipse(eyeBrush, center.X + 6, center.Y - 28, 16, 16);
        g.FillEllipse(pupilBrush, center.X - 18, center.Y - 24, 6, 6);
        g.FillEllipse(pupilBrush, center.X + 10, center.Y - 24, 6, 6);
    }
}
