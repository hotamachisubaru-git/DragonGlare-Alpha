using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawBattle(Graphics g)
    {
        DrawMenuBackdrop(g);

        var statusRect = new Rectangle(44, 26, 236, 124);
        var statusInnerLeft = statusRect.X + 22;
        var statusInnerRight = statusRect.Right - 22;
        var firstLineY = statusRect.Y + 16;
        var hpLineY = firstLineY + 26;
        var mpLineY = firstLineY + 52;
        var statLineY = firstLineY + 78;
        DrawWindow(g, statusRect);
        DrawText(g, GetDisplayPlayerName(), new Rectangle(statusInnerLeft, firstLineY, 96, 24), smallFont);
        DrawText(g, $"Lv.{player.Level}", new Rectangle(statusInnerRight - 80, firstLineY, 80, 24), smallFont, StringAlignment.Far);
        DrawText(g, "HP", new Rectangle(statusInnerLeft, hpLineY, 32, 24), smallFont);
        DrawText(g, $"{player.CurrentHp}/{player.MaxHp}", new Rectangle(statusInnerLeft + 48, hpLineY, statusInnerRight - (statusInnerLeft + 48), 24), smallFont, StringAlignment.Far);
        DrawText(g, "MP", new Rectangle(statusInnerLeft, mpLineY, 32, 24), smallFont);
        DrawText(g, $"{player.CurrentMp}/{player.MaxMp}", new Rectangle(statusInnerLeft + 48, mpLineY, statusInnerRight - (statusInnerLeft + 48), 24), smallFont, StringAlignment.Far);
        DrawText(g, "ATK", new Rectangle(statusInnerLeft, statLineY, 48, 24), smallFont);
        DrawText(g, GetTotalAttack().ToString(), new Rectangle(statusInnerLeft + 48, statLineY, 48, 24), smallFont, StringAlignment.Far);
        DrawText(g, "DEF", new Rectangle(statusInnerLeft + 96, statLineY, 48, 24), smallFont);
        DrawText(g, GetTotalDefense().ToString(), new Rectangle(statusInnerLeft + 144, statLineY, 48, 24), smallFont, StringAlignment.Far);

        var enemyInfoRect = new Rectangle(292, 26, 276, 76);
        DrawWindow(g, enemyInfoRect);
        DrawText(g, currentEncounter?.Enemy.Name ?? "まもの", new Rectangle(enemyInfoRect.X + 18, enemyInfoRect.Y + 14, 236, 24), smallFont);
        if (currentEncounter is not null)
        {
            DrawText(g, $"HP {currentEncounter.CurrentHp}/{currentEncounter.Enemy.MaxHp}", new Rectangle(enemyInfoRect.X + 18, enemyInfoRect.Y + 38, 128, 24), smallFont);
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

        if (ShouldDrawBattleEnemySprite())
        {
            DrawBattleEnemy(g, new Point(198, 228));
        }

        var messageRect = new Rectangle(statusRect.X, 272, actionListRect.Right - statusRect.X, 186);
        DrawWindow(g, messageRect);
        DrawText(
            g,
            battleMessage,
            new Rectangle(messageRect.X + 28, messageRect.Y + 24, messageRect.Width - 56, messageRect.Height - 48),
            smallFont,
            wrap: true);

        if (battleFlowState != BattleFlowState.CommandSelection)
        {
            DrawText(g, "ENTER / Z / X: つぎへ", new Rectangle(messageRect.X + 28, messageRect.Bottom - 34, messageRect.Width - 56, 20), smallFont, StringAlignment.Far);
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
}
