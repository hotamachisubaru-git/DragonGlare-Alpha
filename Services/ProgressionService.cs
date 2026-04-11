using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Player;

namespace DragonGlareAlpha.Services;

public sealed class ProgressionService
{
    public static readonly int MaxLevelExperience = GetExperienceThreshold(PlayerProgress.MaxLevelValue);

    public PlayerProgress CreateNewPlayer(UiLanguage language, Point startTile)
    {
        var player = PlayerProgress.CreateDefault(startTile, language);
        GrantPrototypeStarterItems(player);
        return player;
    }

    public string ApplyBattleRewards(PlayerProgress player, int experienceReward, int goldReward, Random random)
    {
        var previousExperience = player.Experience;
        var previousGold = player.Gold;
        player.Experience = Math.Min(MaxLevelExperience, player.Experience + experienceReward);
        player.Gold = Math.Min(PlayerProgress.MaxGoldValue, player.Gold + goldReward);
        var gainedExperience = player.Experience - previousExperience;
        var gainedGold = player.Gold - previousGold;

        var messages = new List<string>
        {
            $"{gainedExperience}けいけんち と {gainedGold}Gを えた！"
        };

        while (player.Level < PlayerProgress.MaxLevelValue && player.Experience >= GetExperienceThreshold(player.Level + 1))
        {
            player.Level++;

            var previousMaxHp = player.MaxHp;
            var previousMaxMp = player.MaxMp;
            var hpGain = 4 + random.Next(0, 3);
            var mpGain = 1 + random.Next(0, 2);
            var attackGain = 1 + random.Next(0, 2);
            var defenseGain = 1 + random.Next(0, 2);

            player.MaxHp = Math.Min(PlayerProgress.MaxVitalValue, player.MaxHp + hpGain);
            player.MaxMp = Math.Min(PlayerProgress.MaxVitalValue, player.MaxMp + mpGain);
            if (player.Level == PlayerProgress.MaxLevelValue)
            {
                player.MaxHp = PlayerProgress.MaxVitalValue;
                player.MaxMp = PlayerProgress.MaxVitalValue;
            }

            hpGain = player.MaxHp - previousMaxHp;
            mpGain = player.MaxMp - previousMaxMp;
            player.BaseAttack += attackGain;
            player.BaseDefense += defenseGain;
            player.CurrentHp = player.MaxHp;
            player.CurrentMp = player.MaxMp;

            messages.Add($"{GetName(player)}は レベル{player.Level}に あがった！");
            messages.Add($"HP+{hpGain} MP+{mpGain} ATK+{attackGain} DEF+{defenseGain}");
        }

        player.Normalize();
        return string.Join("\n", messages);
    }

    public string ApplyDefeatPenalty(PlayerProgress player, Point respawnTile)
    {
        var goldLoss = Math.Min(player.Gold, Math.Max(0, player.Gold / 5));
        player.Gold -= goldLoss;
        player.TilePosition = respawnTile;
        player.CurrentHp = player.MaxHp;
        player.CurrentMp = player.MaxMp;
        player.Normalize();

        if (goldLoss == 0)
        {
            return "HPとMPを とりもどし\nスタートちてんに もどった。";
        }

        return $"{goldLoss}Gを おとして\nスタートちてんに もどった。";
    }

    public int GetExperienceIntoCurrentLevel(PlayerProgress player)
    {
        if (player.Level >= PlayerProgress.MaxLevelValue)
        {
            return 0;
        }

        return player.Experience - GetExperienceThreshold(player.Level);
    }

    public int GetExperienceNeededForNextLevel(PlayerProgress player)
    {
        if (player.Level >= PlayerProgress.MaxLevelValue)
        {
            return 0;
        }

        return GetExperienceThreshold(player.Level + 1) - GetExperienceThreshold(player.Level);
    }

    private static int GetExperienceThreshold(int level)
    {
        if (level <= 1)
        {
            return 0;
        }

        var cappedLevel = Math.Min(level, PlayerProgress.MaxLevelValue);
        var completedLevels = cappedLevel - 1;
        return completedLevels * (24 + ((completedLevels - 1) * 10)) / 2;
    }

    private static string GetName(PlayerProgress player)
    {
        return string.IsNullOrWhiteSpace(player.Name) ? "プレイヤー" : player.Name;
    }

    public void GrantPrototypeStarterItems(PlayerProgress player)
    {
        if (player.GetItemCount("healing_herb") == 0)
        {
            player.AddItem("healing_herb", 2);
        }

        if (player.GetItemCount("mana_seed") == 0)
        {
            player.AddItem("mana_seed", 1);
        }

        if (player.GetItemCount("fire_orb") == 0)
        {
            player.AddItem("fire_orb", 1);
        }
    }
}
