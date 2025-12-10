namespace InventoryERP.Application.Partners;

/// <summary>
/// R-086: Partner (Cari) DTO for list display in CRUD UI
/// </summary>
public record PartnerCrudListDto(
    int Id,
    string Name,
    string PartnerType,
    string? TaxId,
    string? NationalId,
    bool IsActive
)
{
    // R-094: Override ToString() for ComboBox display (WPF IsEditable uses ToString() for Text property)
    public override string ToString() => Name;
}

/// <summary>
/// R-086: Partner (Cari) DTO for edit/detail in CRUD UI
/// </summary>
public record PartnerCrudDetailDto
{
    public int Id { get; set; }
    public string PartnerType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public decimal? CreditLimitTry { get; set; }
    public int? PaymentTermDays { get; set; }
    public bool IsActive { get; set; } = true;
}
