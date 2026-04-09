using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Persistence;

public enum SaveSlotState
{
    Empty,
    Occupied,
    Corrupted
}

public sealed class SaveSlotSummary
{
    public int SlotNumber { get; init; }

    public SaveSlotState State { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Level { get; init; }

    public int Gold { get; init; }

    public FieldMapId CurrentFieldMap { get; init; } = FieldMapId.Hub;

    public DateTime? SavedAtLocal { get; init; }
}
