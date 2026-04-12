using System.Diagnostics.CodeAnalysis;
using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Field;

namespace DragonGlareAlpha.Services;

public sealed class FieldTransitionService
{
    public bool TryGetTransition(FieldMapId mapId, Point tile, [NotNullWhen(true)] out FieldTransitionDefinition? transition)
    {
        transition = GameContent.FieldTransitions
            .FirstOrDefault(definition => definition.FromMapId == mapId && definition.IsTriggeredBy(tile));

        return transition is not null;
    }
}
