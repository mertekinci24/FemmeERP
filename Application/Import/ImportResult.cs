namespace InventoryERP.Application.Import;

public sealed record ImportResult(
    int Added, 
    int Updated, 
    int Skipped, 
    int Total
);
