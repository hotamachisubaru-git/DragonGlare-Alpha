using System.Text;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;

namespace DragonGlareAlpha.Tests;

public sealed class SaveServiceTests
{
    [Fact]
    public void SaveAndLoadSlot_RoundTripsExpandedProgressAndEncryptsPayload()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SaveService(tempDirectory);

        try
        {
            var save = new SaveData
            {
                Language = "ja",
                Name = "テスター",
                SlotNumber = 2,
                CurrentFieldMap = FieldMapId.Castle,
                PlayerX = 7,
                PlayerY = 8,
                Level = 3,
                Experience = 29,
                MaxHp = 30,
                CurrentHp = 24,
                MaxMp = 6,
                CurrentMp = 5,
                BaseAttack = 10,
                BaseDefense = 7,
                Gold = 123,
                EquippedWeaponId = "bronze_sword",
                Inventory =
                [
                    new InventoryEntry
                    {
                        ItemId = "bronze_sword",
                        Quantity = 1
                    }
                ]
            };

            service.SaveSlot(2, save);
            var loaded = service.TryLoadSlot(2, out var roundTripped);
            var rawText = Encoding.UTF8.GetString(File.ReadAllBytes(service.GetSlotPath(2)));

            Assert.True(loaded);
            Assert.NotNull(roundTripped);
            Assert.Equal(2, roundTripped!.SlotNumber);
            Assert.Equal(FieldMapId.Castle, roundTripped.CurrentFieldMap);
            Assert.Equal(3, roundTripped.Level);
            Assert.Equal("bronze_sword", roundTripped.EquippedWeaponId);
            Assert.Single(roundTripped.Inventory);
            Assert.Equal(123, roundTripped.Gold);
            Assert.False(string.IsNullOrWhiteSpace(roundTripped.Signature));
            Assert.DoesNotContain("テスター", rawText, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Gold\"", rawText, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void TryLoadSlot_WhenEncryptedFileWasTampered_ReturnsInvalidSignature()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SaveService(tempDirectory);

        try
        {
            service.SaveSlot(1, new SaveData
            {
                Language = "ja",
                Name = "テスター",
                Gold = 123
            });

            var path = service.GetSlotPath(1);
            var payload = File.ReadAllBytes(path);
            payload[0] ^= 0x7F;
            File.WriteAllBytes(path, payload);

            var loaded = service.TryLoadSlot(1, out var roundTripped);

            Assert.False(loaded);
            Assert.Null(roundTripped);
            Assert.Equal(SaveLoadFailureReason.InvalidSignature, service.LastFailureReason);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void GetSlotSummaries_ReturnsEmptyAndOccupiedStates()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SaveService(tempDirectory);

        try
        {
            service.SaveSlot(3, new SaveData
            {
                Language = "ja",
                Name = "スロット3",
                Level = 8,
                Gold = 456
            });

            var summaries = service.GetSlotSummaries().ToArray();

            Assert.Equal(SaveSlotState.Empty, summaries[0].State);
            Assert.Equal(SaveSlotState.Empty, summaries[1].State);
            Assert.Equal(SaveSlotState.Occupied, summaries[2].State);
            Assert.Equal("スロット3", summaries[2].Name);
            Assert.Equal(8, summaries[2].Level);
            Assert.Equal(456, summaries[2].Gold);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
