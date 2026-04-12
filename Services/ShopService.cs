using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;

namespace DragonGlareAlpha.Services;

public sealed class ShopService
{
    public ShopPurchaseResult PurchaseEquipment(
        PlayerProgress player,
        IEquipmentDefinition equipment,
        WeaponDefinition? currentWeapon,
        ArmorDefinition? currentArmor)
    {
        if (player.Gold < equipment.Price)
        {
            return new ShopPurchaseResult(false, false, "＊「おかねが たりないね。」");
        }

        player.Gold -= equipment.Price;
        player.AddItem(equipment.Id);

        var shouldEquip = false;
        switch (equipment.Slot)
        {
            case EquipmentSlot.Weapon:
                shouldEquip = currentWeapon is null || equipment.AttackBonus > currentWeapon.AttackBonus;
                if (shouldEquip)
                {
                    player.EquippedWeaponId = equipment.Id;
                }

                break;
            case EquipmentSlot.Armor:
                shouldEquip = currentArmor is null || equipment.DefenseBonus > currentArmor.DefenseBonus;
                if (shouldEquip)
                {
                    player.EquippedArmorId = equipment.Id;
                }

                break;
        }

        var message = shouldEquip
            ? $"＊「{equipment.Name}を かった！\n　さっそく そうびしたぜ。」"
            : $"＊「{equipment.Name}を かった！\n　もちものに いれておくよ。」";

        return new ShopPurchaseResult(true, shouldEquip, message);
    }
}

public sealed record ShopPurchaseResult(bool Success, bool Equipped, string Message);
