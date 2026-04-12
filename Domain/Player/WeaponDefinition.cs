using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Domain.Player;

public sealed record WeaponDefinition(
    string Id,
    string Name,
    int Price,
    int AttackBonus) : IEquipmentDefinition
{
    public EquipmentSlot Slot => EquipmentSlot.Weapon;

    public int DefenseBonus => 0;
}
