using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Domain.Player;

public interface IEquipmentDefinition
{
    string Id { get; }

    string Name { get; }

    int Price { get; }

    EquipmentSlot Slot { get; }

    int AttackBonus { get; }

    int DefenseBonus { get; }
}
