using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Domain.Player;

public sealed record ArmorDefinition(
    string Id,
    string Name,
    int Price,
    int DefenseBonus) : IEquipmentDefinition
{
    public EquipmentSlot Slot => EquipmentSlot.Armor;

    public int AttackBonus => 0;
}
