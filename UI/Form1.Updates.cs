using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void UpdateGame()
    {
        frameCounter++;

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
            case GameState.Field:
                UpdateField();
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

        if (TryLoadGame())
        {
            ChangeGameState(GameState.Field);
            return;
        }

        menuNotice = "NO SAVE DATA / セーブデータがありません";
        menuNoticeFrames = 180;
        PlaySe(SoundEffect.Collision);
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

    private void UpdateField()
    {
        if (isNpcDialogOpen)
        {
            if (WasPressed(Keys.Enter) || WasPressed(Keys.Escape))
            {
                isNpcDialogOpen = false;
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
            var moved = TryMovePlayer(movement);
            if (!moved)
            {
                PlaySe(SoundEffect.Collision);
            }

            movementCooldown = 6;
        }

        if (HasNpcOnCurrentMap() && WasPressed(Keys.Enter) && IsAdjacent(player.TilePosition, NpcTile))
        {
            isNpcDialogOpen = true;
            PlaySe(SoundEffect.Dialog);
        }
    }

    private void UpdateBattle()
    {
        if (currentEncounter is null)
        {
            battleFlowState = BattleFlowState.CommandSelection;
            ChangeGameState(GameState.Field);
            return;
        }

        if (battleFlowState != BattleFlowState.CommandSelection)
        {
            if (WasPressed(Keys.Enter) || WasPressed(Keys.Escape))
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
            battleMessage = "うまく にげきった！";
            battleFlowState = BattleFlowState.Escaped;
            PersistProgress();
            return;
        }

        if (!WasPressed(Keys.Enter))
        {
            return;
        }

        var action = GameContent.BattleCommandGrid[battleCursorRow, battleCursorColumn];
        var result = battleService.ResolveTurn(player, currentEncounter, action, GetEquippedWeapon(), null, random);
        var resultMessage = FormatBattleResolutionMessage(result.Steps);

        switch (result.Outcome)
        {
            case BattleOutcome.Victory:
                battleMessage = $"{resultMessage}\n{progressionService.ApplyBattleRewards(player, currentEncounter.Enemy.ExperienceReward, currentEncounter.Enemy.GoldReward, random)}";
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

    private void UpdateShopBuy()
    {
        if (shopPhase == ShopPhase.Welcome)
        {
            if (WasPressed(Keys.Up) || WasPressed(Keys.W) || WasPressed(Keys.Down) || WasPressed(Keys.S))
            {
                shopPromptCursor = 1 - shopPromptCursor;
            }

            if (WasPressed(Keys.Escape))
            {
                ChangeGameState(GameState.Field);
                return;
            }

            if (!WasPressed(Keys.Enter))
            {
                return;
            }

            if (shopPromptCursor == 0)
            {
                shopPhase = ShopPhase.BuyList;
                shopItemCursor = 0;
                shopMessage = "＊「なにを かっていくかい？」";
                return;
            }

            ChangeGameState(GameState.Field);
            return;
        }

        var maxIndex = GameContent.ShopCatalog.Length;
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            shopItemCursor = Math.Max(0, shopItemCursor - 1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            shopItemCursor = Math.Min(maxIndex, shopItemCursor + 1);
        }

        if (WasPressed(Keys.Escape))
        {
            shopPhase = ShopPhase.Welcome;
            shopMessage = "＊「ほかに ようじは あるかい？」";
            return;
        }

        if (!WasPressed(Keys.Enter))
        {
            return;
        }

        if (shopItemCursor == maxIndex)
        {
            shopPhase = ShopPhase.Welcome;
            shopMessage = "＊「また きてくれよな！」";
            return;
        }

        var item = GameContent.ShopCatalog[shopItemCursor];
        var result = shopService.PurchaseWeapon(player, item, GetEquippedWeapon());
        shopMessage = result.Message;
        if (result.Success)
        {
            PersistProgress();
        }
    }

    private void EnterBattle()
    {
        battleCursorRow = 0;
        battleCursorColumn = 0;
        battleFlowState = BattleFlowState.CommandSelection;
        currentEncounter = battleService.CreateEncounter(random);
        battleMessage = $"{currentEncounter.Enemy.Name}が あらわれた！";
        ChangeGameState(GameState.Battle);
        PlaySe(SoundEffect.Dialog);
    }

    private void EnterShopBuy()
    {
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        shopItemCursor = 0;
        shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
        ChangeGameState(GameState.ShopBuy);
        PlaySe(SoundEffect.Dialog);
    }

    private void FinishBattle()
    {
        currentEncounter = null;
        battleFlowState = BattleFlowState.CommandSelection;
        battleCursorRow = 0;
        battleCursorColumn = 0;
        ChangeGameState(GameState.Field);
        PersistProgress();
    }
}
