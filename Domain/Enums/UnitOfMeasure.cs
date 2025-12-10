namespace InventoryERP.Domain.Enums;

/// <summary>
/// R-050: Turkish-friendly unit of measure names for better UX
/// </summary>
public static class UnitOfMeasure
{
    // Temel Birimler (Basic Units)
    public const string Adet = "ADET";          // Piece
    public const string Kilogram = "KG";        // Kilogram
    public const string Gram = "GR";            // Gram
    public const string Metre = "MT";           // Meter
    public const string Santimetre = "CM";      // Centimeter
    public const string Litre = "LT";           // Liter
    public const string Mililitre = "ML";       // Milliliter
    
    // Paketleme Birimleri (Packaging Units)
    public const string Koli = "KOLI";          // Box
    public const string Paket = "PAKET";        // Package
    public const string Palet = "PALET";        // Pallet
    public const string Sandik = "SANDIK";      // Crate
    public const string Torba = "TORBA";        // Bag
    
    // Toplama Birimleri (Aggregate Units)
    public const string Set = "SET";            // Set
    public const string Takım = "TAKIM";        // Complete Set
    public const string Grup = "GRUP";          // Group
    public const string Lot = "LOT";            // Lot
    
    // Ticari Birimler (Commercial Units)
    public const string Düzine = "DUZINE";      // Dozen (12)
    public const string Gross = "GROSS";         // Gross (144)
    public const string Ton = "TON";            // Ton (1000 kg)
    
    // Alan/Hacim Birimleri (Area/Volume)
    public const string MetreKare = "M2";       // Square Meter
    public const string MetreKüp = "M3";        // Cubic Meter
    
    /// <summary>
    /// Get display name for a UOM code (Turkish-friendly)
    /// </summary>
    public static string GetDisplayName(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            Adet => "Adet",
            Kilogram => "Kilogram (kg)",
            Gram => "Gram (gr)",
            Metre => "Metre (mt)",
            Santimetre => "Santimetre (cm)",
            Litre => "Litre (lt)",
            Mililitre => "Mililitre (ml)",
            Koli => "Koli",
            Paket => "Paket",
            Palet => "Palet",
            Sandik => "Sandık",
            Torba => "Torba",
            Set => "Set",
            Takım => "Takım",
            Grup => "Grup",
            Lot => "Lot",
            Düzine => "Düzine (12 adet)",
            Gross => "Gross (144 adet)",
            Ton => "Ton (1000 kg)",
            MetreKare => "Metrekare (m²)",
            MetreKüp => "Metreküp (m³)",
            _ => code ?? "Bilinmeyen"
        };
    }
    
    /// <summary>
    /// Get all available UOM codes for dropdown lists
    /// </summary>
    public static string[] GetAllCodes()
    {
        return new[]
        {
            Adet,
            Kilogram,
            Gram,
            Metre,
            Santimetre,
            Litre,
            Mililitre,
            Koli,
            Paket,
            Palet,
            Sandik,
            Torba,
            Set,
            Takım,
            Grup,
            Lot,
            Düzine,
            Gross,
            Ton,
            MetreKare,
            MetreKüp
        };
    }
}
