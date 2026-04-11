using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;

namespace DragonGlareAlpha;

public partial class Form1
{
    private string GetText(string key)
    {
        return (selectedLanguage, key) switch
        {
            (UiLanguage.Japanese, "fieldHelpLine1") => "やじるし / WASD: いどう",
            (UiLanguage.English, "fieldHelpLine1") => "ARROWS / WASD: MOVE",
            (UiLanguage.Japanese, "fieldHelpLine2") => "ENTER: はなす・しらべる   X: ステータス",
            (UiLanguage.English, "fieldHelpLine2") => "ENTER: TALK / CHECK   X: STATUS",
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
        player = progressionService.CreateNewPlayer(UiLanguage.Japanese, PlayerStartTile);
        SetFieldMap(FieldMapId.Hub);
        currentEncounter = null;
        battleFlowState = BattleFlowState.CommandSelection;
        CloseFieldDialog();
        isFieldStatusVisible = false;
        movementCooldown = 0;
        ResetFieldMovementAnimation();
        encounterTransitionFrames = 0;
        pendingEncounter = null;
        ResetBattleVisualEffects();
        ResetEncounterCounter();
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        shopItemCursor = 0;
        battleMessage = "まものが あらわれた！";
        shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
        ChangeGameState(GameState.LanguageSelection);
    }

    private bool TryLoadGame(int slotNumber)
    {
        if (!saveService.TryLoadSlot(slotNumber, out var save) || save is null)
        {
            return false;
        }

        activeSaveSlot = slotNumber;
        selectedLanguage = string.Equals(save.Language, "en", StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.English
            : UiLanguage.Japanese;

        var loadedPlayer = PlayerProgress.CreateDefault(PlayerStartTile, selectedLanguage);
        var loadedMapId = Enum.IsDefined(typeof(FieldMapId), save.CurrentFieldMap) ? save.CurrentFieldMap : FieldMapId.Hub;
        SetFieldMap(loadedMapId);
        loadedPlayer.Name = TrimPlayerName(save.Name);
        loadedPlayer.TilePosition = new Point(save.PlayerX, save.PlayerY);
        loadedPlayer.Level = save.Level;
        loadedPlayer.Experience = save.Experience;
        loadedPlayer.MaxHp = save.MaxHp;
        loadedPlayer.CurrentHp = save.CurrentHp;
        loadedPlayer.MaxMp = save.MaxMp;
        loadedPlayer.CurrentMp = save.CurrentMp;
        loadedPlayer.BaseAttack = save.BaseAttack;
        loadedPlayer.BaseDefense = save.BaseDefense;
        loadedPlayer.Gold = save.Gold;
        loadedPlayer.EquippedWeaponId = save.EquippedWeaponId;
        loadedPlayer.Inventory = save.Inventory?.Select(entry => entry.Clone()).ToList() ?? [];
        loadedPlayer.Normalize();

        if (!IsWalkableTile(loadedPlayer.TilePosition) || IsBlockedByFieldEvent(loadedPlayer.TilePosition))
        {
            SetFieldMap(FieldMapId.Hub);
            loadedPlayer.TilePosition = PlayerStartTile;
        }

        player = loadedPlayer;
        SyncPlayerNameBuffer(player.Name);
        currentEncounter = null;
        battleFlowState = BattleFlowState.CommandSelection;
        CloseFieldDialog();
        isFieldStatusVisible = false;
        movementCooldown = 0;
        ResetFieldMovementAnimation();
        encounterTransitionFrames = 0;
        pendingEncounter = null;
        ResetBattleVisualEffects();
        ResetEncounterCounter();
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        shopItemCursor = 0;
        battleMessage = "まものが あらわれた！";
        shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
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

        var save = new SaveData
        {
            Version = 6,
            SavedAtUtc = DateTime.UtcNow,
            Language = selectedLanguage == UiLanguage.English ? "en" : "ja",
            Name = TrimPlayerName(player.Name),
            SlotNumber = activeSaveSlot,
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
            Inventory = player.Inventory.Select(entry => entry.Clone()).ToList()
        };

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
