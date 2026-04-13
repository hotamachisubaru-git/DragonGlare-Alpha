using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DragonGlareAlpha.Unity
{
    public sealed class FieldTransitionService
    {
        public bool TryGetTransition(FieldMapId mapId, Vector2Int tile, out FieldTransitionDefinition transition)
        {
            transition = GameContent.FieldTransitions
                .FirstOrDefault(definition => definition.FromMapId == mapId && definition.IsTriggeredBy(tile));

            return transition != null;
        }
    }

    public sealed class FieldEventService
    {
        public FieldInteractionResult Interact(PlayerProgress player, FieldEventDefinition fieldEvent, UiLanguage language)
        {
            List<string> pages = fieldEvent
                .GetPages(language)
                .Select(page => page.Replace("{player}", GetPlayerName(player)))
                .ToList();

            if (fieldEvent.ActionType == FieldEventActionType.Recover)
            {
                int recoveredHp = Mathf.Min(fieldEvent.RecoverHp, player.MaxHp - player.CurrentHp);
                int recoveredMp = Mathf.Min(fieldEvent.RecoverMp, player.MaxMp - player.CurrentMp);
                player.CurrentHp += recoveredHp;
                player.CurrentMp += recoveredMp;

                string recoveryPage = language == UiLanguage.Japanese
                    ? "HP+" + recoveredHp + "  MP+" + recoveredMp + "\nからだが かるくなった。"
                    : "HP+" + recoveredHp + "  MP+" + recoveredMp + "\nYou feel refreshed.";

                pages.Add(recoveryPage);
            }

            return new FieldInteractionResult
            {
                Pages = pages
            };
        }

        private static string GetPlayerName(PlayerProgress player)
        {
            if (!string.IsNullOrEmpty(player.Name))
            {
                return player.Name;
            }

            return player.Language == UiLanguage.English ? "adventurer" : "ぼうけんしゃ";
        }
    }

    public sealed class BattleService
    {
        private const int SpellCost = 2;

        public BattleEncounter CreateEncounter(System.Random random, FieldMapId encounterMap, int playerLevel)
        {
            IReadOnlyList<EnemyDefinition> pool = GetEncounterPool(encounterMap, playerLevel);
            EnemyDefinition enemy = SelectEnemyFromPool(random, pool);
            return new BattleEncounter(enemy);
        }

        public IReadOnlyList<EnemyDefinition> GetEncounterPool(FieldMapId encounterMap, int playerLevel)
        {
            EnemyDefinition[] mapPool = GameContent.EnemyCatalog
                .Where(enemy => enemy.EncounterMap == encounterMap)
                .ToArray();

            if (mapPool.Length == 0)
            {
                return GameContent.EnemyCatalog;
            }

            EnemyDefinition[] levelPool = mapPool
                .Where(enemy => playerLevel >= enemy.MinRecommendedLevel && playerLevel <= enemy.MaxRecommendedLevel)
                .ToArray();

            return levelPool.Length > 0 ? levelPool : mapPool;
        }

        public int GetPlayerAttack(PlayerProgress player, WeaponDefinition equippedWeapon)
        {
            return player.BaseAttack + player.Level + (equippedWeapon != null ? equippedWeapon.AttackBonus : 0);
        }

        public int GetPlayerDefense(PlayerProgress player, ArmorDefinition equippedArmor)
        {
            return player.BaseDefense + Mathf.Max(0, player.Level / 2) + (equippedArmor != null ? equippedArmor.DefenseBonus : 0);
        }

        public BattleTurnResolution ResolveTurn(
            PlayerProgress player,
            BattleEncounter encounter,
            BattleActionType action,
            WeaponDefinition equippedWeapon,
            ArmorDefinition equippedArmor,
            ConsumableDefinition selectedConsumable,
            IEquipmentDefinition selectedEquipment,
            System.Random random)
        {
            switch (action)
            {
                case BattleActionType.Attack:
                    return ResolveAttack(player, encounter, equippedWeapon, equippedArmor, random);
                case BattleActionType.Spell:
                    return ResolveSpell(player, encounter, equippedArmor, random);
                case BattleActionType.Defend:
                    return ResolveDefend(player, encounter, equippedArmor, random);
                case BattleActionType.Item:
                    return ResolveItem(player, encounter, selectedConsumable, equippedWeapon, equippedArmor, random);
                case BattleActionType.Equip:
                    return ResolveEquip(player, encounter, selectedEquipment, equippedArmor, random);
                case BattleActionType.Run:
                    return ResolveEscape();
                default:
                    return Reject("こうどうできない。");
            }
        }

        private BattleTurnResolution ResolveAttack(
            PlayerProgress player,
            BattleEncounter encounter,
            WeaponDefinition equippedWeapon,
            ArmorDefinition equippedArmor,
            System.Random random)
        {
            List<BattleSequenceStep> steps = new List<BattleSequenceStep>();
            steps.Add(new BattleSequenceStep { Message = GetPlayerName(player) + "の こうげき！" });

            int damage = Mathf.Max(1, GetPlayerAttack(player, equippedWeapon) + random.Next(2, 6) - encounter.Enemy.Defense);
            encounter.CurrentHp = Mathf.Max(0, encounter.CurrentHp - damage);
            steps.Add(new BattleSequenceStep
            {
                Message = encounter.Enemy.Name + "に " + damage + "ダメージ！",
                VisualCue = BattleVisualCue.EnemyHit,
                AnimationFrames = 10
            });

            if (encounter.CurrentHp == 0)
            {
                steps.Add(new BattleSequenceStep
                {
                    Message = encounter.Enemy.Name + "を たおした！",
                    VisualCue = BattleVisualCue.EnemyDefeat,
                    AnimationFrames = 16
                });

                return new BattleTurnResolution
                {
                    Outcome = BattleOutcome.Victory,
                    Steps = steps
                };
            }

            AppendEnemyCounter(player, encounter, equippedArmor, steps, random, false);
            return BuildResolution(player, steps);
        }

        private BattleTurnResolution ResolveSpell(
            PlayerProgress player,
            BattleEncounter encounter,
            ArmorDefinition equippedArmor,
            System.Random random)
        {
            if (player.CurrentMp < SpellCost)
            {
                return Reject("MPが たりない！");
            }

            player.CurrentMp -= SpellCost;
            int damage = 10 + (player.Level * 2) + random.Next(3, 8);
            encounter.CurrentHp = Mathf.Max(0, encounter.CurrentHp - damage);

            List<BattleSequenceStep> steps = new List<BattleSequenceStep>();
            steps.Add(new BattleSequenceStep
            {
                Message = GetPlayerName(player) + "は メラを となえた！",
                VisualCue = BattleVisualCue.SpellCast,
                AnimationFrames = 12
            });
            steps.Add(new BattleSequenceStep
            {
                Message = encounter.Enemy.Name + "に " + damage + "ダメージ！",
                VisualCue = BattleVisualCue.EnemyHit,
                AnimationFrames = 10
            });

            if (encounter.CurrentHp == 0)
            {
                steps.Add(new BattleSequenceStep
                {
                    Message = encounter.Enemy.Name + "を やきはらった！",
                    VisualCue = BattleVisualCue.EnemyDefeat,
                    AnimationFrames = 16
                });

                return new BattleTurnResolution
                {
                    Outcome = BattleOutcome.Victory,
                    Steps = steps
                };
            }

            AppendEnemyCounter(player, encounter, equippedArmor, steps, random, false);
            return BuildResolution(player, steps);
        }

        private BattleTurnResolution ResolveDefend(
            PlayerProgress player,
            BattleEncounter encounter,
            ArmorDefinition equippedArmor,
            System.Random random)
        {
            List<BattleSequenceStep> steps = new List<BattleSequenceStep>();
            steps.Add(new BattleSequenceStep
            {
                Message = GetPlayerName(player) + "は みをまもっている！"
            });

            AppendEnemyCounter(player, encounter, equippedArmor, steps, random, true);
            return BuildResolution(player, steps);
        }

        private BattleTurnResolution ResolveItem(
            PlayerProgress player,
            BattleEncounter encounter,
            ConsumableDefinition selectedConsumable,
            WeaponDefinition equippedWeapon,
            ArmorDefinition equippedArmor,
            System.Random random)
        {
            if (selectedConsumable == null)
            {
                return Reject("つかえる どうぐがない。");
            }

            if (player.GetItemCount(selectedConsumable.Id) <= 0)
            {
                return Reject("その どうぐは もっていない。");
            }

            List<BattleSequenceStep> steps = new List<BattleSequenceStep>();
            steps.Add(new BattleSequenceStep
            {
                Message = GetPlayerName(player) + "は " + selectedConsumable.Name + "を つかった！",
                VisualCue = BattleVisualCue.ItemUse,
                AnimationFrames = 8
            });

            switch (selectedConsumable.EffectType)
            {
                case ConsumableEffectType.HealHp:
                    if (player.CurrentHp >= player.MaxHp)
                    {
                        return Reject("HPは もう まんたんだ。");
                    }

                    player.RemoveItem(selectedConsumable.Id, 1);
                    int healed = Mathf.Min(selectedConsumable.Amount, player.MaxHp - player.CurrentHp);
                    player.CurrentHp += healed;
                    steps.Add(new BattleSequenceStep
                    {
                        Message = "HPが " + healed + "かいふくした！",
                        VisualCue = BattleVisualCue.PlayerHeal,
                        AnimationFrames = 12
                    });
                    AppendEnemyCounter(player, encounter, equippedArmor, steps, random, false);
                    return BuildResolution(player, steps);

                case ConsumableEffectType.HealMp:
                    if (player.CurrentMp >= player.MaxMp)
                    {
                        return Reject("MPは もう まんたんだ。");
                    }

                    player.RemoveItem(selectedConsumable.Id, 1);
                    int restored = Mathf.Min(selectedConsumable.Amount, player.MaxMp - player.CurrentMp);
                    player.CurrentMp += restored;
                    steps.Add(new BattleSequenceStep
                    {
                        Message = "MPが " + restored + "かいふくした！",
                        VisualCue = BattleVisualCue.MpRecover,
                        AnimationFrames = 12
                    });
                    AppendEnemyCounter(player, encounter, equippedArmor, steps, random, false);
                    return BuildResolution(player, steps);

                case ConsumableEffectType.DamageEnemy:
                    player.RemoveItem(selectedConsumable.Id, 1);
                    int damage = Mathf.Max(1, selectedConsumable.Amount + random.Next(-2, 4) - encounter.Enemy.Defense);
                    encounter.CurrentHp = Mathf.Max(0, encounter.CurrentHp - damage);
                    steps.Add(new BattleSequenceStep
                    {
                        Message = encounter.Enemy.Name + "に " + damage + "ダメージ！",
                        VisualCue = BattleVisualCue.EnemyHit,
                        AnimationFrames = 12
                    });

                    if (encounter.CurrentHp == 0)
                    {
                        steps.Add(new BattleSequenceStep
                        {
                            Message = encounter.Enemy.Name + "を ふきとばした！",
                            VisualCue = BattleVisualCue.EnemyDefeat,
                            AnimationFrames = 16
                        });

                        return new BattleTurnResolution
                        {
                            Outcome = BattleOutcome.Victory,
                            Steps = steps
                        };
                    }

                    AppendEnemyCounter(player, encounter, equippedArmor, steps, random, false);
                    return BuildResolution(player, steps);

                default:
                    return Reject("その どうぐは まだ つかえない。");
            }
        }

        private BattleTurnResolution ResolveEquip(
            PlayerProgress player,
            BattleEncounter encounter,
            IEquipmentDefinition selectedEquipment,
            ArmorDefinition equippedArmor,
            System.Random random)
        {
            if (selectedEquipment == null)
            {
                return Reject("そうびできる ものがない。");
            }

            if (player.GetItemCount(selectedEquipment.Id) <= 0)
            {
                return Reject("その そうびは もっていない。");
            }

            if ((selectedEquipment.Slot == EquipmentSlot.Weapon && string.Equals(player.EquippedWeaponId, selectedEquipment.Id, StringComparison.Ordinal)) ||
                (selectedEquipment.Slot == EquipmentSlot.Armor && string.Equals(player.EquippedArmorId, selectedEquipment.Id, StringComparison.Ordinal)))
            {
                return Reject(selectedEquipment.Name + "は もう そうびしている。");
            }

            ArmorDefinition nextArmor = equippedArmor;
            if (selectedEquipment.Slot == EquipmentSlot.Weapon)
            {
                player.EquippedWeaponId = selectedEquipment.Id;
            }
            else
            {
                player.EquippedArmorId = selectedEquipment.Id;
                nextArmor = selectedEquipment as ArmorDefinition;
            }

            List<BattleSequenceStep> steps = new List<BattleSequenceStep>();
            steps.Add(new BattleSequenceStep
            {
                Message = GetPlayerName(player) + "は " + selectedEquipment.Name + "を そうびした！"
            });

            AppendEnemyCounter(player, encounter, nextArmor, steps, random, false);
            return BuildResolution(player, steps);
        }

        private static BattleTurnResolution ResolveEscape()
        {
            BattleTurnResolution result = new BattleTurnResolution();
            result.Outcome = BattleOutcome.Escaped;
            result.Steps.Add(new BattleSequenceStep
            {
                Message = "うまく にげきった！",
                VisualCue = BattleVisualCue.ItemUse,
                AnimationFrames = 8
            });
            return result;
        }

        private void AppendEnemyCounter(
            PlayerProgress player,
            BattleEncounter encounter,
            ArmorDefinition equippedArmor,
            List<BattleSequenceStep> steps,
            System.Random random,
            bool isDefending)
        {
            steps.Add(new BattleSequenceStep
            {
                Message = encounter.Enemy.Name + "の こうげき！"
            });

            int enemyDamage = Mathf.Max(1, encounter.Enemy.Attack + random.Next(1, 5) - GetPlayerDefense(player, equippedArmor));
            if (isDefending)
            {
                enemyDamage = Mathf.Max(1, Mathf.CeilToInt(enemyDamage / 2f));
            }

            player.CurrentHp = Mathf.Max(0, player.CurrentHp - enemyDamage);
            steps.Add(new BattleSequenceStep
            {
                Message = isDefending
                    ? enemyDamage + "ダメージに おさえた！"
                    : enemyDamage + "ダメージを うけた！",
                VisualCue = BattleVisualCue.PlayerHit,
                AnimationFrames = 10
            });

            if (player.CurrentHp == 0)
            {
                steps.Add(new BattleSequenceStep
                {
                    Message = "めのまえが まっくらになった…"
                });
            }
        }

        private static BattleTurnResolution BuildResolution(PlayerProgress player, List<BattleSequenceStep> steps)
        {
            return new BattleTurnResolution
            {
                Outcome = player.CurrentHp == 0 ? BattleOutcome.Defeat : BattleOutcome.Ongoing,
                Steps = steps
            };
        }

        private static BattleTurnResolution Reject(string message)
        {
            BattleTurnResolution result = new BattleTurnResolution();
            result.Outcome = BattleOutcome.Invalid;
            result.ActionAccepted = false;
            result.Steps.Add(new BattleSequenceStep { Message = message });
            return result;
        }

        private static string GetPlayerName(PlayerProgress player)
        {
            return string.IsNullOrEmpty(player.Name) ? "ぼうけんしゃ" : player.Name;
        }

        private static EnemyDefinition SelectEnemyFromPool(System.Random random, IReadOnlyList<EnemyDefinition> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                throw new InvalidOperationException("Encounter pool must contain at least one enemy.");
            }

            int totalWeight = pool.Sum(enemy => Mathf.Max(1, enemy.EncounterWeight));
            int roll = random.Next(totalWeight);
            for (int index = 0; index < pool.Count; index++)
            {
                roll -= Mathf.Max(1, pool[index].EncounterWeight);
                if (roll < 0)
                {
                    return pool[index];
                }
            }

            return pool[pool.Count - 1];
        }
    }

    public sealed class ProgressionService
    {
        public static readonly int MaxLevelExperience = GetExperienceThreshold(PlayerProgress.MaxLevelValue);

        public PlayerProgress CreateNewPlayer(UiLanguage language, Vector2Int startTile)
        {
            PlayerProgress player = PlayerProgress.CreateDefault(startTile, language);
            GrantPrototypeStarterItems(player);
            return player;
        }

        public string ApplyBattleRewards(PlayerProgress player, EnemyDefinition enemy, System.Random random)
        {
            int previousExperience = player.Experience;
            int previousGold = player.Gold;
            player.Experience = Mathf.Min(MaxLevelExperience, player.Experience + enemy.ExperienceReward);
            player.Gold = Mathf.Min(PlayerProgress.MaxGoldValue, player.Gold + enemy.GoldReward);
            int gainedExperience = player.Experience - previousExperience;
            int gainedGold = player.Gold - previousGold;

            List<string> messages = new List<string>();
            messages.Add(gainedExperience + "けいけんち と " + gainedGold + "Gを えた！");

            string dropMessage;
            if (TryAwardBattleDrop(player, enemy, random, out dropMessage))
            {
                messages.Add(dropMessage);
            }

            while (player.Level < PlayerProgress.MaxLevelValue && player.Experience >= GetExperienceThreshold(player.Level + 1))
            {
                player.Level++;

                int previousMaxHp = player.MaxHp;
                int previousMaxMp = player.MaxMp;
                int hpGain = 4 + random.Next(0, 3);
                int mpGain = 1 + random.Next(0, 2);
                int attackGain = 1 + random.Next(0, 2);
                int defenseGain = 1 + random.Next(0, 2);

                player.MaxHp = Mathf.Min(PlayerProgress.MaxVitalValue, player.MaxHp + hpGain);
                player.MaxMp = Mathf.Min(PlayerProgress.MaxVitalValue, player.MaxMp + mpGain);
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

                messages.Add(GetName(player) + "は レベル" + player.Level + "に あがった！");
                messages.Add("HP+" + hpGain + " MP+" + mpGain + " ATK+" + attackGain + " DEF+" + defenseGain);
            }

            player.Normalize();
            return string.Join("\n", messages.ToArray());
        }

        public string ApplyDefeatPenalty(PlayerProgress player, Vector2Int respawnTile)
        {
            int goldLoss = Mathf.Min(player.Gold, Mathf.Max(0, player.Gold / 5));
            player.Gold -= goldLoss;
            player.TilePosition = respawnTile;
            player.CurrentHp = player.MaxHp;
            player.CurrentMp = player.MaxMp;
            player.Normalize();

            if (goldLoss == 0)
            {
                return "HPとMPを とりもどし\nスタートちてんに もどった。";
            }

            return goldLoss + "Gを おとして\nスタートちてんに もどった。";
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

        private static int GetExperienceThreshold(int level)
        {
            if (level <= 1)
            {
                return 0;
            }

            int cappedLevel = Mathf.Min(level, PlayerProgress.MaxLevelValue);
            int completedLevels = cappedLevel - 1;
            return completedLevels * (24 + ((completedLevels - 1) * 10)) / 2;
        }

        private static string GetName(PlayerProgress player)
        {
            return string.IsNullOrEmpty(player.Name) ? "プレイヤー" : player.Name;
        }

        private static bool TryAwardBattleDrop(PlayerProgress player, EnemyDefinition enemy, System.Random random, out string dropMessage)
        {
            dropMessage = string.Empty;
            if (enemy.Drop == null ||
                string.IsNullOrEmpty(enemy.Drop.ItemId) ||
                enemy.Drop.Quantity <= 0 ||
                enemy.Drop.ChancePercent <= 0)
            {
                return false;
            }

            if (random.Next(100) >= enemy.Drop.ChancePercent)
            {
                return false;
            }

            player.AddItem(enemy.Drop.ItemId, enemy.Drop.Quantity);
            string itemName = enemy.Drop.ItemId;
            ConsumableDefinition consumable = GameContent.GetConsumableById(enemy.Drop.ItemId);
            WeaponDefinition weapon = GameContent.GetWeaponById(enemy.Drop.ItemId);
            ArmorDefinition armor = GameContent.GetArmorById(enemy.Drop.ItemId);

            if (consumable != null)
            {
                itemName = consumable.Name;
            }
            else if (weapon != null)
            {
                itemName = weapon.Name;
            }
            else if (armor != null)
            {
                itemName = armor.Name;
            }

            dropMessage = enemy.Drop.Quantity > 1
                ? enemy.Name + "は " + itemName + " x" + enemy.Drop.Quantity + "を おとした！"
                : enemy.Name + "は " + itemName + "を おとした！";
            return true;
        }
    }

    public sealed class ShopPurchaseResult
    {
        public ShopPurchaseResult(bool success, bool equipped, string message)
        {
            Success = success;
            Equipped = equipped;
            Message = message;
        }

        public bool Success { get; private set; }
        public bool Equipped { get; private set; }
        public string Message { get; private set; }
    }

    public sealed class ShopService
    {
        public ShopPurchaseResult PurchaseEquipment(
            PlayerProgress player,
            IEquipmentDefinition equipment,
            WeaponDefinition currentWeapon,
            ArmorDefinition currentArmor)
        {
            if (player.Gold < equipment.Price)
            {
                return new ShopPurchaseResult(false, false, "＊「おかねが たりないね。」");
            }

            player.Gold -= equipment.Price;
            player.AddItem(equipment.Id, 1);

            bool shouldEquip = false;
            if (equipment.Slot == EquipmentSlot.Weapon)
            {
                shouldEquip = currentWeapon == null || equipment.AttackBonus > currentWeapon.AttackBonus;
                if (shouldEquip)
                {
                    player.EquippedWeaponId = equipment.Id;
                }
            }
            else
            {
                shouldEquip = currentArmor == null || equipment.DefenseBonus > currentArmor.DefenseBonus;
                if (shouldEquip)
                {
                    player.EquippedArmorId = equipment.Id;
                }
            }

            string message = shouldEquip
                ? "＊「" + equipment.Name + "を かった！\n　さっそく そうびしたぜ。」"
                : "＊「" + equipment.Name + "を かった！\n　もちものに いれておくよ。」";

            return new ShopPurchaseResult(true, shouldEquip, message);
        }
    }
}
