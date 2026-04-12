using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;

namespace DragonGlareAlpha.Persistence;

public sealed class SaveData
{
    public int Version { get; set; } = 7;

    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

    public string Language { get; set; } = "ja";

    public string Name { get; set; } = string.Empty;

    public int SlotNumber { get; set; }

    public FieldMapId CurrentFieldMap { get; set; } = FieldMapId.Hub;

    public int PlayerX { get; set; }

    public int PlayerY { get; set; }

    public int Level { get; set; }

    public int Experience { get; set; }

    public int MaxHp { get; set; }

    public int CurrentHp { get; set; }

    public int MaxMp { get; set; }

    public int CurrentMp { get; set; }

    public int BaseAttack { get; set; }

    public int BaseDefense { get; set; }

    public int Gold { get; set; }

    public string? EquippedWeaponId { get; set; }

    public string? EquippedArmorId { get; set; }

    public List<InventoryEntry> Inventory { get; set; } = [];

    public string Signature { get; set; } = string.Empty;
}
