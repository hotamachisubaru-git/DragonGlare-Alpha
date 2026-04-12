using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Domain.Battle;

public sealed record EnemyDefinition(
    string Id,
    string Name,
    FieldMapId EncounterMap,
    int MinRecommendedLevel,
    int MaxRecommendedLevel,
    int EncounterWeight,
    int MaxHp,
    int Attack,
    int Defense,
    int ExperienceReward,
    int GoldReward,
    EnemyDropDefinition? Drop = null);
