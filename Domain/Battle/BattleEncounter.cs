using DragonGlareAlpha.Security;

namespace DragonGlareAlpha.Domain.Battle;

public sealed class BattleEncounter
{
    private readonly ProtectedInt currentHp = new();

    public BattleEncounter(EnemyDefinition enemy)
    {
        Enemy = enemy;
        CurrentHp = enemy.MaxHp;
    }

    public EnemyDefinition Enemy { get; }

    public int CurrentHp
    {
        get => currentHp.Value;
        set => currentHp.Value = value;
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
