extern alias monogame;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Domain.Startup;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;
using LegacyPersistence = monogame::DragonGlareAlpha.Persistence;

namespace DragonGlare.UnityPort.Tests;

public sealed class SaveCompatibilityTests
{
    [Fact]
    public void PortSave_IsReadByOriginal_AndOriginalSaveIsReadByPort()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DragonGlare-Core-" + Guid.NewGuid().ToString("N"));
        try
        {
            var port = new SaveService(directory);
            var original = new LegacyPersistence.SaveService(directory);
            var saved = CreateExpandedSave();
            port.SaveSlot(2, saved);

            Assert.True(original.TryLoadSlot(2, out var originalData));
            Assert.NotNull(originalData);
            Assert.Equal(saved.Signature, originalData.Signature);
            Assert.Equal(saved.Name, originalData.Name);
            Assert.Equal(saved.BankGold, originalData.BankGold);
            Assert.Equal(saved.Inventory[0].Quantity, originalData.Inventory[0].Quantity);
            Assert.Equal(saved.CompletedFieldEventIds, originalData.CompletedFieldEventIds);

            original.SaveSlot(2, originalData);
            Assert.True(port.TryLoadSlot(2, out var portData));
            Assert.NotNull(portData);
            Assert.Equal(saved.Signature, portData.Signature);
            Assert.Equal(saved.SavedAtUtc, portData.SavedAtUtc);
            Assert.Equal(saved.EquippedHeadId, portData.EquippedHeadId);
            Assert.Equal(saved.CompletedFieldEventIds, portData.CompletedFieldEventIds);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PortRejectsUnsignedModificationAfterSuccessfulDpapiDecryption()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DragonGlare-Core-" + Guid.NewGuid().ToString("N"));
        try
        {
            var port = new SaveService(directory);
            port.SaveSlot(1, CreateExpandedSave());
            var entropy = SHA256.HashData(Encoding.UTF8.GetBytes("DragonGlareAlpha::Dpapi::2026-04-09::1"));
            var json = ProtectedData.Unprotect(File.ReadAllBytes(port.GetSlotPath(1)), entropy, DataProtectionScope.CurrentUser);
            var changed = JsonSerializer.Deserialize<SaveData>(json)!;
            changed.Gold++;
            var encrypted = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(changed), entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(port.GetSlotPath(1), encrypted);

            Assert.False(port.TryLoadSlot(1, out _));
            Assert.Equal(SaveLoadFailureReason.InvalidSignature, port.LastFailureReason);
            var original = new LegacyPersistence.SaveService(directory);
            Assert.False(original.TryLoadSlot(1, out _));
            Assert.Equal(LegacyPersistence.SaveLoadFailureReason.InvalidSignature, original.LastFailureReason);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PortSaveReplacesExistingSlotAndCleansTemporaryPayload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DragonGlare-Core-" + Guid.NewGuid().ToString("N"));
        try
        {
            var port = new SaveService(directory);
            var save = CreateExpandedSave();
            port.SaveSlot(3, save);
            save.Gold = 909;
            port.SaveSlot(3, save);

            Assert.True(port.TryLoadSlot(3, out var restored));
            Assert.Equal(909, restored!.Gold);
            Assert.False(File.Exists(port.GetSlotPath(3) + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void LegacyImportMigratesStagedCopyAndPreservesOriginalUserFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DragonGlare-Core-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sourceRoot = Path.Combine(directory, "monogame-user");
            var destinationRoot = Path.Combine(directory, "unity-user");
            var stagingRoot = Path.Combine(destinationRoot, ".import-test");
            Directory.CreateDirectory(sourceRoot);
            var legacyPath = Path.Combine(sourceRoot, "save1.sav");
            var legacyJson = """
                {
                  "Version": 4,
                  "SavedAtUtc": "2026-04-08T01:02:03.1234567Z",
                  "Language": "ja",
                  "Name": "旧セーブの勇者",
                  "CurrentFieldMap": 1,
                  "PlayerX": 14,
                  "PlayerY": 20,
                  "Level": 8,
                  "Experience": 511,
                  "MaxHp": 70,
                  "CurrentHp": 61,
                  "MaxMp": 18,
                  "CurrentMp": 15,
                  "BaseAttack": 21,
                  "BaseDefense": 15,
                  "Gold": 1103,
                  "EquippedWeaponId": "bronze_sword",
                  "Inventory": [ { "ItemId": "bronze_sword", "Quantity": 2 } ]
                }
                """;
            File.WriteAllText(legacyPath, legacyJson, new UTF8Encoding(false));
            var originalBytes = File.ReadAllBytes(legacyPath);
            var source = new SaveService(sourceRoot);
            var destination = new SaveService(destinationRoot);
            var staged = new SaveService(stagingRoot);
            Assert.False(source.TryLoadSlot(1, out _));
            Assert.False(File.Exists(source.GetSlotPath(1)));
            Assert.False(File.Exists(destination.GetSlotPath(1)));

            Directory.CreateDirectory(stagingRoot);
            var legacyCopy = Path.Combine(stagingRoot, "legacy-source.sav");
            File.Copy(legacyPath, legacyCopy);
            Assert.True(staged.TryMigrateLegacySave(legacyCopy, 1));
            Assert.False(File.Exists(legacyCopy));
            Assert.True(staged.TryLoadSlot(1, out var imported));
            staged.SaveSlot(1, imported!);
            Assert.True(staged.TryLoadSlot(1, out _));
            File.Move(staged.GetSlotPath(1), destination.GetSlotPath(1));

            Assert.True(destination.TryLoadSlot(1, out var restored));
            Assert.Equal(SaveData.CurrentVersion, restored!.Version);
            Assert.Equal(1, restored.SlotNumber);
            Assert.Equal("旧セーブの勇者", restored.Name);
            Assert.Equal(FieldMapId.Castle, restored.CurrentFieldMap);
            Assert.Equal(14, restored.PlayerX);
            Assert.Equal(20, restored.PlayerY);
            Assert.Equal(8, restored.Level);
            Assert.Equal(511, restored.Experience);
            Assert.Equal(1103, restored.Gold);
            Assert.Equal("bronze_sword", restored.EquippedWeaponId);
            Assert.Equal("bronze_sword", Assert.Single(restored.Inventory).ItemId);
            Assert.Equal(2, restored.Inventory[0].Quantity);
            Assert.Equal(new DateTime(2026, 4, 8, 1, 2, 3, DateTimeKind.Utc).AddTicks(1234567), restored.SavedAtUtc);
            Assert.False(string.IsNullOrWhiteSpace(restored.Signature));
            Assert.Equal(originalBytes, File.ReadAllBytes(legacyPath));
            Assert.False(File.Exists(source.GetSlotPath(1)));
            Assert.False(File.Exists(staged.GetSlotPath(1)));
            Assert.False(File.Exists(legacyCopy));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void PortLaunchSettingsUseTheExplicitPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DragonGlare-Core-" + Guid.NewGuid().ToString("N"));
        try
        {
            var settingsPath = Path.Combine(directory, "launch.json");
            var settings = new LaunchSettingsService(settingsPath);
            settings.Save(new LaunchSettings { Volume = 0.25f, PromptOnStartup = false });
            Assert.True(File.Exists(settingsPath));
            Assert.Equal(0.25f, settings.Load().Volume);
            Assert.False(settings.Load().PromptOnStartup);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static SaveData CreateExpandedSave() => new()
    {
        SavedAtUtc = new DateTime(2026, 10, 14, 1, 2, 3, DateTimeKind.Utc).AddTicks(1234567),
        Name = "竜<&>\"\\😀",
        Language = "ja",
        CurrentFieldMap = FieldMapId.Dungeon,
        PlayerX = 14,
        PlayerY = 20,
        Level = 8,
        Experience = 511,
        MaxHp = 70,
        CurrentHp = 61,
        MaxMp = 18,
        CurrentMp = 15,
        BaseAttack = 21,
        BaseDefense = 15,
        Gold = 1103,
        BankGold = 801,
        LoanBalance = 209,
        LoanStepCounter = 5,
        EquippedWeaponId = "bronze_sword",
        EquippedArmorId = "leather_armor",
        EquippedHeadId = "leather_cap",
        EquippedArmsId = "leather_bracers",
        EquippedLegsId = "leather_leggings",
        EquippedFeetId = "travel_boots",
        Inventory = [new InventoryEntry { ItemId = "bronze_sword", Quantity = 2 }],
        CompletedFieldEventIds = ["field_treasure_chest"]
    };
}
