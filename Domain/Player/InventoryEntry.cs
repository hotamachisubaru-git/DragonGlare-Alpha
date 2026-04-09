using DragonGlareAlpha.Security;

namespace DragonGlareAlpha.Domain.Player;

public sealed class InventoryEntry
{
    private readonly ProtectedInt quantity = new();

    public string ItemId { get; set; } = string.Empty;

    public int Quantity
    {
        get => quantity.Value;
        set => quantity.Value = value;
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
