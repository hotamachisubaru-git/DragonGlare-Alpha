using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Security;

namespace DragonGlareAlpha.Domain.Player;

public sealed class PlayerProgress
{
    public const int MaxLevelValue = 99;
    public const int MaxVitalValue = 999;
    public const int MaxGoldValue = 99999;
    private readonly ProtectedInt level = new(1);
    private readonly ProtectedInt experience = new();
    private readonly ProtectedInt maxHp = new(20);
    private readonly ProtectedInt currentHp = new(20);
    private readonly ProtectedInt maxMp = new(2);
    private readonly ProtectedInt currentMp = new(2);
    private readonly ProtectedInt baseAttack = new(5);
    private readonly ProtectedInt baseDefense = new(3);
    private readonly ProtectedInt gold = new(220);

    public string Name { get; set; } = string.Empty;

    public UiLanguage Language { get; set; } = UiLanguage.Japanese;

    public Point TilePosition { get; set; }

    public int Level
    {
        get => level.Value;
        set => level.Value = value;
    }

    public int Experience
    {
        get => experience.Value;
        set => experience.Value = value;
    }

    public int MaxHp
    {
        get => maxHp.Value;
        set => maxHp.Value = value;
    }

    public int CurrentHp
    {
        get => currentHp.Value;
        set => currentHp.Value = value;
    }

    public int MaxMp
    {
        get => maxMp.Value;
        set => maxMp.Value = value;
    }

    public int CurrentMp
    {
        get => currentMp.Value;
        set => currentMp.Value = value;
    }

    public int BaseAttack
    {
        get => baseAttack.Value;
        set => baseAttack.Value = value;
    }

    public int BaseDefense
    {
        get => baseDefense.Value;
        set => baseDefense.Value = value;
    }

    public int Gold
    {
        get => gold.Value;
        set => gold.Value = value;
    }

    public string? EquippedWeaponId { get; set; }

    public string? EquippedArmorId { get; set; }

    public List<InventoryEntry> Inventory { get; set; } = [];

    public static PlayerProgress CreateDefault(Point startTile, UiLanguage language = UiLanguage.Japanese)
    {
        return new PlayerProgress
        {
            Language = language,
            TilePosition = startTile
        };
    }

    public void AddItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return;
        }

        var existing = Inventory.FirstOrDefault(entry => string.Equals(entry.ItemId, itemId, StringComparison.Ordinal));
        if (existing is null)
        {
            Inventory.Add(new InventoryEntry
            {
                ItemId = itemId,
                Quantity = quantity
            });
            return;
        }

        existing.Quantity += quantity;
    }

    public int GetItemCount(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return Inventory
            .Where(entry => string.Equals(entry.ItemId, itemId, StringComparison.Ordinal))
            .Sum(entry => entry.Quantity);
    }

    public bool RemoveItem(string? itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        var existing = Inventory.FirstOrDefault(entry => string.Equals(entry.ItemId, itemId, StringComparison.Ordinal));
        if (existing is null || existing.Quantity < quantity)
        {
            return false;
        }

        existing.Quantity -= quantity;
        if (existing.Quantity == 0)
        {
            Inventory.Remove(existing);
        }

        if (string.Equals(EquippedWeaponId, itemId, StringComparison.Ordinal) && GetItemCount(itemId) == 0)
        {
            EquippedWeaponId = null;
        }

        if (string.Equals(EquippedArmorId, itemId, StringComparison.Ordinal) && GetItemCount(itemId) == 0)
        {
            EquippedArmorId = null;
        }

        return true;
    }

    public void Normalize()
    {
        Level = Math.Clamp(Level, 1, MaxLevelValue);
        Experience = Math.Max(0, Experience);
        MaxHp = MaxHp <= 0 ? 20 : Math.Min(MaxHp, MaxVitalValue);
        CurrentHp = CurrentHp <= 0 ? MaxHp : Math.Min(CurrentHp, MaxHp);
        MaxMp = MaxMp <= 0 ? 2 : Math.Min(MaxMp, MaxVitalValue);
        CurrentMp = Math.Clamp(CurrentMp, 0, MaxMp);
        BaseAttack = BaseAttack <= 0 ? 5 : BaseAttack;
        BaseDefense = BaseDefense <= 0 ? 3 : BaseDefense;
        Gold = Math.Clamp(Gold, 0, MaxGoldValue);

        if (Level == MaxLevelValue)
        {
            MaxHp = MaxVitalValue;
            MaxMp = MaxVitalValue;
            CurrentHp = Math.Min(CurrentHp, MaxHp);
            CurrentMp = Math.Min(CurrentMp, MaxMp);
        }

        Inventory = Inventory
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ItemId) && entry.Quantity > 0)
            .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
            .Select(group => new InventoryEntry
            {
                ItemId = group.Key,
                Quantity = group.Sum(entry => entry.Quantity)
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(EquippedWeaponId) && GetItemCount(EquippedWeaponId) == 0)
        {
            AddItem(EquippedWeaponId, 1);
        }

        if (!string.IsNullOrWhiteSpace(EquippedArmorId) && GetItemCount(EquippedArmorId) == 0)
        {
            AddItem(EquippedArmorId, 1);
        }
    }

    public void ValidateIntegrity()
    {
        level.Validate();
        experience.Validate();
        maxHp.Validate();
        currentHp.Validate();
        maxMp.Validate();
        currentMp.Validate();
        baseAttack.Validate();
        baseDefense.Validate();
        gold.Validate();

        foreach (var entry in Inventory)
        {
            entry.ValidateIntegrity();
        }
    }

    public void RekeySensitiveValues()
    {
        level.Rekey();
        experience.Rekey();
        maxHp.Rekey();
        currentHp.Rekey();
        maxMp.Rekey();
        currentMp.Rekey();
        baseAttack.Rekey();
        baseDefense.Rekey();
        gold.Rekey();

        foreach (var entry in Inventory)
        {
            entry.RekeySensitiveValues();
        }
    }
}
