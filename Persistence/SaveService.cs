using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Persistence;

public sealed class SaveService
{
    public const int SlotCount = 3;

    private const int CurrentSaveVersion = 6;
    private const int SignedSaveVersion = 5;
    private const string SignatureSecret = "DragonGlareAlpha::SaveSeal::2026-04-09";
    private const string DpapiEntropySecret = "DragonGlareAlpha::Dpapi::2026-04-09";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string saveRootDirectory;

    public SaveService(string? saveRootDirectory = null)
    {
        this.saveRootDirectory = string.IsNullOrWhiteSpace(saveRootDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DragonGlareAlpha")
            : Path.GetFullPath(saveRootDirectory);
    }

    public SaveLoadFailureReason LastFailureReason { get; private set; }

    public string SaveRootDirectory => saveRootDirectory;

    public string GetSlotPath(int slotNumber)
    {
        ValidateSlotNumber(slotNumber);
        return Path.Combine(saveRootDirectory, $"slot{slotNumber}.sav");
    }

    public bool TryLoadSlot(int slotNumber, [NotNullWhen(true)] out SaveData? saveData)
    {
        return TryLoadSlotInternal(slotNumber, updateFailureReason: true, out saveData);
    }

    public IReadOnlyList<SaveSlotSummary> GetSlotSummaries()
    {
        var summaries = new List<SaveSlotSummary>(SlotCount);

        for (var slotNumber = 1; slotNumber <= SlotCount; slotNumber++)
        {
            if (TryLoadSlotInternal(slotNumber, updateFailureReason: false, out var saveData) && saveData is not null)
            {
                summaries.Add(new SaveSlotSummary
                {
                    SlotNumber = slotNumber,
                    State = SaveSlotState.Occupied,
                    Name = saveData.Name,
                    Level = saveData.Level,
                    Gold = saveData.Gold,
                    CurrentFieldMap = saveData.CurrentFieldMap,
                    SavedAtLocal = NormalizeUtc(saveData.SavedAtUtc).ToLocalTime()
                });
                continue;
            }

            summaries.Add(new SaveSlotSummary
            {
                SlotNumber = slotNumber,
                State = File.Exists(GetSlotPath(slotNumber)) ? SaveSlotState.Corrupted : SaveSlotState.Empty
            });
        }

        return summaries;
    }

    public void SaveSlot(int slotNumber, SaveData saveData)
    {
        ValidateSlotNumber(slotNumber);

        Directory.CreateDirectory(saveRootDirectory);

        saveData.Version = CurrentSaveVersion;
        saveData.SlotNumber = slotNumber;
        saveData.Signature = ComputeSignature(saveData);

        var json = JsonSerializer.Serialize(saveData, SerializerOptions);
        var protectedPayload = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(json),
            BuildDpapiEntropy(slotNumber),
            DataProtectionScope.CurrentUser);

        var path = GetSlotPath(slotNumber);
        var tempPath = $"{path}.tmp";
        File.WriteAllBytes(tempPath, protectedPayload);
        File.Move(tempPath, path, overwrite: true);
    }

    public bool TryMigrateLegacySave(string legacyPath, int slotNumber = 1)
    {
        ValidateSlotNumber(slotNumber);

        if (!File.Exists(legacyPath) || File.Exists(GetSlotPath(slotNumber)))
        {
            return false;
        }

        if (!TryLoadLegacyJson(legacyPath, out var saveData) || saveData is null)
        {
            return false;
        }

        try
        {
            SaveSlot(slotNumber, saveData);
            try
            {
                File.Delete(legacyPath);
            }
            catch
            {
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryLoadSlotInternal(int slotNumber, bool updateFailureReason, [NotNullWhen(true)] out SaveData? saveData)
    {
        ValidateSlotNumber(slotNumber);

        saveData = null;
        if (updateFailureReason)
        {
            LastFailureReason = SaveLoadFailureReason.None;
        }

        try
        {
            var path = GetSlotPath(slotNumber);
            if (!File.Exists(path))
            {
                if (updateFailureReason)
                {
                    LastFailureReason = SaveLoadFailureReason.NotFound;
                }

                return false;
            }

            var protectedPayload = File.ReadAllBytes(path);
            var plainPayload = ProtectedData.Unprotect(
                protectedPayload,
                BuildDpapiEntropy(slotNumber),
                DataProtectionScope.CurrentUser);

            var json = Encoding.UTF8.GetString(plainPayload);
            saveData = JsonSerializer.Deserialize<SaveData>(json, SerializerOptions);
            if (saveData is null)
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
        catch (JsonException)
        {
            if (updateFailureReason)
            {
                LastFailureReason = SaveLoadFailureReason.InvalidFormat;
            }

            return false;
        }
        catch (CryptographicException)
        {
            if (updateFailureReason)
            {
                LastFailureReason = SaveLoadFailureReason.InvalidSignature;
            }

            return false;
        }
        catch
        {
            if (updateFailureReason)
            {
                LastFailureReason = SaveLoadFailureReason.InvalidFormat;
            }

            return false;
        }
    }

    private static bool TryLoadLegacyJson(string path, [NotNullWhen(true)] out SaveData? saveData)
    {
        saveData = null;

        try
        {
            var json = File.ReadAllText(path);
            saveData = JsonSerializer.Deserialize<SaveData>(json, SerializerOptions);
            return saveData is not null && HasValidSignature(saveData);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasValidSignature(SaveData saveData)
    {
        if (saveData.Version < SignedSaveVersion && string.IsNullOrWhiteSpace(saveData.Signature))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(saveData.Signature))
        {
            return false;
        }

        var expected = Convert.FromBase64String(ComputeSignature(saveData));
        var actual = Convert.FromBase64String(saveData.Signature);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string ComputeSignature(SaveData saveData)
    {
        var effectiveVersion = Math.Max(saveData.Version, SignedSaveVersion);
        var payload = BuildSignaturePayload(saveData, effectiveVersion);
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(SignatureSecret));
        var hash = HMACSHA256.HashData(key, payload);
        return Convert.ToBase64String(hash);
    }

    private static byte[] BuildSignaturePayload(SaveData saveData, int effectiveVersion)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteNumber("Version", effectiveVersion);
        writer.WriteString("SavedAtUtc", NormalizeUtc(saveData.SavedAtUtc));
        writer.WriteString("Language", saveData.Language ?? string.Empty);
        writer.WriteString("Name", saveData.Name ?? string.Empty);
        if (effectiveVersion >= CurrentSaveVersion)
        {
            writer.WriteNumber("SlotNumber", saveData.SlotNumber);
        }

        writer.WriteNumber("CurrentFieldMap", (int)saveData.CurrentFieldMap);
        writer.WriteNumber("PlayerX", saveData.PlayerX);
        writer.WriteNumber("PlayerY", saveData.PlayerY);
        writer.WriteNumber("Level", saveData.Level);
        writer.WriteNumber("Experience", saveData.Experience);
        writer.WriteNumber("MaxHp", saveData.MaxHp);
        writer.WriteNumber("CurrentHp", saveData.CurrentHp);
        writer.WriteNumber("MaxMp", saveData.MaxMp);
        writer.WriteNumber("CurrentMp", saveData.CurrentMp);
        writer.WriteNumber("BaseAttack", saveData.BaseAttack);
        writer.WriteNumber("BaseDefense", saveData.BaseDefense);
        writer.WriteNumber("Gold", saveData.Gold);
        writer.WriteString("EquippedWeaponId", saveData.EquippedWeaponId ?? string.Empty);
        writer.WritePropertyName("Inventory");
        writer.WriteStartArray();
        foreach (var entry in saveData.Inventory ?? [])
        {
            writer.WriteStartObject();
            writer.WriteString("ItemId", entry.ItemId ?? string.Empty);
            writer.WriteNumber("Quantity", entry.Quantity);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return stream.ToArray();
    }

    private static byte[] BuildDpapiEntropy(int slotNumber)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes($"{DpapiEntropySecret}::{slotNumber}"));
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static void ValidateSlotNumber(int slotNumber)
    {
        if (slotNumber is < 1 or > SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slotNumber), $"slotNumber must be between 1 and {SlotCount}.");
        }
    }
}
