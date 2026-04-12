namespace DragonGlareAlpha.Domain;

public enum GameState
{
    ModeSelect,
    LanguageSelection,
    NameInput,
    SaveSlotSelection,
    Field,
    EncounterTransition,
    Battle,
    ShopBuy
}

public enum SaveSlotSelectionMode
{
    Save,
    Load
}

public enum ShopPhase
{
    Welcome,
    BuyList
}

public enum EquipmentSlot
{
    Weapon,
    Armor
}

public enum UiLanguage
{
    Japanese,
    English
}

public enum FieldMapId
{
    Hub,
    Castle,
    Field
}

public enum BgmTrack
{
    MainMenu,
    Field,
    Castle,
    Battle,
    Shop
}

public enum SoundEffect
{
    Dialog,
    Collision
}

public enum BattleFlowState
{
    Intro,
    CommandSelection,
    ItemSelection,
    Resolving,
    Victory,
    Defeat,
    Escaped
}

public enum BattleActionType
{
    Attack,
    Spell,
    Item,
    Run
}

public enum BattleOutcome
{
    Ongoing,
    Victory,
    Defeat,
    Escaped,
    Invalid
}

public enum BattleVisualCue
{
    None,
    EnemyHit,
    PlayerHit,
    SpellCast,
    PlayerHeal,
    MpRecover,
    EnemyDefeat,
    ItemUse
}

public enum ConsumableEffectType
{
    HealHp,
    HealMp,
    DamageEnemy
}

public enum FieldEventActionType
{
    Dialogue,
    Recover
}
