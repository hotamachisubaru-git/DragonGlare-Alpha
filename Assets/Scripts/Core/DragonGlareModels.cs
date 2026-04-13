using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DragonGlareAlpha.Unity
{
    public interface IEquipmentDefinition
    {
        string Id { get; }
        string Name { get; }
        int Price { get; }
        EquipmentSlot Slot { get; }
        int AttackBonus { get; }
        int DefenseBonus { get; }
    }

    public sealed class WeaponDefinition : IEquipmentDefinition
    {
        public WeaponDefinition(string id, string name, int price, int attackBonus)
        {
            Id = id;
            Name = name;
            Price = price;
            AttackBonus = attackBonus;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Price { get; private set; }
        public EquipmentSlot Slot { get { return EquipmentSlot.Weapon; } }
        public int AttackBonus { get; private set; }
        public int DefenseBonus { get { return 0; } }
    }

    public sealed class ArmorDefinition : IEquipmentDefinition
    {
        public ArmorDefinition(string id, string name, int price, int defenseBonus)
        {
            Id = id;
            Name = name;
            Price = price;
            DefenseBonus = defenseBonus;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Price { get; private set; }
        public EquipmentSlot Slot { get { return EquipmentSlot.Armor; } }
        public int AttackBonus { get { return 0; } }
        public int DefenseBonus { get; private set; }
    }

    public sealed class ConsumableDefinition
    {
        public ConsumableDefinition(string id, string name, string description, ConsumableEffectType effectType, int amount)
        {
            Id = id;
            Name = name;
            Description = description;
            EffectType = effectType;
            Amount = amount;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ConsumableEffectType EffectType { get; private set; }
        public int Amount { get; private set; }
    }

    public sealed class InventoryEntry
    {
        private readonly ProtectedInt quantity = new ProtectedInt();

        public string ItemId { get; set; }

        public int Quantity
        {
            get { return quantity.Value; }
            set { quantity.Value = value; }
        }

        public InventoryEntry()
        {
            ItemId = string.Empty;
        }

        public InventoryEntry Clone()
        {
            return new InventoryEntry
            {
                ItemId = ItemId,
                Quantity = Quantity
            };
        }

        public void ValidateIntegrity()
        {
            quantity.Validate();
        }

        public void RekeySensitiveValues()
        {
            quantity.Rekey();
        }
    }

    public sealed class PlayerProgress
    {
        public const int MaxLevelValue = 99;
        public const int MaxVitalValue = 999;
        public const int MaxGoldValue = 99999;

        private readonly ProtectedInt level = new ProtectedInt(1);
        private readonly ProtectedInt experience = new ProtectedInt();
        private readonly ProtectedInt maxHp = new ProtectedInt(20);
        private readonly ProtectedInt currentHp = new ProtectedInt(20);
        private readonly ProtectedInt maxMp = new ProtectedInt(2);
        private readonly ProtectedInt currentMp = new ProtectedInt(2);
        private readonly ProtectedInt baseAttack = new ProtectedInt(5);
        private readonly ProtectedInt baseDefense = new ProtectedInt(3);
        private readonly ProtectedInt gold = new ProtectedInt(220);

        public string Name { get; set; }
        public UiLanguage Language { get; set; }
        public Vector2Int TilePosition { get; set; }
        public string EquippedWeaponId { get; set; }
        public string EquippedArmorId { get; set; }
        public List<InventoryEntry> Inventory { get; set; }

        public PlayerProgress()
        {
            Name = string.Empty;
            Language = UiLanguage.Japanese;
            EquippedWeaponId = string.Empty;
            EquippedArmorId = string.Empty;
            Inventory = new List<InventoryEntry>();
        }

        public int Level
        {
            get { return level.Value; }
            set { level.Value = value; }
        }

        public int Experience
        {
            get { return experience.Value; }
            set { experience.Value = value; }
        }

        public int MaxHp
        {
            get { return maxHp.Value; }
            set { maxHp.Value = value; }
        }

        public int CurrentHp
        {
            get { return currentHp.Value; }
            set { currentHp.Value = value; }
        }

        public int MaxMp
        {
            get { return maxMp.Value; }
            set { maxMp.Value = value; }
        }

        public int CurrentMp
        {
            get { return currentMp.Value; }
            set { currentMp.Value = value; }
        }

        public int BaseAttack
        {
            get { return baseAttack.Value; }
            set { baseAttack.Value = value; }
        }

        public int BaseDefense
        {
            get { return baseDefense.Value; }
            set { baseDefense.Value = value; }
        }

        public int Gold
        {
            get { return gold.Value; }
            set { gold.Value = value; }
        }

        public static PlayerProgress CreateDefault(Vector2Int startTile, UiLanguage language)
        {
            PlayerProgress player = new PlayerProgress();
            player.Language = language;
            player.TilePosition = startTile;
            return player;
        }

        public void AddItem(string itemId, int quantityToAdd)
        {
            if (string.IsNullOrEmpty(itemId) || quantityToAdd <= 0)
            {
                return;
            }

            InventoryEntry existing = Inventory.FirstOrDefault(entry => string.Equals(entry.ItemId, itemId, StringComparison.Ordinal));
            if (existing == null)
            {
                Inventory.Add(new InventoryEntry
                {
                    ItemId = itemId,
                    Quantity = quantityToAdd
                });
                return;
            }

            existing.Quantity += quantityToAdd;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return 0;
            }

            return Inventory
                .Where(entry => string.Equals(entry.ItemId, itemId, StringComparison.Ordinal))
                .Sum(entry => entry.Quantity);
        }

        public bool RemoveItem(string itemId, int quantityToRemove)
        {
            if (string.IsNullOrEmpty(itemId) || quantityToRemove <= 0)
            {
                return false;
            }

            InventoryEntry existing = Inventory.FirstOrDefault(entry => string.Equals(entry.ItemId, itemId, StringComparison.Ordinal));
            if (existing == null || existing.Quantity < quantityToRemove)
            {
                return false;
            }

            existing.Quantity -= quantityToRemove;
            if (existing.Quantity == 0)
            {
                Inventory.Remove(existing);
            }

            if (string.Equals(EquippedWeaponId, itemId, StringComparison.Ordinal) && GetItemCount(itemId) == 0)
            {
                EquippedWeaponId = string.Empty;
            }

            if (string.Equals(EquippedArmorId, itemId, StringComparison.Ordinal) && GetItemCount(itemId) == 0)
            {
                EquippedArmorId = string.Empty;
            }

            return true;
        }

        public void Normalize()
        {
            Level = Mathf.Clamp(Level, 1, MaxLevelValue);
            Experience = Mathf.Max(0, Experience);
            MaxHp = MaxHp <= 0 ? 20 : Mathf.Min(MaxHp, MaxVitalValue);
            CurrentHp = CurrentHp <= 0 ? MaxHp : Mathf.Min(CurrentHp, MaxHp);
            MaxMp = MaxMp <= 0 ? 2 : Mathf.Min(MaxMp, MaxVitalValue);
            CurrentMp = Mathf.Clamp(CurrentMp, 0, MaxMp);
            BaseAttack = BaseAttack <= 0 ? 5 : BaseAttack;
            BaseDefense = BaseDefense <= 0 ? 3 : BaseDefense;
            Gold = Mathf.Clamp(Gold, 0, MaxGoldValue);

            if (Level == MaxLevelValue)
            {
                MaxHp = MaxVitalValue;
                MaxMp = MaxVitalValue;
                CurrentHp = Mathf.Min(CurrentHp, MaxHp);
                CurrentMp = Mathf.Min(CurrentMp, MaxMp);
            }

            Inventory = Inventory
                .Where(entry => !string.IsNullOrEmpty(entry.ItemId) && entry.Quantity > 0)
                .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
                .Select(group => new InventoryEntry
                {
                    ItemId = group.Key,
                    Quantity = group.Sum(entry => entry.Quantity)
                })
                .ToList();

            if (!string.IsNullOrEmpty(EquippedWeaponId) && GetItemCount(EquippedWeaponId) == 0)
            {
                AddItem(EquippedWeaponId, 1);
            }

            if (!string.IsNullOrEmpty(EquippedArmorId) && GetItemCount(EquippedArmorId) == 0)
            {
                AddItem(EquippedArmorId, 1);
            }
        }

        public void ValidateIntegrity()
        {
            level.Validate();
            experience.Validate();
            maxHp.Validate();
            currentHp.Validate();
            maxMp.Validate();
            currentMp.Validate();
            baseAttack.Validate();
            baseDefense.Validate();
            gold.Validate();

            for (int index = 0; index < Inventory.Count; index++)
            {
                Inventory[index].ValidateIntegrity();
            }
        }

        public void RekeySensitiveValues()
        {
            level.Rekey();
            experience.Rekey();
            maxHp.Rekey();
            currentHp.Rekey();
            maxMp.Rekey();
            currentMp.Rekey();
            baseAttack.Rekey();
            baseDefense.Rekey();
            gold.Rekey();

            for (int index = 0; index < Inventory.Count; index++)
            {
                Inventory[index].RekeySensitiveValues();
            }
        }
    }

    public sealed class EnemyDropDefinition
    {
        public EnemyDropDefinition(string itemId, int chancePercent, int quantity)
        {
            ItemId = itemId;
            ChancePercent = chancePercent;
            Quantity = quantity;
        }

        public string ItemId { get; private set; }
        public int ChancePercent { get; private set; }
        public int Quantity { get; private set; }
    }

    public sealed class EnemyDefinition
    {
        public EnemyDefinition(
            string id,
            string name,
            FieldMapId encounterMap,
            int minRecommendedLevel,
            int maxRecommendedLevel,
            int encounterWeight,
            int maxHp,
            int attack,
            int defense,
            int experienceReward,
            int goldReward,
            EnemyDropDefinition drop)
        {
            Id = id;
            Name = name;
            EncounterMap = encounterMap;
            MinRecommendedLevel = minRecommendedLevel;
            MaxRecommendedLevel = maxRecommendedLevel;
            EncounterWeight = encounterWeight;
            MaxHp = maxHp;
            Attack = attack;
            Defense = defense;
            ExperienceReward = experienceReward;
            GoldReward = goldReward;
            Drop = drop;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public FieldMapId EncounterMap { get; private set; }
        public int MinRecommendedLevel { get; private set; }
        public int MaxRecommendedLevel { get; private set; }
        public int EncounterWeight { get; private set; }
        public int MaxHp { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }
        public int ExperienceReward { get; private set; }
        public int GoldReward { get; private set; }
        public EnemyDropDefinition Drop { get; private set; }
    }

    public sealed class BattleEncounter
    {
        private readonly ProtectedInt currentHp = new ProtectedInt();

        public BattleEncounter(EnemyDefinition enemy)
        {
            Enemy = enemy;
            CurrentHp = enemy.MaxHp;
        }

        public EnemyDefinition Enemy { get; private set; }

        public int CurrentHp
        {
            get { return currentHp.Value; }
            set { currentHp.Value = value; }
        }

        public void ValidateIntegrity()
        {
            currentHp.Validate();
        }

        public void RekeySensitiveValues()
        {
            currentHp.Rekey();
        }
    }

    public sealed class BattleSequenceStep
    {
        public string Message { get; set; }
        public BattleVisualCue VisualCue { get; set; }
        public int AnimationFrames { get; set; }

        public BattleSequenceStep()
        {
            Message = string.Empty;
            AnimationFrames = 12;
        }
    }

    public sealed class BattleTurnResolution
    {
        public BattleOutcome Outcome { get; set; }
        public bool ActionAccepted { get; set; }
        public List<BattleSequenceStep> Steps { get; set; }

        public BattleTurnResolution()
        {
            ActionAccepted = true;
            Steps = new List<BattleSequenceStep>();
        }
    }

    public sealed class FieldTransitionDefinition
    {
        public FieldTransitionDefinition(FieldMapId fromMapId, RectInt triggerArea, FieldMapId toMapId, Vector2Int destinationTile)
        {
            FromMapId = fromMapId;
            TriggerArea = triggerArea;
            ToMapId = toMapId;
            DestinationTile = destinationTile;
        }

        public FieldMapId FromMapId { get; private set; }
        public RectInt TriggerArea { get; private set; }
        public FieldMapId ToMapId { get; private set; }
        public Vector2Int DestinationTile { get; private set; }

        public bool IsTriggeredBy(Vector2Int tile)
        {
            return TriggerArea.Contains(tile);
        }
    }

    public sealed class FieldEventDefinition
    {
        public FieldEventDefinition(
            string id,
            FieldMapId mapId,
            Vector2Int tilePosition,
            Color32 displayColor,
            bool blocksMovement,
            FieldEventActionType actionType,
            string[] japanesePages,
            string[] englishPages,
            string spriteAssetName,
            string portraitAssetName,
            int recoverHp,
            int recoverMp)
        {
            Id = id;
            MapId = mapId;
            TilePosition = tilePosition;
            DisplayColor = displayColor;
            BlocksMovement = blocksMovement;
            ActionType = actionType;
            JapanesePages = japanesePages;
            EnglishPages = englishPages;
            SpriteAssetName = spriteAssetName;
            PortraitAssetName = portraitAssetName;
            RecoverHp = recoverHp;
            RecoverMp = recoverMp;
        }

        public string Id { get; private set; }
        public FieldMapId MapId { get; private set; }
        public Vector2Int TilePosition { get; private set; }
        public Color32 DisplayColor { get; private set; }
        public bool BlocksMovement { get; private set; }
        public FieldEventActionType ActionType { get; private set; }
        public string[] JapanesePages { get; private set; }
        public string[] EnglishPages { get; private set; }
        public string SpriteAssetName { get; private set; }
        public string PortraitAssetName { get; private set; }
        public int RecoverHp { get; private set; }
        public int RecoverMp { get; private set; }

        public IList<string> GetPages(UiLanguage language)
        {
            return language == UiLanguage.Japanese ? JapanesePages : EnglishPages;
        }
    }

    public sealed class FieldInteractionResult
    {
        public List<string> Pages { get; set; }

        public FieldInteractionResult()
        {
            Pages = new List<string>();
        }
    }
}
