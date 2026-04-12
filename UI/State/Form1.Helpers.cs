using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private IEnumerable<FieldEventDefinition> GetFieldEvents(FieldMapId mapId)
    {
        return GameContent.FieldEvents.Where(fieldEvent => fieldEvent.MapId == mapId);
    }

    private IEnumerable<FieldEventDefinition> GetCurrentFieldEvents()
    {
        return GetFieldEvents(currentFieldMap);
    }

    private bool IsBlockedByFieldEvent(Point tile)
    {
        return IsBlockedByFieldEvent(currentFieldMap, tile);
    }

    private bool IsBlockedByFieldEvent(FieldMapId mapId, Point tile)
    {
        return GetFieldEvents(mapId).Any(fieldEvent => fieldEvent.BlocksMovement && fieldEvent.TilePosition == tile);
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

    private Point GetFieldCameraOrigin()
    {
        var viewportWidthTiles = GetFieldViewportWidthTiles();
        var viewportHeightTiles = GetFieldViewportHeightTiles();
        var maxCameraX = Math.Max(0, map.GetLength(1) - viewportWidthTiles);
        var maxCameraY = Math.Max(0, map.GetLength(0) - viewportHeightTiles);

        return new Point(
            Math.Clamp(player.TilePosition.X - (viewportWidthTiles / 2), 0, maxCameraX),
            Math.Clamp(player.TilePosition.Y - (viewportHeightTiles / 2), 0, maxCameraY));
    }

    private Point GetFieldCameraAnimationOffset(Point cameraOrigin, Point animationOffset)
    {
        var maxCameraX = Math.Max(0, map.GetLength(1) - GetFieldViewportWidthTiles());
        var maxCameraY = Math.Max(0, map.GetLength(0) - GetFieldViewportHeightTiles());

        return new Point(
            cameraOrigin.X > 0 && cameraOrigin.X < maxCameraX ? animationOffset.X : 0,
            cameraOrigin.Y > 0 && cameraOrigin.Y < maxCameraY ? animationOffset.Y : 0);
    }

    private Point GetPlayerAnimationOffset(Point cameraOrigin, Point animationOffset)
    {
        var cameraOffset = GetFieldCameraAnimationOffset(cameraOrigin, animationOffset);
        return new Point(animationOffset.X - cameraOffset.X, animationOffset.Y - cameraOffset.Y);
    }

    private Rectangle GetFieldTileRectangle(Rectangle viewport, Point cameraOrigin, Point tile, Point offset)
    {
        var x = viewport.X + ((tile.X - cameraOrigin.X) * TileSize) + offset.X;
        var y = viewport.Y + ((tile.Y - cameraOrigin.Y) * TileSize) + offset.Y;

        return new Rectangle(x, y, TileSize, TileSize);
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
        if (!fieldTransitionService.TryGetTransition(currentFieldMap, tile, out var transition))
        {
            return false;
        }

        SwitchFieldMap(transition.ToMapId, transition.DestinationTile);
        return true;
    }

    private WeaponDefinition? GetEquippedWeapon()
    {
        return GameContent.GetWeaponById(player.EquippedWeaponId);
    }

    private ArmorDefinition? GetEquippedArmor()
    {
        return GameContent.GetArmorById(player.EquippedArmorId);
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

    private string GetEquippedArmorName()
    {
        return GetEquippedArmor()?.Name ?? "なし";
    }

    private int GetShopPageCount()
    {
        return Math.Max(1, (GameContent.ShopCatalog.Length + ShopItemsPerPage - 1) / ShopItemsPerPage);
    }

    private IReadOnlyList<ShopMenuEntry> GetShopVisibleEntries()
    {
        var pageStartIndex = shopPageIndex * ShopItemsPerPage;
        var entries = GameContent.ShopCatalog
            .Skip(pageStartIndex)
            .Take(ShopItemsPerPage)
            .Select(item => new ShopMenuEntry(ShopMenuEntryType.Item, item.Name, item))
            .ToList();

        if (shopPageIndex > 0)
        {
            entries.Add(new ShopMenuEntry(ShopMenuEntryType.PreviousPage, "まえへ"));
        }

        if (shopPageIndex + 1 < GetShopPageCount())
        {
            entries.Add(new ShopMenuEntry(ShopMenuEntryType.NextPage, "つぎへ"));
        }

        entries.Add(new ShopMenuEntry(ShopMenuEntryType.Quit, "やめる"));
        return entries;
    }

    private void ResetShopListSelection(int pageIndex = 0)
    {
        shopPageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, GetShopPageCount() - 1));
        shopItemCursor = 0;
    }

    private int GetTotalAttack()
    {
        return battleService.GetPlayerAttack(player, GetEquippedWeapon());
    }

    private int GetTotalDefense()
    {
        return battleService.GetPlayerDefense(player, GetEquippedArmor());
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

    private string GetBattleEncounterMessage(string enemyName)
    {
        return selectedLanguage == UiLanguage.English
            ? $"{enemyName} appears!"
            : $"{enemyName}が あらわれた！";
    }

    private string GetBattleCommandPromptMessage()
    {
        var playerName = GetDisplayPlayerName();
        return selectedLanguage == UiLanguage.English
            ? $"What will {playerName} do?"
            : $"{playerName}は どうする？";
    }
}
