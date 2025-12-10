using InventoryERP.Domain.Common;

namespace InventoryERP.Domain.Entities;

public class DocumentSequence : EntityBase
{
    public string DocumentType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int CurrentValue { get; set; }
}
