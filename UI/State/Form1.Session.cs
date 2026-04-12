using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;

namespace DragonGlareAlpha;

public partial class Form1
{
    private const string DefaultBattleMessage = "まものが あらわれた！";
    private const string BattleEscapeMessage = "うまく にげきった！";
    private const string ShopWelcomeMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
    private const string ShopBrowseMessage = "＊「なにを かっていくかい？」";
    private const string ShopReturnMessage = "＊「ほかに ようじは あるかい？」";
    private const string ShopFarewellMessage = "＊「また きてくれよな！」";

    private void ApplyExplorationSession(PlayerProgress nextPlayer, FieldMapId mapId)
    {
        player = nextPlayer;
        SetFieldMap(mapId);
        SyncPlayerNameBuffer(player.Name);
        ResetFieldUiState();
        ResetBattleState();
        ResetShopState();
    }

    private void ResetFieldUiState()
    {
        CloseFieldDialog();
        isFieldStatusVisible = false;
        movementCooldown = 0;
        playerFacingDirection = PlayerFacingDirection.Down;
    }

    private void ResetBattleSelectionState()
    {
        battleFlowState = BattleFlowState.CommandSelection;
        battleCursorRow = 0;
        battleCursorColumn = 0;
        ResetBattleVisualEffects();
    }

    private void ResetBattleState(string? message = null)
    {
        currentEncounter = null;
        pendingEncounter = null;
        encounterTransitionFrames = 0;
        ResetBattleSelectionState();
        battleMessage = message ?? DefaultBattleMessage;
    }

    private void ResetShopState(string? message = null)
    {
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        ResetShopListSelection();
        shopMessage = message ?? ShopWelcomeMessage;
    }

    private void OpenShopCatalog()
    {
        shopPhase = ShopPhase.BuyList;
        ResetShopListSelection();
        shopMessage = ShopBrowseMessage;
    }

    private void ReturnToShopPrompt(string message)
    {
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        ResetShopListSelection();
        shopMessage = message;
    }

    private void ChangeShopPage(int pageDelta)
    {
        ResetShopListSelection(shopPageIndex + pageDelta);
        shopMessage = ShopBrowseMessage;
    }
}
