using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void DrawShopBuy(Graphics g)
    {
        DrawFieldScene(g);

        var shopHelpRect = new Rectangle(32, 20, 242, 112);
        var shopListRect = new Rectangle(304, 20, 316, 274);
        var shopInfoRect = new Rectangle(32, 178, 242, 108);

        DrawWindow(g, shopHelpRect);
        if (shopPhase == ShopPhase.Welcome)
        {
            DrawOption(g, shopPromptCursor == 0, 94, 58, "はい");
            DrawOption(g, shopPromptCursor == 1, 94, 94, "いいえ");
        }
        else
        {
            DrawText(g, "↑↓: しょうひん", new Rectangle(54, 50, 188, 24), smallFont);
            DrawText(g, "ENTER: こうにゅう", new Rectangle(54, 78, 188, 24), smallFont);
            DrawText(g, "ESC: もどる", new Rectangle(54, 106, 188, 24), smallFont);
        }

        DrawWindow(g, shopListRect);
        DrawText(g, "しょうひん", new Rectangle(shopListRect.X + 20, 34, 120, 24), smallFont);
        DrawText(g, "ATK", new Rectangle(shopListRect.X + 154, 34, 46, 24), smallFont, StringAlignment.Center);
        DrawText(g, "G", new Rectangle(shopListRect.X + 208, 34, 56, 24), smallFont, StringAlignment.Center);
        DrawText(g, "OWN", new Rectangle(shopListRect.X + 262, 34, 40, 24), smallFont, StringAlignment.Center);

        for (var i = 0; i < GameContent.ShopCatalog.Length; i++)
        {
            var item = GameContent.ShopCatalog[i];
            var rowY = 68 + (i * 30);
            if (shopPhase == ShopPhase.BuyList && shopItemCursor == i)
            {
                DrawSelectionMarker(g, shopListRect.X + 12, rowY + 7);
            }

            DrawText(g, item.Name, new Rectangle(shopListRect.X + 36, rowY, 118, 24), smallFont);
            DrawText(g, $"+{item.AttackBonus}", new Rectangle(shopListRect.X + 156, rowY, 40, 24), smallFont, StringAlignment.Center);
            DrawText(g, item.Price.ToString(), new Rectangle(shopListRect.X + 208, rowY, 56, 24), smallFont, StringAlignment.Center);
            DrawText(g, player.GetItemCount(item.Id).ToString(), new Rectangle(shopListRect.X + 262, rowY, 40, 24), smallFont, StringAlignment.Center);
        }

        var quitY = 68 + (GameContent.ShopCatalog.Length * 30);
        if (shopPhase == ShopPhase.BuyList && shopItemCursor == GameContent.ShopCatalog.Length)
        {
            DrawSelectionMarker(g, shopListRect.X + 12, quitY + 7);
        }

        DrawText(g, "やめる", new Rectangle(shopListRect.X + 36, quitY, 118, 24), smallFont);
        DrawText(g, $"G {player.Gold}", new Rectangle(shopListRect.X + 180, 254, 118, 24), smallFont, StringAlignment.Far);

        DrawWindow(g, shopInfoRect);
        DrawText(g, "そうび:", shopInfoRect.X + 20, 196, smallFont);
        DrawText(g, GetEquippedWeaponName(), new Rectangle(shopInfoRect.X + 94, 196, 126, 24), smallFont, StringAlignment.Far);
        DrawText(g, $"ATK {GetTotalAttack()}  DEF {GetTotalDefense()}", new Rectangle(52, 224, 190, 24), smallFont);
        DrawText(g, $"LV {player.Level}  EXP {GetExperienceSummary()}", new Rectangle(52, 252, 190, 24), smallFont);

        DrawWindow(g, new Rectangle(70, 304, 498, 140));
        DrawText(g, shopMessage, Rectangle.Inflate(new Rectangle(70, 304, 498, 140), -24, -24), smallFont, wrap: true);
    }
}
