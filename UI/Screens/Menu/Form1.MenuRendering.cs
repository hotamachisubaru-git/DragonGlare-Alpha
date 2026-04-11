using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Persistence;

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
}
