using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawShopBuy(Graphics g)
    {
        DrawFieldScene(g);
        const int itemRowHeight = 22;
        const int listStartY = 62;

        var shopHelpRect = new Rectangle(32, 20, 242, 112);
        var shopListRect = new Rectangle(304, 20, 316, 274);
        var shopInfoRect = new Rectangle(32, 152, 242, 112);
        var shopMessageRect = new Rectangle(70, 304, 498, 140);
        var visibleEntries = GetShopVisibleEntries();

        DrawWindow(g, shopHelpRect);
        if (shopPhase == ShopPhase.Welcome)
        {
            DrawOption(g, shopPromptCursor == 0, 94, 58, "はい");
            DrawOption(g, shopPromptCursor == 1, 94, 94, "いいえ");
        }
        else
        {
            DrawText(g, "↑↓: せんたく", new Rectangle(54, 50, 188, 24), smallFont);
            DrawText(g, "Z: こうにゅう", new Rectangle(54, 78, 188, 24), smallFont);
            DrawText(g, "ESC(X): もどる", new Rectangle(54, 106, 188, 24), smallFont);
        }

        DrawWindow(g, shopListRect);
        DrawText(g, "いちらん", new Rectangle(shopListRect.X + 20, 34, 120, 24), smallFont);
        DrawText(g, "ATK", new Rectangle(shopListRect.X + 142, 34, 38, 24), smallFont, StringAlignment.Center);
        DrawText(g, "DEF", new Rectangle(shopListRect.X + 180, 34, 38, 24), smallFont, StringAlignment.Center);
        DrawText(g, "G", new Rectangle(shopListRect.X + 220, 34, 44, 24), smallFont, StringAlignment.Center);
        DrawText(g, "OWN", new Rectangle(shopListRect.X + 266, 34, 34, 24), smallFont, StringAlignment.Center);

        for (var i = 0; i < visibleEntries.Count; i++)
        {
            var entry = visibleEntries[i];
            var rowY = listStartY + (i * itemRowHeight);
            if (shopPhase == ShopPhase.BuyList && shopItemCursor == i)
            {
                DrawSelectionMarker(g, shopListRect.X + 12, rowY + 7);
            }

            if (entry.Type == ShopMenuEntryType.Item && entry.Item is not null)
            {
                var item = entry.Item;
                DrawText(g, item.Name, new Rectangle(shopListRect.X + 36, rowY, 106, 20), smallFont);
                DrawText(g, item.AttackBonus > 0 ? $"+{item.AttackBonus}" : "-", new Rectangle(shopListRect.X + 142, rowY, 38, 20), smallFont, StringAlignment.Center);
                DrawText(g, item.DefenseBonus > 0 ? $"+{item.DefenseBonus}" : "-", new Rectangle(shopListRect.X + 180, rowY, 38, 20), smallFont, StringAlignment.Center);
                DrawText(g, item.Price.ToString(), new Rectangle(shopListRect.X + 220, rowY, 44, 20), smallFont, StringAlignment.Center);
                DrawText(g, player.GetItemCount(item.Id).ToString(), new Rectangle(shopListRect.X + 266, rowY, 34, 20), smallFont, StringAlignment.Center);
                continue;
            }

            DrawText(g, entry.Label, new Rectangle(shopListRect.X + 36, rowY, 118, 20), smallFont);
        }

        DrawText(g, $"{shopPageIndex + 1}/{GetShopPageCount()}", new Rectangle(shopListRect.X + 20, shopListRect.Bottom - 28, 60, 24), smallFont);
        DrawText(g, $"G {player.Gold}", new Rectangle(shopListRect.X + 176, shopListRect.Bottom - 28, 122, 24), smallFont, StringAlignment.Far);

        DrawWindow(g, shopInfoRect);
        DrawText(g, "ぶき:", shopInfoRect.X + 20, shopInfoRect.Y + 14, smallFont);
        DrawText(g, GetEquippedWeaponName(), new Rectangle(shopInfoRect.X + 94, shopInfoRect.Y + 14, 126, 20), smallFont, StringAlignment.Far);
        DrawText(g, "ぼうぐ:", shopInfoRect.X + 20, shopInfoRect.Y + 36, smallFont);
        DrawText(g, GetEquippedArmorName(), new Rectangle(shopInfoRect.X + 94, shopInfoRect.Y + 36, 126, 20), smallFont, StringAlignment.Far);
        DrawText(g, $"ATK {GetTotalAttack()}  DEF {GetTotalDefense()}", new Rectangle(shopInfoRect.X + 20, shopInfoRect.Y + 58, 196, 20), smallFont);
        DrawText(g, $"LV {player.Level}", new Rectangle(shopInfoRect.X + 20, shopInfoRect.Y + 80, 58, 20), smallFont);
        DrawText(g, $"EXP {GetExperienceSummary()}", new Rectangle(shopInfoRect.X + 84, shopInfoRect.Y + 80, 136, 20), smallFont, StringAlignment.Far);

        DrawWindow(g, shopMessageRect);
        DrawText(g, shopMessage, Rectangle.Inflate(shopMessageRect, -24, -24), smallFont, wrap: true);
    }
}
