using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha;

public partial class Form1
{
    private void MoveNameCursor(int deltaX, int deltaY)
    {
        var table = GameContent.GetNameTable(selectedLanguage);
        nameCursorRow = Math.Clamp(nameCursorRow + deltaY, 0, table.Length - 1);
        var maxColumn = table[nameCursorRow].Length - 1;
        nameCursorColumn = Math.Clamp(nameCursorColumn + deltaX, 0, maxColumn);
    }

    private void AddSelectedCharacter()
    {
        var table = GameContent.GetNameTable(selectedLanguage);
        var selected = table[nameCursorRow][nameCursorColumn];

        var deleteToken = selectedLanguage == UiLanguage.Japanese ? "けす" : "DEL";
        var endToken = selectedLanguage == UiLanguage.Japanese ? "おわり" : "END";

        if (selected == deleteToken)
        {
            RemoveLastCharacter();
            return;
        }

        if (selected == endToken)
        {
            if (playerName.Length > 0)
            {
                player.Name = TrimPlayerName(playerName.ToString());
                OpenSaveSlotSelection(SaveSlotSelectionMode.Save);
            }

            return;
        }

        if (playerName.Length < 10)
        {
            playerName.Append(selected);
        }
    }

    private void RemoveLastCharacter()
    {
        if (playerName.Length > 0)
        {
            playerName.Remove(playerName.Length - 1, 1);
        }
    }

    private bool IsWalkableTile(Point tile)
    {
        if (tile.X < 0 || tile.Y < 0 || tile.X >= map.GetLength(1) || tile.Y >= map.GetLength(0))
        {
            return false;
        }

        return map[tile.Y, tile.X] != 1;
    }

    private bool TryMovePlayer(Point movement)
    {
        var target = new Point(player.TilePosition.X + movement.X, player.TilePosition.Y + movement.Y);
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

    private static bool IsAdjacent(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
    }

    private bool WasPressed(Keys key) => pressedKeys.Contains(key);

    private bool WasConfirmPressed()
    {
        return WasPressed(Keys.Enter) || WasPressed(Keys.Z) || WasPressed(Keys.X);
    }

    private bool WasShopConfirmPressed()
    {
        return WasPressed(Keys.Enter) || WasPressed(Keys.Z);
    }

    private bool WasShopBackPressed()
    {
        return WasPressed(Keys.Escape) || WasPressed(Keys.X);
    }

    private bool WasFieldInteractPressed()
    {
        return WasPressed(Keys.Enter) || WasPressed(Keys.Z);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!heldKeys.Contains(e.KeyCode))
        {
            pressedKeys.Add(e.KeyCode);
        }

        heldKeys.Add(e.KeyCode);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        heldKeys.Remove(e.KeyCode);
    }
}
