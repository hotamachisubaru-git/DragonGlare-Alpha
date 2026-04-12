using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Domain.Field;

public sealed record FieldTransitionDefinition(
    FieldMapId FromMapId,
    Rectangle TriggerArea,
    FieldMapId ToMapId,
    Point DestinationTile)
{
    public bool IsTriggeredBy(Point tile)
    {
        return TriggerArea.Contains(tile);
    }
}
