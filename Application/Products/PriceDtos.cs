using System;

namespace InventoryERP.Application.Products;

/// <summary>
/// DTO for creating a new price list entry.
/// </summary>
public record CreatePriceDto(
    int ProductId,
    string ListCode,
    string UomName,
    decimal UnitPrice,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

/// <summary>
/// DTO for updating an existing price list entry.
/// </summary>
public record UpdatePriceDto(
    int Id,
    string ListCode,
    string UomName,
    decimal UnitPrice,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

/// <summary>
/// DTO for querying price list entries.
/// </summary>
public record PriceDto(
    int Id,
    int ProductId,
    string ListCode,
    string UomName,
    decimal UnitPrice,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo
);
