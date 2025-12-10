namespace InventoryERP.Domain.Wms;

public sealed class Location
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool VisibleInUI { get; set; } = true;
    public int? ParentLocationId { get; set; }
}

