using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Security;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void UpdateGame()
    {
        frameCounter++;
        UpdateFieldMovementAnimation();
        UpdateBattleVisualEffects();
        RunAntiCheatChecks();

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
                UpdateModeSelect();
                break;
            case GameState.LanguageSelection:
                UpdateLanguageSelection();
                break;
            case GameState.NameInput:
                UpdateNameInput();
                break;
            case GameState.SaveSlotSelection:
                UpdateSaveSlotSelection();
                break;
            case GameState.Field:
                UpdateField();
                break;
            case GameState.EncounterTransition:
                UpdateEncounterTransition();
                break;
            case GameState.Battle:
                UpdateBattle();
                break;
            case GameState.ShopBuy:
                UpdateShopBuy();
                break;
        }

        UpdateBgm();
    }

    private void RunAntiCheatChecks()
    {
        if (frameCounter % 30 == 0)
        {
            player.RekeySensitiveValues();
            currentEncounter?.RekeySensitiveValues();
            pendingEncounter?.RekeySensitiveValues();
        }

        if (frameCounter % 120 != 0)
        {
            return;
        }

        player.ValidateIntegrity();
        currentEncounter?.ValidateIntegrity();
        pendingEncounter?.ValidateIntegrity();

        if (antiCheatService.TryDetectViolation(out var message))
        {
            throw new TamperDetectedException(message);
        }
    }

    private void UpdateModeSelect()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            modeCursor = 0;
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            modeCursor = 1;
        }

        if (!WasPressed(Keys.Enter))
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

    private void UpdateLanguageSelection()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            languageCursor = 0;
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            languageCursor = 1;
        }

        if (WasPressed(Keys.Enter))
        {
            selectedLanguage = languageCursor == 0 ? UiLanguage.Japanese : UiLanguage.English;
            player.Language = selectedLanguage;
            ChangeGameState(GameState.NameInput);
            playerName.Clear();
            nameCursorRow = 0;
            nameCursorColumn = 0;
        }

        if (WasPressed(Keys.Escape))
        {
            ChangeGameState(GameState.ModeSelect);
        }
    }

    private void UpdateNameInput()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            MoveNameCursor(0, -1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            MoveNameCursor(0, 1);
        }
        else if (WasPressed(Keys.Left) || WasPressed(Keys.A))
        {
            MoveNameCursor(-1, 0);
        }
        else if (WasPressed(Keys.Right) || WasPressed(Keys.D))
        {
            MoveNameCursor(1, 0);
        }

        if (WasPressed(Keys.Back))
        {
            RemoveLastCharacter();
        }

        if (WasPressed(Keys.Escape))
        {
            ChangeGameState(GameState.LanguageSelection);
            return;
        }

        if (WasPressed(Keys.Enter))
        {
            AddSelectedCharacter();
        }
    }

    private void UpdateSaveSlotSelection()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            saveSlotCursor = Math.Max(0, saveSlotCursor - 1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            saveSlotCursor = Math.Min(SaveService.SlotCount - 1, saveSlotCursor + 1);
        }

        if (WasPressed(Keys.Escape))
        {
            ChangeGameState(saveSlotSelectionMode == SaveSlotSelectionMode.Save
                ? GameState.NameInput
                : GameState.ModeSelect);
            return;
        }

        if (!WasPressed(Keys.Enter))
        {
            return;
        }

        var selectedSlot = saveSlotCursor + 1;
        if (saveSlotSelectionMode == SaveSlotSelectionMode.Load)
        {
            if (TryLoadGame(selectedSlot))
            {
                ChangeGameState(GameState.Field);
                return;
            }

            var failureReason = saveService.LastFailureReason;
            RefreshSaveSlotSummaries();
            ShowTransientNotice(failureReason switch
            {
                SaveLoadFailureReason.InvalidSignature => "SAVE DATA INVALID / セーブデータが改ざんされています",
                SaveLoadFailureReason.InvalidFormat => "SAVE DATA ERROR / セーブデータが壊れています",
                _ => "NO SAVE DATA / セーブデータがありません"
            });
            PlaySe(SoundEffect.Collision);
            return;
        }

        activeSaveSlot = selectedSlot;
        SaveGame();
        ChangeGameState(GameState.Field);
    }

    private void UpdateField()
    {
        if (isFieldDialogOpen)
        {
            if (WasFieldInteractPressed())
            {
                AdvanceFieldDialog();
            }
            else if (WasPressed(Keys.Escape))
            {
                CloseFieldDialog();
            }

            return;
        }

        if (WasPressed(Keys.B))
        {
            EnterBattle();
            return;
        }

        if (WasPressed(Keys.V))
        {
            EnterShopBuy();
            return;
        }

        if (WasPressed(Keys.X))
        {
            isFieldStatusVisible = !isFieldStatusVisible;
            return;
        }

        if (movementCooldown > 0)
        {
            movementCooldown--;
        }

        var movement = Point.Empty;
        if (heldKeys.Contains(Keys.Up) || heldKeys.Contains(Keys.W))
        {
            movement = new Point(0, -1);
        }
        else if (heldKeys.Contains(Keys.Down) || heldKeys.Contains(Keys.S))
        {
            movement = new Point(0, 1);
        }
        else if (heldKeys.Contains(Keys.Left) || heldKeys.Contains(Keys.A))
        {
            movement = new Point(-1, 0);
        }
        else if (heldKeys.Contains(Keys.Right) || heldKeys.Contains(Keys.D))
        {
            movement = new Point(1, 0);
        }

        if (movement != Point.Empty && movementCooldown == 0)
        {
            SetPlayerFacingDirection(movement);
            var moved = TryMovePlayer(movement);
            if (!moved)
            {
                PlaySe(SoundEffect.Collision);
            }

            movementCooldown = 6;
            if (gameState != GameState.Field)
            {
                return;
            }
        }

        if (WasFieldInteractPressed())
        {
            var fieldEvent = GetInteractableFieldEvent();
            if (fieldEvent is not null)
            {
                OpenFieldDialog(fieldEvent);
            }
        }
    }

    private void UpdateBattle()
    {
        if (currentEncounter is null)
        {
            ResetBattleState();
            ChangeGameState(GameState.Field);
            return;
        }

        if (battleFlowState == BattleFlowState.Intro)
        {
            if (WasConfirmPressed())
            {
                battleFlowState = BattleFlowState.CommandSelection;
                battleMessage = GetBattleCommandPromptMessage();
            }

            return;
        }

        if (battleFlowState != BattleFlowState.CommandSelection)
        {
            if (WasConfirmPressed() || WasPressed(Keys.Escape))
            {
                FinishBattle();
            }

            return;
        }

        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            battleCursorRow = Math.Max(0, battleCursorRow - 1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            battleCursorRow = Math.Min(1, battleCursorRow + 1);
        }
        else if (WasPressed(Keys.Left) || WasPressed(Keys.A))
        {
            battleCursorColumn = Math.Max(0, battleCursorColumn - 1);
        }
        else if (WasPressed(Keys.Right) || WasPressed(Keys.D))
        {
            battleCursorColumn = Math.Min(1, battleCursorColumn + 1);
        }

        if (WasPressed(Keys.Escape))
        {
            battleMessage = BattleEscapeMessage;
            battleFlowState = BattleFlowState.Escaped;
            PersistProgress();
            return;
        }

        if (!WasConfirmPressed())
        {
            return;
        }

        var action = GameContent.BattleCommandGrid[battleCursorRow, battleCursorColumn];
        var result = battleService.ResolveTurn(player, currentEncounter, action, GetEquippedWeapon(), GetEquippedArmor(), null, random);
        ApplyBattleVisualEffects(result);
        var resultMessage = FormatBattleResolutionMessage(result.Steps);

        switch (result.Outcome)
        {
            case BattleOutcome.Victory:
                battleMessage = $"{resultMessage}\n{progressionService.ApplyBattleRewards(player, currentEncounter.Enemy, random)}";
                battleFlowState = BattleFlowState.Victory;
                PersistProgress();
                break;
            case BattleOutcome.Defeat:
                battleMessage = $"{resultMessage}\n{progressionService.ApplyDefeatPenalty(player, PlayerStartTile)}";
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
                PersistProgress();
                break;
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

        if (pendingEncounter is null)
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

    private void UpdateShopBuy()
    {
        if (shopPhase == ShopPhase.Welcome)
        {
            if (WasPressed(Keys.Up) || WasPressed(Keys.W) || WasPressed(Keys.Down) || WasPressed(Keys.S))
            {
                shopPromptCursor = 1 - shopPromptCursor;
            }

            if (WasShopBackPressed())
            {
                ChangeGameState(GameState.Field);
                return;
            }

            if (!WasShopConfirmPressed())
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

        var visibleEntries = GetShopVisibleEntries();
        var maxIndex = visibleEntries.Count - 1;
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            shopItemCursor = Math.Max(0, shopItemCursor - 1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            shopItemCursor = Math.Min(maxIndex, shopItemCursor + 1);
        }

        if (WasShopBackPressed())
        {
            ReturnToShopPrompt(ShopReturnMessage);
            return;
        }

        if (!WasShopConfirmPressed())
        {
            return;
        }

        var selectedEntry = visibleEntries[shopItemCursor];
        if (selectedEntry.Type == ShopMenuEntryType.PreviousPage)
        {
            ChangeShopPage(-1);
            return;
        }

        if (selectedEntry.Type == ShopMenuEntryType.NextPage)
        {
            ChangeShopPage(1);
            return;
        }

        if (selectedEntry.Type == ShopMenuEntryType.Quit)
        {
            ReturnToShopPrompt(ShopFarewellMessage);
            return;
        }

        var result = shopService.PurchaseEquipment(player, selectedEntry.Item!, GetEquippedWeapon(), GetEquippedArmor());
        shopMessage = result.Message;
        if (result.Success)
        {
            PersistProgress();
        }
    }

    private void EnterBattle()
    {
        StartEncounterTransition(battleService.CreateEncounter(random, currentFieldMap, player.Level));
    }

    private void EnterShopBuy()
    {
        ResetShopState();
        ChangeGameState(GameState.ShopBuy);
        PlaySe(SoundEffect.Dialog);
    }

    private void FinishBattle()
    {
        ResetEncounterCounter();
        ResetBattleState();
        ChangeGameState(GameState.Field);
        PersistProgress();
    }

    private bool TryTriggerRandomEncounter()
    {
        if (currentFieldMap != FieldMapId.Field)
        {
            return false;
        }

        var tileId = map[player.TilePosition.Y, player.TilePosition.X];
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

    private void StartEncounterTransition(BattleEncounter encounter)
    {
        pendingEncounter = encounter;
        encounterTransitionFrames = EncounterTransitionDuration;
        ResetBattleSelectionState();
        ResetEncounterCounter();
        ChangeGameState(GameState.EncounterTransition);
        PlaySe(SoundEffect.Dialog);
    }

    private void UpdateBattleVisualEffects()
    {
        if (enemyHitFlashFramesRemaining > 0)
        {
            enemyHitFlashFramesRemaining--;
        }
    }

    private void ResetBattleVisualEffects()
    {
        enemyHitFlashFramesRemaining = 0;
    }

    private void ApplyBattleVisualEffects(BattleTurnResolution result)
    {
        enemyHitFlashFramesRemaining = 0;
        if (currentEncounter is null || currentEncounter.CurrentHp <= 0)
        {
            return;
        }

        foreach (var step in result.Steps)
        {
            if (step.VisualCue == BattleVisualCue.EnemyHit)
            {
                enemyHitFlashFramesRemaining = Math.Max(enemyHitFlashFramesRemaining, step.AnimationFrames);
            }
        }
    }
}
