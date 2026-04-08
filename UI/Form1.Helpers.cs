using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class Form1
{
    private bool HasNpcOnCurrentMap()
    {
        return currentFieldMap == FieldMapId.Hub;
    }

    private bool IsNpcTile(Point tile)
    {
        return HasNpcOnCurrentMap() && tile == NpcTile;
    }

    private void SetFieldMap(FieldMapId mapId)
    {
        currentFieldMap = mapId;
        map = MapFactory.CreateMap(mapId);
        UpdateBgm();
    }

    private void ChangeGameState(GameState nextState)
    {
        gameState = nextState;
        UpdateBgm();
    }

    private void SwitchFieldMap(FieldMapId mapId, Point destinationTile, bool persistProgress = true)
    {
        SetFieldMap(mapId);
        player.TilePosition = destinationTile;
        isNpcDialogOpen = false;
        movementCooldown = 6;

        if (persistProgress)
        {
            PersistProgress();
        }
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
}
