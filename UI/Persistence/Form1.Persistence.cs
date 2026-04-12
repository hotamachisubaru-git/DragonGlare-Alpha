using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private string GetText(string key)
    {
        return (selectedLanguage, key) switch
        {
            (UiLanguage.Japanese, "fieldHelpLine1") => "やじるし / WASD: いどう",
            (UiLanguage.English, "fieldHelpLine1") => "ARROWS / WASD: MOVE",
            (UiLanguage.Japanese, "fieldHelpLine2") => "Z: はなす・しらべる   X: ステータス",
            (UiLanguage.English, "fieldHelpLine2") => "Z: TALK / CHECK   X: STATUS",
            (UiLanguage.Japanese, "fieldHelpLine3") => currentFieldMap switch
            {
                FieldMapId.Castle => "B: バトル   V: ショップ   いま: しろ",
                FieldMapId.Field => "B: バトル   V: ショップ   いま: フィールド",
                _ => "B: バトル   V: ショップ   いま: ハブ"
            },
            (UiLanguage.English, "fieldHelpLine3") => currentFieldMap switch
            {
                FieldMapId.Castle => "B: BATTLE   V: SHOP   AREA: CASTLE",
                FieldMapId.Field => "B: BATTLE   V: SHOP   AREA: FIELD",
                _ => "B: BATTLE   V: SHOP   AREA: HUB"
            },
            (UiLanguage.Japanese, "areaField") => "フィールドBGM: SFC_field",
            (UiLanguage.English, "areaField") => "FIELD BGM: SFC_field",
            (UiLanguage.Japanese, "areaCastle") => "おしろBGM: SFC_castle",
            (UiLanguage.English, "areaCastle") => "CASTLE BGM: SFC_castle",
            _ => string.Empty
        };
    }

    private void StartNewGame()
    {
        selectedLanguage = UiLanguage.Japanese;
        languageCursor = 0;
        nameCursorRow = 0;
        nameCursorColumn = 0;
        activeSaveSlot = 0;
        saveSlotCursor = 0;
        playerName.Clear();
        ApplyExplorationSession(progressionService.CreateNewPlayer(UiLanguage.Japanese, PlayerStartTile), FieldMapId.Hub);
        ChangeGameState(GameState.LanguageSelection);
    }

    private bool TryLoadGame(int slotNumber)
    {
        if (!saveService.TryLoadSlot(slotNumber, out var save) || save is null)
        {
            return false;
        }

        activeSaveSlot = slotNumber;
        var restored = SaveDataMapper.Restore(save, PlayerStartTile);
        var loadedPlayer = restored.Player;
        loadedPlayer.Name = TrimPlayerName(loadedPlayer.Name);

        var loadedMapId = restored.MapId;
        if (!IsWalkableTile(MapFactory.CreateMap(loadedMapId), loadedPlayer.TilePosition) ||
            IsBlockedByFieldEvent(loadedMapId, loadedPlayer.TilePosition))
        {
            loadedMapId = FieldMapId.Hub;
            loadedPlayer.TilePosition = PlayerStartTile;
        }

        selectedLanguage = restored.Language;
        ApplyExplorationSession(loadedPlayer, loadedMapId);
        return true;
    }

    private void PersistProgress()
    {
        SaveGame();
    }

    private void SaveGame()
    {
        if (gameState == GameState.ModeSelect || gameState == GameState.LanguageSelection)
        {
            return;
        }

        if (gameState == GameState.NameInput)
        {
            if (playerName.Length == 0)
            {
                return;
            }

            player.Name = TrimPlayerName(playerName.ToString());
        }

        if (string.IsNullOrWhiteSpace(player.Name))
        {
            return;
        }

        if (activeSaveSlot is < 1 or > SaveService.SlotCount)
        {
            return;
        }

        player.Language = selectedLanguage;

        var save = SaveDataMapper.Create(player, selectedLanguage, currentFieldMap, activeSaveSlot);

        try
        {
            saveService.SaveSlot(activeSaveSlot, save);
            RefreshSaveSlotSummaries();
        }
        catch
        {
        }
    }
}
