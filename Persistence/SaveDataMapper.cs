using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;

namespace DragonGlareAlpha.Persistence;

public static class SaveDataMapper
{
    public static RestoredSaveState Restore(SaveData saveData, Point defaultStartTile)
    {
        var language = string.Equals(saveData.Language, "en", StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.English
            : UiLanguage.Japanese;
        var mapId = Enum.IsDefined(typeof(FieldMapId), saveData.CurrentFieldMap)
            ? saveData.CurrentFieldMap
            : FieldMapId.Hub;

        var player = PlayerProgress.CreateDefault(defaultStartTile, language);
        player.Name = saveData.Name;
        player.TilePosition = new Point(saveData.PlayerX, saveData.PlayerY);
        player.Level = saveData.Level;
        player.Experience = saveData.Experience;
        player.MaxHp = saveData.MaxHp;
        player.CurrentHp = saveData.CurrentHp;
        player.MaxMp = saveData.MaxMp;
        player.CurrentMp = saveData.CurrentMp;
        player.BaseAttack = saveData.BaseAttack;
        player.BaseDefense = saveData.BaseDefense;
        player.Gold = saveData.Gold;
        player.EquippedWeaponId = saveData.EquippedWeaponId;
        player.EquippedArmorId = saveData.EquippedArmorId;
        player.Inventory = saveData.Inventory?.Select(entry => entry.Clone()).ToList() ?? [];
        player.Normalize();

        return new RestoredSaveState(language, mapId, player);
    }

    public static SaveData Create(PlayerProgress player, UiLanguage language, FieldMapId currentFieldMap, int slotNumber)
    {
        return new SaveData
        {
            SavedAtUtc = DateTime.UtcNow,
            Language = language == UiLanguage.English ? "en" : "ja",
            Name = player.Name,
            SlotNumber = slotNumber,
            CurrentFieldMap = currentFieldMap,
            PlayerX = player.TilePosition.X,
            PlayerY = player.TilePosition.Y,
            Level = player.Level,
            Experience = player.Experience,
            MaxHp = player.MaxHp,
            CurrentHp = player.CurrentHp,
            MaxMp = player.MaxMp,
            CurrentMp = player.CurrentMp,
            BaseAttack = player.BaseAttack,
            BaseDefense = player.BaseDefense,
            Gold = player.Gold,
            EquippedWeaponId = player.EquippedWeaponId,
            EquippedArmorId = player.EquippedArmorId,
            Inventory = player.Inventory.Select(entry => entry.Clone()).ToList()
        };
    }
}

public readonly record struct RestoredSaveState(UiLanguage Language, FieldMapId MapId, PlayerProgress Player);
