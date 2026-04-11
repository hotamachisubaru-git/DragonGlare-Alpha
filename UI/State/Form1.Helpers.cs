using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private IEnumerable<FieldEventDefinition> GetCurrentFieldEvents()
    {
        return GameContent.FieldEvents.Where(fieldEvent => fieldEvent.MapId == currentFieldMap);
    }

    private bool IsBlockedByFieldEvent(Point tile)
    {
        return GetCurrentFieldEvents().Any(fieldEvent => fieldEvent.BlocksMovement && fieldEvent.TilePosition == tile);
    }

    private FieldEventDefinition? GetInteractableFieldEvent()
    {
        return GetCurrentFieldEvents()
            .FirstOrDefault(fieldEvent =>
                fieldEvent.TilePosition == player.TilePosition ||
                IsAdjacent(player.TilePosition, fieldEvent.TilePosition));
    }

    private void SetFieldMap(FieldMapId mapId)
    {
        currentFieldMap = mapId;
        map = MapFactory.CreateMap(mapId);
        ResetFieldMovementAnimation();
        ResetEncounterCounter();
        UpdateBgm();
    }

    private void ChangeGameState(GameState nextState)
    {
        gameState = nextState;
        UpdateBgm();
    }

    private void OpenSaveSlotSelection(SaveSlotSelectionMode mode)
    {
        saveSlotSelectionMode = mode;
        RefreshSaveSlotSummaries();
        saveSlotCursor = Math.Clamp(activeSaveSlot - 1, 0, SaveService.SlotCount - 1);
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

    private void ShowTransientNotice(string message, int frames = 180)
    {
        menuNotice = message;
        menuNoticeFrames = frames;
    }

    private void SwitchFieldMap(FieldMapId mapId, Point destinationTile, bool persistProgress = true)
    {
        SetFieldMap(mapId);
        player.TilePosition = destinationTile;
        CloseFieldDialog();
        movementCooldown = 6;

        if (persistProgress)
        {
            PersistProgress();
        }
    }

    private void ResetEncounterCounter()
    {
        fieldEncounterStepsRemaining = random.Next(6, 12);
    }

    private void StartFieldMovementAnimation(Point movement)
    {
        fieldMovementAnimationDirection = movement;
        fieldMovementAnimationFramesRemaining = FieldMovementAnimationDuration;
    }

    private void UpdateFieldMovementAnimation()
    {
        if (fieldMovementAnimationFramesRemaining <= 0)
        {
            fieldMovementAnimationDirection = Point.Empty;
            return;
        }

        fieldMovementAnimationFramesRemaining--;
        if (fieldMovementAnimationFramesRemaining == 0)
        {
            fieldMovementAnimationDirection = Point.Empty;
        }
    }

    private void ResetFieldMovementAnimation()
    {
        fieldMovementAnimationDirection = Point.Empty;
        fieldMovementAnimationFramesRemaining = 0;
    }

    private Point GetFieldMovementAnimationOffset()
    {
        if (fieldMovementAnimationFramesRemaining <= 0)
        {
            return Point.Empty;
        }

        var progress = fieldMovementAnimationFramesRemaining / (float)FieldMovementAnimationDuration;
        return new Point(
            (int)Math.Round(fieldMovementAnimationDirection.X * TileSize * progress),
            (int)Math.Round(fieldMovementAnimationDirection.Y * TileSize * progress));
    }

    private int GetFieldViewportWidthTiles()
    {
        return isFieldStatusVisible ? CompactFieldViewportWidthTiles : ExpandedFieldViewportWidthTiles;
    }

    private int GetFieldViewportHeightTiles()
    {
        return isFieldStatusVisible ? CompactFieldViewportHeightTiles : ExpandedFieldViewportHeightTiles;
    }

    private Rectangle GetFieldViewport()
    {
        var widthTiles = GetFieldViewportWidthTiles();
        var heightTiles = GetFieldViewportHeightTiles();
        var width = widthTiles * TileSize;
        var height = heightTiles * TileSize;
        var x = isFieldStatusVisible ? 16 : (UiCanvas.VirtualWidth - width) / 2;
        var y = isFieldStatusVisible ? 112 : 114;

        if (!isFieldStatusVisible)
        {
            y += ExpandedFieldViewportVerticalTrim / 2;
            height -= ExpandedFieldViewportVerticalTrim;
        }

        return new Rectangle(x, y, width, height);
    }

    private Rectangle GetFieldHelpWindow()
    {
        return isFieldStatusVisible
            ? FieldLayout.StatusVisibleHelpWindow
            : FieldLayout.ExpandedHelpWindow;
    }

    private Rectangle GetCenteredFieldTileRectangle(Rectangle viewport)
    {
        return new Rectangle(
            viewport.X + (viewport.Width / 2) - (TileSize / 2),
            viewport.Y + (viewport.Height / 2) - (TileSize / 2),
            TileSize,
            TileSize);
    }

    private int GetTileIdAtWorldPosition(Point tile)
    {
        if (tile.X < 0 || tile.Y < 0 || tile.X >= map.GetLength(1) || tile.Y >= map.GetLength(0))
        {
            return MapFactory.WallTile;
        }

        return map[tile.Y, tile.X];
    }

    private void OpenFieldDialog(FieldEventDefinition fieldEvent)
    {
        var result = fieldEventService.Interact(player, fieldEvent, selectedLanguage);
        activeFieldDialogPages = result.Pages
            .Where(page => !string.IsNullOrWhiteSpace(page))
            .ToArray();
        activeFieldDialogPageIndex = 0;
        isFieldDialogOpen = activeFieldDialogPages.Count > 0;

        if (fieldEvent.ActionType == FieldEventActionType.Recover)
        {
            PersistProgress();
        }

        PlaySe(SoundEffect.Dialog);
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
            PlaySe(SoundEffect.Dialog);
            return;
        }

        CloseFieldDialog();
    }

    private void CloseFieldDialog()
    {
        isFieldDialogOpen = false;
        activeFieldDialogPages = [];
        activeFieldDialogPageIndex = 0;
    }

    private string GetCurrentFieldDialogPage()
    {
        if (!isFieldDialogOpen || activeFieldDialogPages.Count == 0)
        {
            return string.Empty;
        }

        return activeFieldDialogPages[Math.Clamp(activeFieldDialogPageIndex, 0, activeFieldDialogPages.Count - 1)];
    }

    private bool TryTransitionFromTile(Point tile)
    {
        switch (currentFieldMap)
        {
            case FieldMapId.Hub when tile.X >= 9 && tile.X <= 10 && tile.Y <= 1:
                SwitchFieldMap(FieldMapId.Castle, CastleEntryTile);
                return true;
            case FieldMapId.Hub when tile.X >= map.GetLength(1) - 1 && tile.Y >= 7 && tile.Y <= 8:
                SwitchFieldMap(FieldMapId.Field, FieldEntryTile);
                return true;
            case FieldMapId.Castle when tile.X >= 9 && tile.X <= 10 && tile.Y >= map.GetLength(0) - 1:
                SwitchFieldMap(FieldMapId.Hub, HubFromCastleTile);
                return true;
            case FieldMapId.Field when tile.X <= 0 && tile.Y >= 7 && tile.Y <= 8:
                SwitchFieldMap(FieldMapId.Hub, HubFromFieldTile);
                return true;
            default:
                return false;
        }
    }

    private WeaponDefinition? GetEquippedWeapon()
    {
        return GameContent.GetWeaponById(player.EquippedWeaponId);
    }

    private string GetDisplayPlayerName()
    {
        if (!string.IsNullOrWhiteSpace(player.Name))
        {
            return player.Name;
        }

        return playerName.Length == 0 ? "のりたま" : playerName.ToString();
    }

    private string GetEquippedWeaponName()
    {
        return GetEquippedWeapon()?.Name ?? "なし";
    }

    private int GetTotalAttack()
    {
        return battleService.GetPlayerAttack(player, GetEquippedWeapon());
    }

    private int GetTotalDefense()
    {
        return battleService.GetPlayerDefense(player);
    }

    private string GetExperienceSummary()
    {
        if (player.Level >= PlayerProgress.MaxLevelValue)
        {
            return "MAX";
        }

        var current = progressionService.GetExperienceIntoCurrentLevel(player);
        var needed = progressionService.GetExperienceNeededForNextLevel(player);
        return $"{current}/{needed}";
    }

    private string TrimPlayerName(string name)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        return trimmed.Length <= 10 ? trimmed : trimmed[..10];
    }

    private void SyncPlayerNameBuffer(string name)
    {
        playerName.Clear();
        if (!string.IsNullOrWhiteSpace(name))
        {
            playerName.Append(TrimPlayerName(name));
        }
    }

    private static string FormatBattleResolutionMessage(IEnumerable<DragonGlareAlpha.Domain.Battle.BattleSequenceStep> steps)
    {
        return string.Join('\n', steps.Select(step => step.Message).Where(message => !string.IsNullOrWhiteSpace(message)));
    }

    private static string GetMapDisplayName(FieldMapId mapId)
    {
        return mapId switch
        {
            FieldMapId.Castle => "CASTLE",
            FieldMapId.Field => "FIELD",
            _ => "HUB"
        };
    }
}
