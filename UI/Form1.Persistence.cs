using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;

namespace DragonGlareAlpha;

public partial class Form1
{
    private string GetText(string key)
    {
        var name = GetDisplayPlayerName();
        return (selectedLanguage, key) switch
        {
            (UiLanguage.Japanese, "fieldHelpLine1") => "やじるし:いどう  ENTER:はなす",
            (UiLanguage.English, "fieldHelpLine1") => "ARROWS: MOVE  ENTER: TALK",
            (UiLanguage.Japanese, "fieldHelpLine2") => "B:バトル  V:ショップ  X:ステータス",
            (UiLanguage.English, "fieldHelpLine2") => "B:BATTLE  V:SHOP  X:STATUS",
            (UiLanguage.Japanese, "areaField") => "フィールドBGM: SFC_field",
            (UiLanguage.English, "areaField") => "FIELD BGM: SFC_field",
            (UiLanguage.Japanese, "areaCastle") => "おしろBGM: SFC_castle",
            (UiLanguage.English, "areaCastle") => "CASTLE BGM: SFC_castle",
            (UiLanguage.Japanese, "npcLine1") => $"{name}、ようこそ。",
            (UiLanguage.Japanese, "npcLine2") => "けんをみがき たびのしたくをしよう。",
            (UiLanguage.English, "npcLine1") => $"Welcome, {name}.",
            (UiLanguage.English, "npcLine2") => "Sharpen your blade and prepare.",
            _ => string.Empty
        };
    }

    private void StartNewGame()
    {
        selectedLanguage = UiLanguage.Japanese;
        languageCursor = 0;
        nameCursorRow = 0;
        nameCursorColumn = 0;
        playerName.Clear();
        player = progressionService.CreateNewPlayer(UiLanguage.Japanese, PlayerStartTile);
        SetFieldMap(FieldMapId.Hub);
        currentEncounter = null;
        battleFlowState = BattleFlowState.CommandSelection;
        isNpcDialogOpen = false;
        isFieldStatusVisible = false;
        movementCooldown = 0;
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        shopItemCursor = 0;
        battleMessage = "まものが あらわれた！";
        shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
        ChangeGameState(GameState.LanguageSelection);
    }

    private bool TryLoadGame()
    {
        if (!saveService.TryLoad(SaveFilePath, out var save) || save is null)
        {
            return false;
        }

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

        if (!IsWalkableTile(loadedPlayer.TilePosition) || IsNpcTile(loadedPlayer.TilePosition))
        {
            SetFieldMap(FieldMapId.Hub);
            loadedPlayer.TilePosition = PlayerStartTile;
        }

        player = loadedPlayer;
        SyncPlayerNameBuffer(player.Name);
        currentEncounter = null;
        battleFlowState = BattleFlowState.CommandSelection;
        isNpcDialogOpen = false;
        isFieldStatusVisible = false;
        movementCooldown = 0;
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

        player.Language = selectedLanguage;

        var save = new SaveData
        {
            Version = 4,
            SavedAtUtc = DateTime.UtcNow,
            Language = selectedLanguage == UiLanguage.English ? "en" : "ja",
            Name = TrimPlayerName(player.Name),
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
            saveService.Save(SaveFilePath, save);
        }
        catch
        {
        }
    }
}
