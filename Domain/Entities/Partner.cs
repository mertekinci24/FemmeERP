using InventoryERP.Domain.Common;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Domain.Entities;

/// <summary>
/// R-086: Partner (Cari) entity - Customer/Supplier management
/// Enhanced from existing Partner with R-086 requirements
/// </summary>
public class Partner : EntityBase
{
    /// <summary>
    /// Partner type: Customer, Supplier, or Other
    /// </summary>
    public PartnerType PartnerType { get; set; }
    
    /// <summary>
    /// Partner name/title (Unvan)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tax ID (VKN) - for companies (10 digits)
    /// </summary>
    public string? TaxId { get; set; }
    
    /// <summary>
    /// National ID (TCKN) - for individuals (11 digits)
    /// </summary>
    public string? NationalId { get; set; }
    
    /// <summary>
    /// Address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// Credit limit in TRY (optional) - Risk Durumu
    /// </summary>
    public decimal? CreditLimitTry { get; set; }
    
    /// <summary>
    /// Payment term in days (optional) - Vade
    /// Options: 7 (1 hafta), 15, 30, 45, 60, 90, 120 days
    /// </summary>
    public int? PaymentTermDays { get; set; }
    
    /// <summary>
    /// Is this partner active?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Legacy field mapping (kept for backward compatibility if needed)
    [Obsolete("Use PartnerType instead")]
    public PartnerRole Role
    {
        get => PartnerType == PartnerType.Customer ? PartnerRole.CUSTOMER : PartnerRole.SUPPLIER;
        set => PartnerType = value == PartnerRole.CUSTOMER ? PartnerType.Customer : PartnerType.Supplier;
    }
    
    [Obsolete("Use Name instead")]
    public string Title
    {
        get => Name;
        set => Name = value;
    }
    
    [Obsolete("Use TaxId instead")]
    public string? TaxNo
    {
        get => TaxId;
        set => TaxId = value;
    }
    
    /// <summary>
    /// R-086/R-110: Validate Partner entity business rules
    /// Rule: For Customer/Supplier/Other, a partner must have EITHER a valid TaxId (VKN, 10 digits)
    /// OR a valid NationalId (TCKN, 11 digits). If at least one is valid, accept and do not fail
    /// due to the other being missing/invalid. Only error when neither identifier is valid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Partner name is required");
        }
        
        // Normalize inputs
        var taxId = string.IsNullOrWhiteSpace(TaxId) ? null : TaxId.Trim();
        var nationalId = string.IsNullOrWhiteSpace(NationalId) ? null : NationalId.Trim();

        // Evaluate validity
        bool taxIdValid = taxId is not null && System.Text.RegularExpressions.Regex.IsMatch(taxId, @"^\d{10}$");
        bool nationalIdValid = nationalId is not null && System.Text.RegularExpressions.Regex.IsMatch(nationalId, @"^\d{11}$");

        // Business rule (R-110): Accept if either identifier is valid
        if (!taxIdValid && !nationalIdValid)
        {
            // Neither provided correctly: emit the most helpful error based on what was provided
            if (taxId is not null && (nationalId is null || nationalId.Length == 0))
                throw new InvalidOperationException("TaxId (VKN) must be exactly 10 digits");
            if (nationalId is not null && (taxId is null || taxId.Length == 0))
                throw new InvalidOperationException("NationalId (TCKN) must be exactly 11 digits");
            // Both provided but both invalid: prefer combined requirement message
            throw new InvalidOperationException("Either TaxId (VKN) or NationalId (TCKN) is required");
        }
    }
}

