using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DragonGlareAlpha.Unity
{
    public enum FacingDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public struct InputSnapshot
    {
        public bool UpPressed;
        public bool DownPressed;
        public bool LeftPressed;
        public bool RightPressed;
        public bool UpHeld;
        public bool DownHeld;
        public bool LeftHeld;
        public bool RightHeld;
        public bool ConfirmPressed;
        public bool CancelPressed;
        public bool ToggleStatusPressed;
        public bool OpenBattlePressed;
        public bool OpenShopPressed;
        public bool BackspacePressed;
    }

    public sealed class BattleSelectionEntry
    {
        public string Label { get; set; }
        public string Detail { get; set; }
        public string Badge { get; set; }
        public ConsumableDefinition Consumable { get; set; }
        public IEquipmentDefinition Equipment { get; set; }

        public BattleSelectionEntry()
        {
            Label = string.Empty;
            Detail = string.Empty;
            Badge = string.Empty;
        }
    }

    public sealed class ShopMenuEntry
    {
        public enum MenuEntryType
        {
            Item,
            PreviousPage,
            NextPage,
            Quit
        }

        public MenuEntryType Type { get; set; }
        public string Label { get; set; }
        public IEquipmentDefinition Item { get; set; }

        public ShopMenuEntry()
        {
            Label = string.Empty;
        }
    }

    public sealed partial class DragonGlareGameSession
    {
        private const int ShopItemsPerPage = 6;
        private const int FieldMovementAnimationDuration = 6;
        private const int EncounterTransitionDuration = 24;
        private const int BattleSelectionVisibleRows = 4;
        private static readonly Vector2Int PlayerStartTile = new Vector2Int(3, 12);

        private const string DefaultBattleMessage = "まものが あらわれた！";
        private const string BattleEscapeMessage = "うまく にげきった！";
        private const string ShopWelcomeMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
        private const string ShopBrowseMessage = "＊「なにを かっていくかい？」";
        private const string ShopReturnMessage = "＊「ほかに ようじは あるかい？」";
        private const string ShopFarewellMessage = "＊「また きてくれよな！」";

        private readonly System.Random random = new System.Random();
        private readonly SaveService saveService = new SaveService();
        private readonly BattleService battleService = new BattleService();
        private readonly ProgressionService progressionService = new ProgressionService();
        private readonly ShopService shopService = new ShopService();
        private readonly FieldEventService fieldEventService = new FieldEventService();
        private readonly FieldTransitionService fieldTransitionService = new FieldTransitionService();
        private readonly Queue<SoundEffect> pendingSoundEffects = new Queue<SoundEffect>();

        private PlayerProgress player;
        private int[,] map;
        private BattleEncounter currentEncounter;
        private BattleEncounter pendingEncounter;
        private GameState gameState;
        private FieldMapId currentFieldMap;
        private UiLanguage selectedLanguage;
        private FacingDirection playerFacingDirection;
        private SaveSlotSelectionMode saveSlotSelectionMode;
        private ShopPhase shopPhase;
        private BattleFlowState battleFlowState;

        private int frameCounter;
        private int startupFadeFrames = 18;
        private int modeCursor;
        private int languageCursor;
        private int nameCursorRow;
        private int nameCursorColumn;
        private string playerNameBuffer = string.Empty;
        private int saveSlotCursor;
        private int activeSaveSlot;
        private int movementCooldown;
        private bool isFieldDialogOpen;
        private bool isFieldStatusVisible;
        private Vector2Int fieldMovementAnimationDirection = Vector2Int.zero;
        private int fieldMovementAnimationFramesRemaining;
        private int fieldEncounterStepsRemaining = 7;
        private int encounterTransitionFrames;
        private int enemyHitFlashFramesRemaining;
        private int battleCursorRow;
        private int battleCursorColumn;
        private int battleListCursor;
        private int battleListScroll;
        private int shopPromptCursor;
        private int shopItemCursor;
        private int shopPageIndex;
        private string battleMessage = DefaultBattleMessage;
        private string shopMessage = ShopWelcomeMessage;
        private string menuNotice = string.Empty;
        private int menuNoticeFrames;
        private List<string> activeFieldDialogPages = new List<string>();
        private int activeFieldDialogPageIndex;
        private string activeFieldDialogPortraitAssetName = string.Empty;
        private List<SaveSlotSummary> saveSlotSummaries = new List<SaveSlotSummary>();
        private BgmTrack currentBgmTrack = BgmTrack.MainMenu;

        public DragonGlareGameSession()
        {
            player = PlayerProgress.CreateDefault(PlayerStartTile, UiLanguage.Japanese);
            map = MapFactory.CreateDefaultMap();
            currentFieldMap = FieldMapId.Hub;
            selectedLanguage = UiLanguage.Japanese;
            playerFacingDirection = FacingDirection.Down;
            gameState = GameState.ModeSelect;
            battleFlowState = BattleFlowState.CommandSelection;
            shopPhase = ShopPhase.Welcome;
            RefreshSaveSlotSummaries();
            UpdateBgmTrack();
        }

        public GameState State { get { return gameState; } }
        public UiLanguage SelectedLanguage { get { return selectedLanguage; } }
        public PlayerProgress Player { get { return player; } }
        public int[,] Map { get { return map; } }
        public FieldMapId CurrentFieldMap { get { return currentFieldMap; } }
        public FacingDirection PlayerFacingDirection { get { return playerFacingDirection; } }
        public BattleEncounter CurrentEncounter { get { return currentEncounter; } }
        public BattleFlowState BattleFlowState { get { return battleFlowState; } }
        public string BattleMessage { get { return battleMessage; } }
        public ShopPhase ShopPhase { get { return shopPhase; } }
        public string ShopMessage { get { return shopMessage; } }
        public string MenuNotice { get { return menuNotice; } }
        public SaveSlotSelectionMode SaveSlotSelectionMode { get { return saveSlotSelectionMode; } }
        public IList<SaveSlotSummary> SaveSlotSummaries { get { return saveSlotSummaries; } }
        public int ActiveSaveSlot { get { return activeSaveSlot; } }
        public int ModeCursor { get { return modeCursor; } }
        public int LanguageCursor { get { return languageCursor; } }
        public int NameCursorRow { get { return nameCursorRow; } }
        public int NameCursorColumn { get { return nameCursorColumn; } }
        public string PlayerNameBuffer { get { return playerNameBuffer; } }
        public int SaveSlotCursor { get { return saveSlotCursor; } }
        public int BattleCursorRow { get { return battleCursorRow; } }
        public int BattleCursorColumn { get { return battleCursorColumn; } }
        public int BattleListCursor { get { return battleListCursor; } }
        public int BattleListScroll { get { return battleListScroll; } }
        public int ShopPromptCursor { get { return shopPromptCursor; } }
        public int ShopItemCursor { get { return shopItemCursor; } }
        public int ShopPageIndex { get { return shopPageIndex; } }
        public bool IsFieldDialogOpen { get { return isFieldDialogOpen; } }
        public bool IsFieldStatusVisible { get { return isFieldStatusVisible; } }
        public string ActiveFieldDialogPortraitAssetName { get { return activeFieldDialogPortraitAssetName; } }
        public int StartupFadeFrames { get { return startupFadeFrames; } }
        public int EncounterTransitionFrames { get { return encounterTransitionFrames; } }
        public int EnemyHitFlashFramesRemaining { get { return enemyHitFlashFramesRemaining; } }
        public Vector2Int FieldMovementAnimationDirection { get { return fieldMovementAnimationDirection; } }
        public int FieldMovementAnimationFramesRemaining { get { return fieldMovementAnimationFramesRemaining; } }
        public int FieldMovementAnimationFrameDuration { get { return FieldMovementAnimationDuration; } }
        public BgmTrack CurrentBgmTrack { get { return currentBgmTrack; } }

        public void Tick(InputSnapshot input)
        {
            frameCounter++;
            UpdateFieldMovementAnimation();
            UpdateBattleVisualEffects();
            RunIntegrityChecks();

            if (startupFadeFrames > 0)
            {
                startupFadeFrames--;
            }

            if (menuNoticeFrames > 0)
            {
                menuNoticeFrames--;
                if (menuNoticeFrames == 0)
                {
                    menuNotice = string.Empty;
                }
            }

            switch (gameState)
            {
                case GameState.ModeSelect:
                    UpdateModeSelect(input);
                    break;
                case GameState.LanguageSelection:
                    UpdateLanguageSelection(input);
                    break;
                case GameState.NameInput:
                    UpdateNameInput(input);
                    break;
                case GameState.SaveSlotSelection:
                    UpdateSaveSlotSelection(input);
                    break;
                case GameState.Field:
                    UpdateField(input);
                    break;
                case GameState.EncounterTransition:
                    UpdateEncounterTransition();
                    break;
                case GameState.Battle:
                    UpdateBattle(input);
                    break;
                case GameState.ShopBuy:
                    UpdateShopBuy(input);
                    break;
            }

            UpdateBgmTrack();
        }

        public List<SoundEffect> DrainPendingSoundEffects()
        {
            List<SoundEffect> sounds = new List<SoundEffect>();
            while (pendingSoundEffects.Count > 0)
            {
                sounds.Add(pendingSoundEffects.Dequeue());
            }

            return sounds;
        }

        public IEnumerable<FieldEventDefinition> GetCurrentFieldEvents()
        {
            return GameContent.FieldEvents.Where(fieldEvent => fieldEvent.MapId == currentFieldMap);
        }

        public string GetCurrentFieldDialogPage()
        {
            if (!isFieldDialogOpen || activeFieldDialogPages.Count == 0)
            {
                return string.Empty;
            }

            return activeFieldDialogPages[Mathf.Clamp(activeFieldDialogPageIndex, 0, activeFieldDialogPages.Count - 1)];
        }

        public string GetDisplayPlayerName()
        {
            if (!string.IsNullOrEmpty(player.Name))
            {
                return player.Name;
            }

            return string.IsNullOrEmpty(playerNameBuffer) ? "のりたま" : playerNameBuffer;
        }

        public WeaponDefinition GetEquippedWeapon()
        {
            return GameContent.GetWeaponById(player.EquippedWeaponId);
        }

        public ArmorDefinition GetEquippedArmor()
        {
            return GameContent.GetArmorById(player.EquippedArmorId);
        }

        public string GetEquippedWeaponName()
        {
            WeaponDefinition weapon = GetEquippedWeapon();
            return weapon != null ? weapon.Name : "なし";
        }

        public string GetEquippedArmorName()
        {
            ArmorDefinition armor = GetEquippedArmor();
            return armor != null ? armor.Name : "なし";
        }

        public int GetTotalAttack()
        {
            return battleService.GetPlayerAttack(player, GetEquippedWeapon());
        }

        public int GetTotalDefense()
        {
            return battleService.GetPlayerDefense(player, GetEquippedArmor());
        }

        public string GetExperienceSummary()
        {
            if (player.Level >= PlayerProgress.MaxLevelValue)
            {
                return "MAX";
            }

            int current = progressionService.GetExperienceIntoCurrentLevel(player);
            int needed = progressionService.GetExperienceNeededForNextLevel(player);
            return current + "/" + needed;
        }

        public string GetFieldHelpLine1()
        {
            return selectedLanguage == UiLanguage.English ? "ARROWS / WASD: MOVE" : "やじるし / WASD: いどう";
        }

        public string GetFieldHelpLine2()
        {
            return selectedLanguage == UiLanguage.English ? "Z: TALK / CHECK   X: STATUS" : "Z: はなす・しらべる   X: ステータス";
        }

        public string GetFieldHelpLine3()
        {
            if (selectedLanguage == UiLanguage.English)
            {
                switch (currentFieldMap)
                {
                    case FieldMapId.Castle:
                        return "B: BATTLE   V: SHOP   AREA: CASTLE";
                    case FieldMapId.Field:
                        return "B: BATTLE   V: SHOP   AREA: FIELD";
                    default:
                        return "B: BATTLE   V: SHOP   AREA: HUB";
                }
            }

            switch (currentFieldMap)
            {
                case FieldMapId.Castle:
                    return "B: バトル   V: ショップ   いま: しろ";
                case FieldMapId.Field:
                    return "B: バトル   V: ショップ   いま: フィールド";
                default:
                    return "B: バトル   V: ショップ   いま: ハブ";
            }
        }

        public IReadOnlyList<BattleSelectionEntry> GetActiveBattleSelectionEntries()
        {
            if (battleFlowState == BattleFlowState.ItemSelection)
            {
                return GetBattleItemEntries();
            }

            if (battleFlowState == BattleFlowState.EquipmentSelection)
            {
                return GetBattleEquipmentEntries();
            }

            return new List<BattleSelectionEntry>();
        }

        public string GetBattleSelectionTitle()
        {
            switch (battleFlowState)
            {
                case BattleFlowState.ItemSelection:
                    return selectedLanguage == UiLanguage.English ? "ITEM" : "どうぐ";
                case BattleFlowState.EquipmentSelection:
                    return selectedLanguage == UiLanguage.English ? "EQUIP" : "そうび";
                default:
                    return selectedLanguage == UiLanguage.English ? "COMMAND" : "こうどう";
            }
        }

        public string GetBattleSelectionCounterText()
        {
            IReadOnlyList<BattleSelectionEntry> entries = GetActiveBattleSelectionEntries();
            if (entries.Count == 0)
            {
                return "0/0";
            }

            return (battleListCursor + 1) + "/" + entries.Count;
        }

        public IReadOnlyList<ShopMenuEntry> GetShopVisibleEntries()
        {
            int pageStartIndex = shopPageIndex * ShopItemsPerPage;
            List<ShopMenuEntry> entries = GameContent.ShopCatalog
                .Skip(pageStartIndex)
                .Take(ShopItemsPerPage)
                .Select(item => new ShopMenuEntry
                {
                    Type = ShopMenuEntry.MenuEntryType.Item,
                    Label = item.Name,
                    Item = item
                })
                .ToList();

            if (shopPageIndex > 0)
            {
                entries.Add(new ShopMenuEntry
                {
                    Type = ShopMenuEntry.MenuEntryType.PreviousPage,
                    Label = "まえへ"
                });
            }

            if (shopPageIndex + 1 < GetShopPageCount())
            {
                entries.Add(new ShopMenuEntry
                {
                    Type = ShopMenuEntry.MenuEntryType.NextPage,
                    Label = "つぎへ"
                });
            }

            entries.Add(new ShopMenuEntry
            {
                Type = ShopMenuEntry.MenuEntryType.Quit,
                Label = "やめる"
            });

            return entries;
        }

        public int GetShopPageCount()
        {
            return Mathf.Max(1, (GameContent.ShopCatalog.Length + ShopItemsPerPage - 1) / ShopItemsPerPage);
        }

        public string GetBattleCommandLabel(int row, int column)
        {
            return GameContent.GetBattleCommandLabel(selectedLanguage, row, column);
        }

        private void RunIntegrityChecks()
        {
            if (frameCounter % 30 == 0)
            {
                player.RekeySensitiveValues();
                if (currentEncounter != null)
                {
                    currentEncounter.RekeySensitiveValues();
                }

                if (pendingEncounter != null)
                {
                    pendingEncounter.RekeySensitiveValues();
                }
            }

            if (frameCounter % 120 != 0)
            {
                return;
            }

            player.ValidateIntegrity();
            if (currentEncounter != null)
            {
                currentEncounter.ValidateIntegrity();
            }

            if (pendingEncounter != null)
            {
                pendingEncounter.ValidateIntegrity();
            }
        }

        private void UpdateModeSelect(InputSnapshot input)
        {
            if (input.UpPressed)
            {
                modeCursor = 0;
            }
            else if (input.DownPressed)
            {
                modeCursor = 1;
            }

            if (!input.ConfirmPressed)
            {
                return;
            }

            if (modeCursor == 0)
            {
                StartNewGame();
                return;
            }

            OpenSaveSlotSelection(SaveSlotSelectionMode.Load);
        }

        private void UpdateLanguageSelection(InputSnapshot input)
        {
            if (input.UpPressed)
            {
                languageCursor = 0;
            }
            else if (input.DownPressed)
            {
                languageCursor = 1;
            }

            if (input.ConfirmPressed)
            {
                selectedLanguage = languageCursor == 0 ? UiLanguage.Japanese : UiLanguage.English;
                player.Language = selectedLanguage;
                playerNameBuffer = string.Empty;
                nameCursorRow = 0;
                nameCursorColumn = 0;
                ChangeGameState(GameState.NameInput);
            }

            if (input.CancelPressed)
            {
                ChangeGameState(GameState.ModeSelect);
            }
        }

        private void UpdateNameInput(InputSnapshot input)
        {
            if (input.UpPressed)
            {
                MoveNameCursor(0, -1);
            }
            else if (input.DownPressed)
            {
                MoveNameCursor(0, 1);
            }
            else if (input.LeftPressed)
            {
                MoveNameCursor(-1, 0);
            }
            else if (input.RightPressed)
            {
                MoveNameCursor(1, 0);
            }

            if (input.BackspacePressed)
            {
                RemoveLastCharacter();
            }

            if (input.CancelPressed)
            {
                ChangeGameState(GameState.LanguageSelection);
                return;
            }

            if (input.ConfirmPressed)
            {
                AddSelectedCharacter();
            }
        }

        private void UpdateSaveSlotSelection(InputSnapshot input)
        {
            if (input.UpPressed)
            {
                saveSlotCursor = Mathf.Max(0, saveSlotCursor - 1);
            }
            else if (input.DownPressed)
            {
                saveSlotCursor = Mathf.Min(SaveService.SlotCount - 1, saveSlotCursor + 1);
            }

            if (input.CancelPressed)
            {
                ChangeGameState(saveSlotSelectionMode == SaveSlotSelectionMode.Save ? GameState.NameInput : GameState.ModeSelect);
                return;
            }

            if (!input.ConfirmPressed)
            {
                return;
            }

            int selectedSlot = saveSlotCursor + 1;
            if (saveSlotSelectionMode == SaveSlotSelectionMode.Load)
            {
                if (TryLoadGame(selectedSlot))
                {
                    ChangeGameState(GameState.Field);
                    return;
                }

                RefreshSaveSlotSummaries();
                ShowTransientNotice(saveService.LastFailureReason == SaveLoadFailureReason.InvalidSignature
                    ? "SAVE DATA INVALID / セーブデータが改ざんされています"
                    : saveService.LastFailureReason == SaveLoadFailureReason.InvalidFormat
                        ? "SAVE DATA ERROR / セーブデータが壊れています"
                        : "NO SAVE DATA / セーブデータがありません");
                EnqueueSound(SoundEffect.Collision);
                return;
            }

            activeSaveSlot = selectedSlot;
            SaveGame();
            ChangeGameState(GameState.Field);
        }

        private void UpdateField(InputSnapshot input)
        {
            if (isFieldDialogOpen)
            {
                if (input.ConfirmPressed)
                {
                    AdvanceFieldDialog();
                }
                else if (input.CancelPressed)
                {
                    CloseFieldDialog();
                }

                return;
            }

            if (input.OpenBattlePressed)
            {
                EnterBattle();
                return;
            }

            if (input.OpenShopPressed)
            {
                EnterShopBuy();
                return;
            }

            if (input.ToggleStatusPressed)
            {
                isFieldStatusVisible = !isFieldStatusVisible;
                return;
            }

            if (movementCooldown > 0)
            {
                movementCooldown--;
            }

            Vector2Int movement = Vector2Int.zero;
            if (input.UpHeld)
            {
                movement = new Vector2Int(0, -1);
            }
            else if (input.DownHeld)
            {
                movement = new Vector2Int(0, 1);
            }
            else if (input.LeftHeld)
            {
                movement = new Vector2Int(-1, 0);
            }
            else if (input.RightHeld)
            {
                movement = new Vector2Int(1, 0);
            }

            if (movement != Vector2Int.zero && movementCooldown == 0)
            {
                SetPlayerFacingDirection(movement);
                bool moved = TryMovePlayer(movement);
                if (!moved)
                {
                    EnqueueSound(SoundEffect.Collision);
                }

                movementCooldown = 6;
                if (gameState != GameState.Field)
                {
                    return;
                }
            }

            if (input.ConfirmPressed)
            {
                FieldEventDefinition fieldEvent = GetInteractableFieldEvent();
                if (fieldEvent != null)
                {
                    OpenFieldDialog(fieldEvent);
                }
            }
        }

        private void UpdateBattle(InputSnapshot input)
        {
            if (currentEncounter == null)
            {
                ResetBattleState();
                ChangeGameState(GameState.Field);
                return;
            }

            if (battleFlowState == BattleFlowState.Intro)
            {
                if (input.ConfirmPressed)
                {
                    battleFlowState = BattleFlowState.CommandSelection;
                    battleMessage = GetBattleCommandPromptMessage();
                }

                return;
            }

            if (battleFlowState == BattleFlowState.ItemSelection || battleFlowState == BattleFlowState.EquipmentSelection)
            {
                UpdateBattleSelectionMenu(input);
                return;
            }

            if (battleFlowState != BattleFlowState.CommandSelection)
            {
                if (input.ConfirmPressed || input.CancelPressed)
                {
                    FinishBattle();
                }

                return;
            }

            UpdateBattleCommandCursor(input);

            if (input.CancelPressed)
            {
                battleMessage = BattleEscapeMessage;
                battleFlowState = BattleFlowState.Escaped;
                PersistProgress();
                return;
            }

            if (!input.ConfirmPressed)
            {
                return;
            }

            BattleActionType action = GameContent.BattleCommandGrid[battleCursorRow, battleCursorColumn];
            if (action == BattleActionType.Item)
            {
                OpenBattleSelectionMenu(BattleFlowState.ItemSelection);
                return;
            }

            if (action == BattleActionType.Equip)
            {
                OpenBattleSelectionMenu(BattleFlowState.EquipmentSelection);
                return;
            }

            BattleTurnResolution result = battleService.ResolveTurn(
                player,
                currentEncounter,
                action,
                GetEquippedWeapon(),
                GetEquippedArmor(),
                null,
                null,
                random);

            ApplyBattleResolution(result);
        }

        private void UpdateBattleCommandCursor(InputSnapshot input)
        {
            if (input.UpPressed)
            {
                battleCursorRow = Mathf.Max(0, battleCursorRow - 1);
            }
            else if (input.DownPressed)
            {
                battleCursorRow = Mathf.Min(GameContent.BattleCommandGrid.GetLength(0) - 1, battleCursorRow + 1);
            }
            else if (input.LeftPressed)
            {
                battleCursorColumn = Mathf.Max(0, battleCursorColumn - 1);
            }
            else if (input.RightPressed)
            {
                battleCursorColumn = Mathf.Min(GameContent.BattleCommandGrid.GetLength(1) - 1, battleCursorColumn + 1);
            }
        }

        private void UpdateBattleSelectionMenu(InputSnapshot input)
        {
            IReadOnlyList<BattleSelectionEntry> entries = GetActiveBattleSelectionEntries();
            if (entries.Count == 0)
            {
                CloseBattleSelectionMenu(battleFlowState == BattleFlowState.ItemSelection ? GetBattleNoItemsMessage() : GetBattleNoEquipmentMessage());
                return;
            }

            if (input.UpPressed)
            {
                MoveBattleSelectionCursor(-1, entries.Count);
            }
            else if (input.DownPressed)
            {
                MoveBattleSelectionCursor(1, entries.Count);
            }

            if (input.CancelPressed)
            {
                CloseBattleSelectionMenu(null);
                return;
            }

            if (!input.ConfirmPressed || currentEncounter == null)
            {
                return;
            }

            BattleSelectionEntry selectedEntry = entries[battleListCursor];
            BattleActionType action = battleFlowState == BattleFlowState.ItemSelection ? BattleActionType.Item : BattleActionType.Equip;

            BattleTurnResolution result = battleService.ResolveTurn(
                player,
                currentEncounter,
                action,
                GetEquippedWeapon(),
                GetEquippedArmor(),
                selectedEntry.Consumable,
                selectedEntry.Equipment,
                random);

            ApplyBattleResolution(result);
            if (result.Outcome == BattleOutcome.Ongoing)
            {
                battleFlowState = BattleFlowState.CommandSelection;
            }
        }

        private void UpdateEncounterTransition()
        {
            if (encounterTransitionFrames > 0)
            {
                encounterTransitionFrames--;
            }

            if (encounterTransitionFrames > 0)
            {
                return;
            }

            if (pendingEncounter == null)
            {
                ChangeGameState(GameState.Field);
                return;
            }

            currentEncounter = pendingEncounter;
            pendingEncounter = null;
            ResetBattleSelectionState();
            battleFlowState = BattleFlowState.Intro;
            battleMessage = GetBattleEncounterMessage(currentEncounter.Enemy.Name);
            ChangeGameState(GameState.Battle);
        }

        private void UpdateShopBuy(InputSnapshot input)
        {
            if (shopPhase == ShopPhase.Welcome)
            {
                if (input.UpPressed || input.DownPressed)
                {
                    shopPromptCursor = 1 - shopPromptCursor;
                }

                if (input.CancelPressed)
                {
                    ChangeGameState(GameState.Field);
                    return;
                }

                if (!input.ConfirmPressed)
                {
                    return;
                }

                if (shopPromptCursor == 0)
                {
                    OpenShopCatalog();
                    return;
                }

                ChangeGameState(GameState.Field);
                return;
            }

            IReadOnlyList<ShopMenuEntry> visibleEntries = GetShopVisibleEntries();
            int maxIndex = visibleEntries.Count - 1;
            if (input.UpPressed)
            {
                shopItemCursor = Mathf.Max(0, shopItemCursor - 1);
            }
            else if (input.DownPressed)
            {
                shopItemCursor = Mathf.Min(maxIndex, shopItemCursor + 1);
            }

            if (input.CancelPressed)
            {
                ReturnToShopPrompt(ShopReturnMessage);
                return;
            }

            if (!input.ConfirmPressed)
            {
                return;
            }

            ShopMenuEntry selectedEntry = visibleEntries[shopItemCursor];
            if (selectedEntry.Type == ShopMenuEntry.MenuEntryType.PreviousPage)
            {
                ChangeShopPage(-1);
                return;
            }

            if (selectedEntry.Type == ShopMenuEntry.MenuEntryType.NextPage)
            {
                ChangeShopPage(1);
                return;
            }

            if (selectedEntry.Type == ShopMenuEntry.MenuEntryType.Quit)
            {
                ReturnToShopPrompt(ShopFarewellMessage);
                return;
            }

            if (selectedEntry.Item == null)
            {
                return;
            }

            ShopPurchaseResult result = shopService.PurchaseEquipment(player, selectedEntry.Item, GetEquippedWeapon(), GetEquippedArmor());
            shopMessage = result.Message;
            if (result.Success)
            {
                PersistProgress();
            }
        }

        private void StartNewGame()
        {
            selectedLanguage = UiLanguage.Japanese;
            languageCursor = 0;
            nameCursorRow = 0;
            nameCursorColumn = 0;
            activeSaveSlot = 0;
            saveSlotCursor = 0;
            playerNameBuffer = string.Empty;
            ApplyExplorationSession(progressionService.CreateNewPlayer(UiLanguage.Japanese, PlayerStartTile), FieldMapId.Hub);
            ChangeGameState(GameState.LanguageSelection);
        }

        private bool TryLoadGame(int slotNumber)
        {
            SaveData save;
            if (!saveService.TryLoadSlot(slotNumber, out save) || save == null)
            {
                return false;
            }

            activeSaveSlot = slotNumber;
            RestoredSaveState restored = SaveDataMapper.Restore(save, PlayerStartTile);
            PlayerProgress loadedPlayer = restored.Player;
            loadedPlayer.Name = TrimPlayerName(loadedPlayer.Name);

            FieldMapId loadedMapId = restored.MapId;
            int[,] loadedMap = MapFactory.CreateMap(loadedMapId);
            if (!IsWalkableTile(loadedMap, loadedPlayer.TilePosition) || IsBlockedByFieldEvent(loadedMapId, loadedPlayer.TilePosition))
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
                if (string.IsNullOrEmpty(playerNameBuffer))
                {
                    return;
                }

                player.Name = TrimPlayerName(playerNameBuffer);
            }

            if (string.IsNullOrEmpty(player.Name))
            {
                return;
            }

            if (activeSaveSlot < 1 || activeSaveSlot > SaveService.SlotCount)
            {
                return;
            }

            player.Language = selectedLanguage;
            SaveData save = SaveDataMapper.Create(player, selectedLanguage, currentFieldMap, activeSaveSlot);

            try
            {
                saveService.SaveSlot(activeSaveSlot, save);
                RefreshSaveSlotSummaries();
            }
            catch
            {
            }
        }

        private void ApplyExplorationSession(PlayerProgress nextPlayer, FieldMapId mapId)
        {
            player = nextPlayer;
            SetFieldMap(mapId);
            playerNameBuffer = TrimPlayerName(player.Name);
            ResetFieldUiState();
            ResetBattleState();
            ResetShopState();
        }

        private void SetFieldMap(FieldMapId mapId)
        {
            currentFieldMap = mapId;
            map = MapFactory.CreateMap(mapId);
            ResetFieldMovementAnimation();
            ResetEncounterCounter();
        }

        private void ChangeGameState(GameState nextState)
        {
            gameState = nextState;
            UpdateBgmTrack();
        }

        private void OpenSaveSlotSelection(SaveSlotSelectionMode mode)
        {
            saveSlotSelectionMode = mode;
            RefreshSaveSlotSummaries();
            saveSlotCursor = Mathf.Clamp(activeSaveSlot - 1, 0, SaveService.SlotCount - 1);
            if (mode == SaveSlotSelectionMode.Save && activeSaveSlot == 0)
            {
                saveSlotCursor = 0;
            }

            menuNotice = string.Empty;
            menuNoticeFrames = 0;
            ChangeGameState(GameState.SaveSlotSelection);
        }

        private void RefreshSaveSlotSummaries()
        {
            saveSlotSummaries = saveService.GetSlotSummaries();
        }

        private void ShowTransientNotice(string message)
        {
            menuNotice = message;
            menuNoticeFrames = 180;
        }

        private void ResetFieldUiState()
        {
            CloseFieldDialog();
            isFieldStatusVisible = false;
            movementCooldown = 0;
            playerFacingDirection = FacingDirection.Down;
        }

        private void ResetBattleSelectionState()
        {
            battleFlowState = BattleFlowState.CommandSelection;
            battleCursorRow = 0;
            battleCursorColumn = 0;
            battleListCursor = 0;
            battleListScroll = 0;
            enemyHitFlashFramesRemaining = 0;
        }

        private void ResetBattleState()
        {
            currentEncounter = null;
            pendingEncounter = null;
            encounterTransitionFrames = 0;
            ResetBattleSelectionState();
            battleMessage = DefaultBattleMessage;
        }

        private void ResetShopState()
        {
            shopPhase = ShopPhase.Welcome;
            shopPromptCursor = 0;
            shopPageIndex = 0;
            shopItemCursor = 0;
            shopMessage = ShopWelcomeMessage;
        }

        private void OpenShopCatalog()
        {
            shopPhase = ShopPhase.BuyList;
            shopPageIndex = 0;
            shopItemCursor = 0;
            shopMessage = ShopBrowseMessage;
        }

        private void ReturnToShopPrompt(string message)
        {
            shopPhase = ShopPhase.Welcome;
            shopPromptCursor = 0;
            shopItemCursor = 0;
            shopPageIndex = 0;
            shopMessage = message;
        }

        private void ChangeShopPage(int delta)
        {
            shopPageIndex = Mathf.Clamp(shopPageIndex + delta, 0, Mathf.Max(0, GetShopPageCount() - 1));
            shopItemCursor = 0;
            shopMessage = ShopBrowseMessage;
        }

        private void ResetEncounterCounter()
        {
            fieldEncounterStepsRemaining = random.Next(6, 12);
        }

        private void StartFieldMovementAnimation(Vector2Int movement)
        {
            fieldMovementAnimationDirection = movement;
            fieldMovementAnimationFramesRemaining = FieldMovementAnimationDuration;
        }

        private void UpdateFieldMovementAnimation()
        {
            if (fieldMovementAnimationFramesRemaining <= 0)
            {
                fieldMovementAnimationDirection = Vector2Int.zero;
                return;
            }

            fieldMovementAnimationFramesRemaining--;
            if (fieldMovementAnimationFramesRemaining == 0)
            {
                fieldMovementAnimationDirection = Vector2Int.zero;
            }
        }

        private void ResetFieldMovementAnimation()
        {
            fieldMovementAnimationDirection = Vector2Int.zero;
            fieldMovementAnimationFramesRemaining = 0;
        }

        private void UpdateBattleVisualEffects()
        {
            if (enemyHitFlashFramesRemaining > 0)
            {
                enemyHitFlashFramesRemaining--;
            }
        }

        private void ApplyBattleVisualEffects(BattleTurnResolution result)
        {
            enemyHitFlashFramesRemaining = 0;
            if (currentEncounter == null || currentEncounter.CurrentHp <= 0)
            {
                return;
            }

            for (int index = 0; index < result.Steps.Count; index++)
            {
                if (result.Steps[index].VisualCue == BattleVisualCue.EnemyHit)
                {
                    enemyHitFlashFramesRemaining = Mathf.Max(enemyHitFlashFramesRemaining, result.Steps[index].AnimationFrames);
                }
            }
        }

        private void ApplyBattleResolution(BattleTurnResolution result)
        {
            ApplyBattleVisualEffects(result);
            string resultMessage = string.Join("\n", result.Steps.Select(step => step.Message).Where(message => !string.IsNullOrEmpty(message)).ToArray());

            switch (result.Outcome)
            {
                case BattleOutcome.Victory:
                    battleMessage = resultMessage + "\n" + progressionService.ApplyBattleRewards(player, currentEncounter.Enemy, random);
                    battleFlowState = BattleFlowState.Victory;
                    PersistProgress();
                    break;
                case BattleOutcome.Defeat:
                    battleMessage = resultMessage + "\n" + progressionService.ApplyDefeatPenalty(player, PlayerStartTile);
                    SetFieldMap(FieldMapId.Hub);
                    battleFlowState = BattleFlowState.Defeat;
                    PersistProgress();
                    break;
                case BattleOutcome.Escaped:
                    battleMessage = resultMessage;
                    battleFlowState = BattleFlowState.Escaped;
                    PersistProgress();
                    break;
                case BattleOutcome.Invalid:
                    battleMessage = resultMessage;
                    break;
                default:
                    battleMessage = resultMessage;
                    battleFlowState = BattleFlowState.CommandSelection;
                    PersistProgress();
                    break;
            }
        }

        private void OpenBattleSelectionMenu(BattleFlowState nextState)
        {
            battleFlowState = nextState;
            battleListCursor = 0;
            battleListScroll = 0;

            IReadOnlyList<BattleSelectionEntry> entries = GetActiveBattleSelectionEntries();
            if (entries.Count == 0)
            {
                CloseBattleSelectionMenu(nextState == BattleFlowState.ItemSelection ? GetBattleNoItemsMessage() : GetBattleNoEquipmentMessage());
                return;
            }

            battleMessage = nextState == BattleFlowState.ItemSelection ? GetBattleItemPromptMessage() : GetBattleEquipmentPromptMessage();
        }

        private void CloseBattleSelectionMenu(string message)
        {
            battleFlowState = BattleFlowState.CommandSelection;
            battleListCursor = 0;
            battleListScroll = 0;
            battleMessage = string.IsNullOrEmpty(message) ? GetBattleCommandPromptMessage() : message;
        }

        private void MoveBattleSelectionCursor(int delta, int itemCount)
        {
            battleListCursor = Mathf.Clamp(battleListCursor + delta, 0, itemCount - 1);
            if (battleListCursor < battleListScroll)
            {
                battleListScroll = battleListCursor;
                return;
            }

            if (battleListCursor >= battleListScroll + BattleSelectionVisibleRows)
            {
                battleListScroll = battleListCursor - BattleSelectionVisibleRows + 1;
            }
        }

        private IReadOnlyList<BattleSelectionEntry> GetBattleItemEntries()
        {
            return GameContent.ConsumableCatalog
                .Where(item => player.GetItemCount(item.Id) > 0)
                .Select(item => new BattleSelectionEntry
                {
                    Label = item.Name,
                    Detail = GetBattleConsumableDetail(item),
                    Badge = GetBattleCountBadge(player.GetItemCount(item.Id)),
                    Consumable = item
                })
                .ToList();
        }

        private IReadOnlyList<BattleSelectionEntry> GetBattleEquipmentEntries()
        {
            IEnumerable<BattleSelectionEntry> weaponEntries = GameContent.WeaponCatalog
                .Where(item => player.GetItemCount(item.Id) > 0 && !string.Equals(player.EquippedWeaponId, item.Id, StringComparison.Ordinal))
                .Select(item => new BattleSelectionEntry
                {
                    Label = item.Name,
                    Detail = GetBattleEquipmentDetail(item),
                    Badge = GetBattleCountBadge(player.GetItemCount(item.Id)),
                    Equipment = item
                });

            IEnumerable<BattleSelectionEntry> armorEntries = GameContent.ArmorCatalog
                .Where(item => player.GetItemCount(item.Id) > 0 && !string.Equals(player.EquippedArmorId, item.Id, StringComparison.Ordinal))
                .Select(item => new BattleSelectionEntry
                {
                    Label = item.Name,
                    Detail = GetBattleEquipmentDetail(item),
                    Badge = GetBattleCountBadge(player.GetItemCount(item.Id)),
                    Equipment = item
                });

            return weaponEntries.Concat(armorEntries).ToList();
        }

        private string GetBattleConsumableDetail(ConsumableDefinition item)
        {
            switch (item.EffectType)
            {
                case ConsumableEffectType.HealHp:
                    return "HP+" + item.Amount;
                case ConsumableEffectType.HealMp:
                    return "MP+" + item.Amount;
                case ConsumableEffectType.DamageEnemy:
                    return selectedLanguage == UiLanguage.English ? "DMG " + item.Amount : "与D " + item.Amount;
                default:
                    return item.Description;
            }
        }

        private string GetBattleEquipmentDetail(IEquipmentDefinition equipment)
        {
            if (equipment.Slot == EquipmentSlot.Weapon)
            {
                int difference = equipment.AttackBonus - (GetEquippedWeapon() != null ? GetEquippedWeapon().AttackBonus : 0);
                return "ATK " + equipment.AttackBonus + FormatSignedStat(difference);
            }

            int armorDifference = equipment.DefenseBonus - (GetEquippedArmor() != null ? GetEquippedArmor().DefenseBonus : 0);
            return "DEF " + equipment.DefenseBonus + FormatSignedStat(armorDifference);
        }

        private static string FormatSignedStat(int value)
        {
            if (value > 0)
            {
                return " (+" + value + ")";
            }

            if (value < 0)
            {
                return " (" + value + ")";
            }

            return string.Empty;
        }

        private string GetBattleCountBadge(int count)
        {
            return selectedLanguage == UiLanguage.English ? "x" + count : "×" + count;
        }

        private void EnterBattle()
        {
            StartEncounterTransition(battleService.CreateEncounter(random, currentFieldMap, player.Level));
        }

        private void EnterShopBuy()
        {
            ResetShopState();
            ChangeGameState(GameState.ShopBuy);
            EnqueueSound(SoundEffect.Dialog);
        }

        private void FinishBattle()
        {
            ResetEncounterCounter();
            ResetBattleState();
            ChangeGameState(GameState.Field);
            PersistProgress();
        }

        private void StartEncounterTransition(BattleEncounter encounter)
        {
            pendingEncounter = encounter;
            encounterTransitionFrames = EncounterTransitionDuration;
            ResetBattleSelectionState();
            ResetEncounterCounter();
            ChangeGameState(GameState.EncounterTransition);
            EnqueueSound(SoundEffect.Dialog);
        }

        private bool TryMovePlayer(Vector2Int movement)
        {
            Vector2Int target = player.TilePosition + movement;
            if (!IsWalkableTile(target) || IsBlockedByFieldEvent(target))
            {
                return false;
            }

            player.TilePosition = target;
            StartFieldMovementAnimation(movement);
            if (TryTransitionFromTile(target))
            {
                return true;
            }

            if (TryTriggerRandomEncounter())
            {
                PersistProgress();
                return true;
            }

            PersistProgress();
            return true;
        }

        private bool TryTriggerRandomEncounter()
        {
            if (currentFieldMap != FieldMapId.Field)
            {
                return false;
            }

            int tileId = map[player.TilePosition.y, player.TilePosition.x];
            if (tileId == MapFactory.FieldGateTile)
            {
                return false;
            }

            fieldEncounterStepsRemaining -= tileId == MapFactory.GrassTile ? 2 : 1;
            if (fieldEncounterStepsRemaining > 0)
            {
                return false;
            }

            StartEncounterTransition(battleService.CreateEncounter(random, currentFieldMap, player.Level));
            return true;
        }

        private void MoveNameCursor(int deltaX, int deltaY)
        {
            string[][] table = GameContent.GetNameTable(selectedLanguage);
            nameCursorRow = Mathf.Clamp(nameCursorRow + deltaY, 0, table.Length - 1);
            int maxColumn = table[nameCursorRow].Length - 1;
            nameCursorColumn = Mathf.Clamp(nameCursorColumn + deltaX, 0, maxColumn);
        }

        private void AddSelectedCharacter()
        {
            string[][] table = GameContent.GetNameTable(selectedLanguage);
            string selected = table[nameCursorRow][nameCursorColumn];
            string deleteToken = selectedLanguage == UiLanguage.Japanese ? "けす" : "DEL";
            string endToken = selectedLanguage == UiLanguage.Japanese ? "おわり" : "END";

            if (selected == deleteToken)
            {
                RemoveLastCharacter();
                return;
            }

            if (selected == endToken)
            {
                if (!string.IsNullOrEmpty(playerNameBuffer))
                {
                    player.Name = TrimPlayerName(playerNameBuffer);
                    OpenSaveSlotSelection(SaveSlotSelectionMode.Save);
                }

                return;
            }

            if (playerNameBuffer.Length < 10)
            {
                playerNameBuffer += selected;
            }
        }

        private void RemoveLastCharacter()
        {
            if (!string.IsNullOrEmpty(playerNameBuffer))
            {
                playerNameBuffer = playerNameBuffer.Substring(0, playerNameBuffer.Length - 1);
            }
        }

        private bool IsWalkableTile(Vector2Int tile)
        {
            return IsWalkableTile(map, tile);
        }

        private static bool IsWalkableTile(int[,] fieldMap, Vector2Int tile)
        {
            if (tile.x < 0 || tile.y < 0 || tile.x >= fieldMap.GetLength(1) || tile.y >= fieldMap.GetLength(0))
            {
                return false;
            }

            return fieldMap[tile.y, tile.x] != MapFactory.WallTile;
        }

        private void SetPlayerFacingDirection(Vector2Int movement)
        {
            if (movement.x < 0)
            {
                playerFacingDirection = FacingDirection.Left;
                return;
            }

            if (movement.x > 0)
            {
                playerFacingDirection = FacingDirection.Right;
                return;
            }

            if (movement.y < 0)
            {
                playerFacingDirection = FacingDirection.Up;
                return;
            }

            if (movement.y > 0)
            {
                playerFacingDirection = FacingDirection.Down;
            }
        }

        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private bool IsBlockedByFieldEvent(Vector2Int tile)
        {
            return IsBlockedByFieldEvent(currentFieldMap, tile);
        }

        private bool IsBlockedByFieldEvent(FieldMapId mapId, Vector2Int tile)
        {
            return GameContent.FieldEvents.Any(fieldEvent => fieldEvent.MapId == mapId && fieldEvent.BlocksMovement && fieldEvent.TilePosition == tile);
        }

        private FieldEventDefinition GetInteractableFieldEvent()
        {
            return GetCurrentFieldEvents().FirstOrDefault(fieldEvent => fieldEvent.TilePosition == player.TilePosition || IsAdjacent(player.TilePosition, fieldEvent.TilePosition));
        }

        private void OpenFieldDialog(FieldEventDefinition fieldEvent)
        {
            FieldInteractionResult result = fieldEventService.Interact(player, fieldEvent, selectedLanguage);
            activeFieldDialogPages = result.Pages.Where(page => !string.IsNullOrEmpty(page)).ToList();
            activeFieldDialogPageIndex = 0;
            activeFieldDialogPortraitAssetName = fieldEvent.PortraitAssetName;
            isFieldDialogOpen = activeFieldDialogPages.Count > 0;

            if (fieldEvent.ActionType == FieldEventActionType.Recover)
            {
                PersistProgress();
            }

            EnqueueSound(SoundEffect.Dialog);
        }

        private void AdvanceFieldDialog()
        {
            if (!isFieldDialogOpen)
            {
                return;
            }

            if (activeFieldDialogPageIndex < activeFieldDialogPages.Count - 1)
            {
                activeFieldDialogPageIndex++;
                EnqueueSound(SoundEffect.Dialog);
                return;
            }

            CloseFieldDialog();
        }

        private void CloseFieldDialog()
        {
            isFieldDialogOpen = false;
            activeFieldDialogPages.Clear();
            activeFieldDialogPageIndex = 0;
            activeFieldDialogPortraitAssetName = string.Empty;
        }

        private bool TryTransitionFromTile(Vector2Int tile)
        {
            FieldTransitionDefinition transition;
            if (!fieldTransitionService.TryGetTransition(currentFieldMap, tile, out transition))
            {
                return false;
            }

            SetFieldMap(transition.ToMapId);
            player.TilePosition = transition.DestinationTile;
            CloseFieldDialog();
            movementCooldown = 6;
            PersistProgress();
            return true;
        }

        private string GetBattleEncounterMessage(string enemyName)
        {
            return selectedLanguage == UiLanguage.English ? enemyName + " appears!" : enemyName + "が あらわれた！";
        }

        private string GetBattleCommandPromptMessage()
        {
            string displayName = GetDisplayPlayerName();
            return selectedLanguage == UiLanguage.English ? "What will " + displayName + " do?" : displayName + "は どうする？";
        }

        private string GetBattleItemPromptMessage()
        {
            return selectedLanguage == UiLanguage.English ? "Choose an item." : "なにを つかう？";
        }

        private string GetBattleEquipmentPromptMessage()
        {
            return selectedLanguage == UiLanguage.English ? "Choose gear." : "なにを そうびする？";
        }

        private string GetBattleNoItemsMessage()
        {
            return selectedLanguage == UiLanguage.English ? "You have no usable items." : "つかえる どうぐがない。";
        }

        private string GetBattleNoEquipmentMessage()
        {
            return selectedLanguage == UiLanguage.English ? "No gear to switch." : "つけかえられる そうびがない。";
        }

        private string TrimPlayerName(string name)
        {
            string trimmed = string.IsNullOrEmpty(name) ? string.Empty : name.Trim();
            return trimmed.Length <= 10 ? trimmed : trimmed.Substring(0, 10);
        }

        private void UpdateBgmTrack()
        {
            switch (gameState)
            {
                case GameState.Battle:
                case GameState.EncounterTransition:
                    currentBgmTrack = BgmTrack.Battle;
                    break;
                case GameState.ShopBuy:
                    currentBgmTrack = BgmTrack.Shop;
                    break;
                case GameState.Field:
                    currentBgmTrack = currentFieldMap == FieldMapId.Castle ? BgmTrack.Castle : BgmTrack.Field;
                    break;
                default:
                    currentBgmTrack = BgmTrack.MainMenu;
                    break;
            }
        }

        private void EnqueueSound(SoundEffect soundEffect)
        {
            pendingSoundEffects.Enqueue(soundEffect);
        }
    }
}
