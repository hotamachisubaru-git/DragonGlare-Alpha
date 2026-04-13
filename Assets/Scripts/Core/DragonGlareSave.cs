using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DragonGlareAlpha.Unity
{
    [Serializable]
    public sealed class SaveInventoryEntry
    {
        public string ItemId = string.Empty;
        public int Quantity;
    }

    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SavedAtUtc = string.Empty;
        public string Language = "ja";
        public string Name = string.Empty;
        public int SlotNumber;
        public FieldMapId CurrentFieldMap = FieldMapId.Hub;
        public int PlayerX;
        public int PlayerY;
        public int Level;
        public int Experience;
        public int MaxHp;
        public int CurrentHp;
        public int MaxMp;
        public int CurrentMp;
        public int BaseAttack;
        public int BaseDefense;
        public int Gold;
        public string EquippedWeaponId = string.Empty;
        public string EquippedArmorId = string.Empty;
        public List<SaveInventoryEntry> Inventory = new List<SaveInventoryEntry>();
        public string Signature = string.Empty;
    }

    public sealed class RestoredSaveState
    {
        public RestoredSaveState(UiLanguage language, FieldMapId mapId, PlayerProgress player)
        {
            Language = language;
            MapId = mapId;
            Player = player;
        }

        public UiLanguage Language { get; private set; }
        public FieldMapId MapId { get; private set; }
        public PlayerProgress Player { get; private set; }
    }

    public sealed class SaveSlotSummary
    {
        public int SlotNumber { get; set; }
        public SaveSlotState State { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Gold { get; set; }
        public FieldMapId CurrentFieldMap { get; set; }
        public DateTime SavedAtLocal { get; set; }

        public SaveSlotSummary()
        {
            Name = string.Empty;
        }
    }

    public static class SaveDataMapper
    {
        public static RestoredSaveState Restore(SaveData saveData, Vector2Int defaultStartTile)
        {
            UiLanguage language = string.Equals(saveData.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? UiLanguage.English
                : UiLanguage.Japanese;

            FieldMapId mapId = Enum.IsDefined(typeof(FieldMapId), saveData.CurrentFieldMap)
                ? saveData.CurrentFieldMap
                : FieldMapId.Hub;

            PlayerProgress player = PlayerProgress.CreateDefault(defaultStartTile, language);
            player.Name = saveData.Name ?? string.Empty;
            player.TilePosition = new Vector2Int(saveData.PlayerX, saveData.PlayerY);
            player.Level = saveData.Level;
            player.Experience = saveData.Experience;
            player.MaxHp = saveData.MaxHp;
            player.CurrentHp = saveData.CurrentHp;
            player.MaxMp = saveData.MaxMp;
            player.CurrentMp = saveData.CurrentMp;
            player.BaseAttack = saveData.BaseAttack;
            player.BaseDefense = saveData.BaseDefense;
            player.Gold = saveData.Gold;
            player.EquippedWeaponId = saveData.EquippedWeaponId ?? string.Empty;
            player.EquippedArmorId = saveData.EquippedArmorId ?? string.Empty;
            player.Inventory = new List<InventoryEntry>();

            if (saveData.Inventory != null)
            {
                for (int index = 0; index < saveData.Inventory.Count; index++)
                {
                    SaveInventoryEntry saveEntry = saveData.Inventory[index];
                    if (saveEntry == null)
                    {
                        continue;
                    }

                    player.Inventory.Add(new InventoryEntry
                    {
                        ItemId = saveEntry.ItemId ?? string.Empty,
                        Quantity = saveEntry.Quantity
                    });
                }
            }

            player.Normalize();
            return new RestoredSaveState(language, mapId, player);
        }

        public static SaveData Create(PlayerProgress player, UiLanguage language, FieldMapId currentFieldMap, int slotNumber)
        {
            SaveData data = new SaveData();
            data.SavedAtUtc = DateTime.UtcNow.ToString("O");
            data.Language = language == UiLanguage.English ? "en" : "ja";
            data.Name = player.Name ?? string.Empty;
            data.SlotNumber = slotNumber;
            data.CurrentFieldMap = currentFieldMap;
            data.PlayerX = player.TilePosition.x;
            data.PlayerY = player.TilePosition.y;
            data.Level = player.Level;
            data.Experience = player.Experience;
            data.MaxHp = player.MaxHp;
            data.CurrentHp = player.CurrentHp;
            data.MaxMp = player.MaxMp;
            data.CurrentMp = player.CurrentMp;
            data.BaseAttack = player.BaseAttack;
            data.BaseDefense = player.BaseDefense;
            data.Gold = player.Gold;
            data.EquippedWeaponId = player.EquippedWeaponId ?? string.Empty;
            data.EquippedArmorId = player.EquippedArmorId ?? string.Empty;
            data.Inventory = new List<SaveInventoryEntry>();

            for (int index = 0; index < player.Inventory.Count; index++)
            {
                InventoryEntry entry = player.Inventory[index];
                data.Inventory.Add(new SaveInventoryEntry
                {
                    ItemId = entry.ItemId,
                    Quantity = entry.Quantity
                });
            }

            return data;
        }
    }

    public sealed class SaveService
    {
        public const int SlotCount = 3;
        private const string SignatureSecret = "DragonGlareAlpha::UnitySaveSeal::2026-04-13";

        private readonly string saveRootDirectory;

        public SaveService(string saveRoot = null)
        {
            saveRootDirectory = string.IsNullOrEmpty(saveRoot)
                ? Path.Combine(Application.persistentDataPath, "DragonGlareAlpha")
                : Path.GetFullPath(saveRoot);
        }

        public SaveLoadFailureReason LastFailureReason { get; private set; }

        public string GetSlotPath(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);
            return Path.Combine(saveRootDirectory, "slot" + slotNumber + ".json");
        }

        public void SaveSlot(int slotNumber, SaveData saveData)
        {
            ValidateSlotNumber(slotNumber);
            Directory.CreateDirectory(saveRootDirectory);

            saveData.Version = SaveData.CurrentVersion;
            saveData.SlotNumber = slotNumber;
            saveData.Signature = ComputeSignature(saveData);

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSlotPath(slotNumber), json, Encoding.UTF8);
        }

        public bool TryLoadSlot(int slotNumber, out SaveData saveData)
        {
            return TryLoadSlotInternal(slotNumber, true, out saveData);
        }

        public List<SaveSlotSummary> GetSlotSummaries()
        {
            List<SaveSlotSummary> summaries = new List<SaveSlotSummary>();

            for (int slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
            {
                SaveData saveData;
                if (TryLoadSlotInternal(slotNumber, false, out saveData) && saveData != null)
                {
                    DateTime savedAt;
                    DateTime.TryParse(saveData.SavedAtUtc, out savedAt);
                    summaries.Add(new SaveSlotSummary
                    {
                        SlotNumber = slotNumber,
                        State = SaveSlotState.Occupied,
                        Name = saveData.Name,
                        Level = saveData.Level,
                        Gold = saveData.Gold,
                        CurrentFieldMap = saveData.CurrentFieldMap,
                        SavedAtLocal = savedAt == default(DateTime) ? DateTime.MinValue : savedAt.ToLocalTime()
                    });
                }
                else
                {
                    summaries.Add(new SaveSlotSummary
                    {
                        SlotNumber = slotNumber,
                        State = File.Exists(GetSlotPath(slotNumber)) ? SaveSlotState.Corrupted : SaveSlotState.Empty
                    });
                }
            }

            return summaries;
        }

        private bool TryLoadSlotInternal(int slotNumber, bool updateFailureReason, out SaveData saveData)
        {
            ValidateSlotNumber(slotNumber);

            saveData = null;
            if (updateFailureReason)
            {
                LastFailureReason = SaveLoadFailureReason.None;
            }

            try
            {
                string path = GetSlotPath(slotNumber);
                if (!File.Exists(path))
                {
                    if (updateFailureReason)
                    {
                        LastFailureReason = SaveLoadFailureReason.NotFound;
                    }

                    return false;
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null)
                {
                    if (updateFailureReason)
                    {
                        LastFailureReason = SaveLoadFailureReason.InvalidFormat;
                    }

                    return false;
                }

                if (saveData.SlotNumber != 0 && saveData.SlotNumber != slotNumber)
                {
                    if (updateFailureReason)
                    {
                        LastFailureReason = SaveLoadFailureReason.InvalidSignature;
                    }

                    saveData = null;
                    return false;
                }

                saveData.SlotNumber = slotNumber;
                if (!HasValidSignature(saveData))
                {
                    if (updateFailureReason)
                    {
                        LastFailureReason = SaveLoadFailureReason.InvalidSignature;
                    }

                    saveData = null;
                    return false;
                }

                return true;
            }
            catch
            {
                if (updateFailureReason)
                {
                    LastFailureReason = SaveLoadFailureReason.InvalidFormat;
                }

                saveData = null;
                return false;
            }
        }

        private static bool HasValidSignature(SaveData saveData)
        {
            if (string.IsNullOrEmpty(saveData.Signature))
            {
                return false;
            }

            return string.Equals(saveData.Signature, ComputeSignature(saveData), StringComparison.Ordinal);
        }

        private static string ComputeSignature(SaveData saveData)
        {
            byte[] key;
            using (SHA256 sha = SHA256.Create())
            {
                key = sha.ComputeHash(Encoding.UTF8.GetBytes(SignatureSecret));
            }

            byte[] hash;
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(BuildSignaturePayload(saveData)));
            }

            return Convert.ToBase64String(hash);
        }

        private static string BuildSignaturePayload(SaveData saveData)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(saveData.Version).Append('|');
            builder.Append(saveData.SavedAtUtc ?? string.Empty).Append('|');
            builder.Append(saveData.Language ?? string.Empty).Append('|');
            builder.Append(saveData.Name ?? string.Empty).Append('|');
            builder.Append(saveData.SlotNumber).Append('|');
            builder.Append((int)saveData.CurrentFieldMap).Append('|');
            builder.Append(saveData.PlayerX).Append('|');
            builder.Append(saveData.PlayerY).Append('|');
            builder.Append(saveData.Level).Append('|');
            builder.Append(saveData.Experience).Append('|');
            builder.Append(saveData.MaxHp).Append('|');
            builder.Append(saveData.CurrentHp).Append('|');
            builder.Append(saveData.MaxMp).Append('|');
            builder.Append(saveData.CurrentMp).Append('|');
            builder.Append(saveData.BaseAttack).Append('|');
            builder.Append(saveData.BaseDefense).Append('|');
            builder.Append(saveData.Gold).Append('|');
            builder.Append(saveData.EquippedWeaponId ?? string.Empty).Append('|');
            builder.Append(saveData.EquippedArmorId ?? string.Empty).Append('|');

            if (saveData.Inventory != null)
            {
                for (int index = 0; index < saveData.Inventory.Count; index++)
                {
                    SaveInventoryEntry entry = saveData.Inventory[index];
                    builder.Append(entry.ItemId ?? string.Empty).Append(':').Append(entry.Quantity).Append('|');
                }
            }

            return builder.ToString();
        }

        private static void ValidateSlotNumber(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > SlotCount)
            {
                throw new ArgumentOutOfRangeException("slotNumber");
            }
        }
    }
}
