using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Import;
using InventoryERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace InventoryERP.Infrastructure.Services;

public sealed class ImportService : IImportService
{
    private readonly AppDbContext _db;
    private readonly Serilog.ILogger _logger; // R-037
    private readonly Application.Products.IBarcodeService _barcodeService; // R-332
    
    public ImportService(AppDbContext db, Serilog.ILogger logger, Application.Products.IBarcodeService barcodeService)
    {
        _db = db;
        _logger = logger;
        _barcodeService = barcodeService;
    }

    // v1.0.23: Retry logic to handle race condition (file lock after OpenFileDialog closes)
    // Part of "Super Fix" addressing all events.log errors including HeaderValidationException
    private async Task<StreamReader> OpenFileWithRetryAsync(string filePath, int maxRetries = 3, int delayMs = 250)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.Debug("R-037: Dosya açılıyor (Deneme {Attempt}/{MaxRetries}): {FilePath}", attempt, maxRetries, filePath);
                return new StreamReader(filePath);
            }
            catch (IOException) when (attempt < maxRetries)
            {
                _logger.Warning("R-037: Dosya kilidi algılandı, {DelayMs}ms sonra yeniden deneniyor... (Deneme {Attempt}/{MaxRetries})", delayMs, attempt, maxRetries);
                await Task.Delay(delayMs);
            }
        }
        
        // Final attempt - let exception propagate if it fails
        _logger.Debug("R-037: Son deneme (Deneme {MaxRetries}/{MaxRetries})", maxRetries);
        return new StreamReader(filePath);
    }

    public async Task<ImportResult> ImportProductsFromCsvAsync(string filePath, bool safeMode = true)
    {
        try
        {
            _logger.Information("R-037: ImportService: Ürün import işlemi başladı: {FilePath}", filePath);
            
            int addedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;
            // P1 Fix v1.0.17: Read CSV completely into memory BEFORE any DB operations
            // This ensures file is closed before SaveChangesAsync() call
            var records = new System.Collections.Generic.List<ProductCsvRecord>();
            
            // v1.0.23: Use retry logic to handle race condition (file lock after OpenFileDialog)
            // Part of "Super Fix" - HeaderValidated = null ensures unknown columns are ignored
            using (var reader = await OpenFileWithRetryAsync(filePath))
            using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,        // v1.0.23: Don't fail on missing optional fields
                IgnoreBlankLines = true,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                Delimiter = ";",                 // Turkish CSV format
                HeaderValidated = null,          // v1.0.23: CRITICAL - Ignore unknown columns like "Stok Adedi"
            }))
            {
                csv.Context.RegisterClassMap<ProductCsvRecordMap>();
                _logger.Debug("R-037: ImportService: CSV okunuyor (GetRecordsAsync)...");
                await foreach (var record in csv.GetRecordsAsync<ProductCsvRecord>())
                {
                    records.Add(record);
                }
                _logger.Debug("R-037: ImportService: CSV tamamen okundu. {RecordCount} kayıt", records.Count);
            } // File is now CLOSED before any DB operations
            
            _logger.Debug("R-037: ImportService: Dosya kapatıldı. DB işlemleri başlıyor...");
            
            // R-336: Pre-flight check - Get default warehouse/location for stock moves
            var defaultLocationId = await _db.Locations.OrderBy(l => l.Id).Select(l => l.Id).FirstOrDefaultAsync();
            if (defaultLocationId == 0)
            {
                throw new Exception("Sistemde tanımlı lokasyon (depo) bulunamadı! Lütfen önce Ayarlar'dan bir depo ve lokasyon tanımlayın.");
            }
            _logger.Debug("R-336: Import için varsayılan lokasyon ID: {LocationId}", defaultLocationId);
            
            // v1.0.22: Process records AFTER file is closed - with UPSERT logic
            // Part of "Super Fix" addressing UNIQUE constraint violation (Hata 3)
            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Sku) || string.IsNullOrWhiteSpace(record.Name) || string.IsNullOrWhiteSpace(record.BaseUom))
                    continue;
                
                // v1.0.22: UPSERT - Check if product exists by SKU
                // Part of "Super Fix" addressing UNIQUE constraint violation (Hata 3)
                var existingProduct = await _db.Products.FirstOrDefaultAsync(p => p.Sku == record.Sku);
                
                if (existingProduct == null)
                {
                    // INSERT: Product doesn't exist, add new
                    var newProduct = new Product
                    {
                        Sku = record.Sku!,
                        Name = record.Name!,
                        BaseUom = record.BaseUom!,
                        VatRate = record.VatRate ?? 1,
                        Active = record.Active ?? true,
                        Category = record.Category,
                        Brand = record.Brand,          // R-323
                        Barcode = record.Barcode,      // R-323
                        SalesPrice = record.SalesPrice ?? 0, // R-323
                        Cost = record.Cost ?? 0        // R-323
                    };

                    // R-332: Auto-Generate Barcode if missing
                    if (string.IsNullOrWhiteSpace(newProduct.Barcode))
                    {
                        newProduct.Barcode = await _barcodeService.GenerateUniqueBarcodeAsync();
                    }

                    _db.Products.Add(newProduct);
                    await _db.SaveChangesAsync(); // Save immediately to get ID for StockMove


                    // R-323: Single-File Import Logic (Ghost Stock Prevention)
                    // If InitialQty > 0, create a StockMove immediately
                    if (record.InitialQty.HasValue && record.InitialQty.Value > 0)
                    {
                        // R-336: Use pre-fetched defaultLocationId (from pre-flight check)
                        var move = new StockMove
                        {
                            ItemId = newProduct.Id,
                            Date = System.DateTime.Today,
                            QtySigned = record.InitialQty.Value,
                            UnitCost = record.Cost, // Use the imported cost
                            Note = "IMPORT",        // Traceability
                            DestinationLocationId = defaultLocationId // R-336: Fix Inbound Move
                        };
                         _db.StockMoves.Add(move);
                    }
                    addedCount++;
                }
                else
                {
                    // R-326: Safe Import Mode
                    if (safeMode)
                    {
                        skippedCount++;
                        continue; // Skip update if Safe Mode is ON
                    }

                    // UPDATE: Product exists, update fields from CSV
                    existingProduct.Name = record.Name!;
                    existingProduct.BaseUom = record.BaseUom!;
                    existingProduct.VatRate = record.VatRate ?? 1;
                    existingProduct.Active = record.Active ?? true;
                    existingProduct.Category = record.Category;
                    // R-323: Update extended fields
                    if (!string.IsNullOrWhiteSpace(record.Brand)) existingProduct.Brand = record.Brand;
                    
                    // R-332: Update logic for Barcode
                    if (!string.IsNullOrWhiteSpace(record.Barcode)) 
                    {
                        existingProduct.Barcode = record.Barcode;
                    }
                    else if (string.IsNullOrWhiteSpace(existingProduct.Barcode))
                    {
                        // Auto-generate if header exists but value is empty AND existing product has no barcode
                        existingProduct.Barcode = await _barcodeService.GenerateUniqueBarcodeAsync();
                    }

                    if (record.SalesPrice.HasValue) existingProduct.SalesPrice = record.SalesPrice.Value;
                    if (record.Cost.HasValue) existingProduct.Cost = record.Cost.Value;
                    // Note: Sku is NOT updated (it's the unique key)
                    updatedCount++;
                }
            }
            
            _logger.Debug("R-037: ImportService: SaveChangesAsync çağrılıyor...");
            await _db.SaveChangesAsync();
            _logger.Information("R-037: ImportService: Ürün import tamamlandı. {Added} eklendi, {Updated} güncellendi, {Skipped} atlandı.", addedCount, updatedCount, skippedCount);
            return new ImportResult(addedCount, updatedCount, skippedCount, records.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "R-037: CSV Import servisinde hata oluştu! FilePath: {FilePath}", filePath);
            
            // R-341: Extract the deepest Inner Exception to unmask the real database error
            var realError = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                realError += " --> " + inner.Message;
                inner = inner.InnerException;
            }
            
            // Throw a clean error for the UI to display the REAL cause
            throw new Exception($"VERİTABANI HATASI: {realError}", ex);
        }
    }

    public async Task<int> ImportPartnersFromCsvAsync(string filePath)
    {
        int count = 0;
        // P1 Fix v1.0.17: Read CSV completely into memory BEFORE any DB operations
        var records = new System.Collections.Generic.List<PartnerCsvRecord>();
        
        // v1.0.21: Use retry logic to handle race condition (file lock after OpenFileDialog)
        using (var reader = await OpenFileWithRetryAsync(filePath))
        using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
            TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
        }))
        {
            csv.Context.RegisterClassMap<PartnerCsvRecordMap>();
            await foreach (var record in csv.GetRecordsAsync<PartnerCsvRecord>())
            {
                records.Add(record);
            }
        } // File is now CLOSED before any DB operations
        
        // Process records AFTER file is closed
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Title)) continue;
            var exists = await _db.Partners.AnyAsync(p => p.Title == record.Title);
            if (exists) continue;
            if (!System.Enum.TryParse<InventoryERP.Domain.Enums.PartnerRole>(record.Role ?? string.Empty, true, out var parsedRole)) parsedRole = InventoryERP.Domain.Enums.PartnerRole.CUSTOMER;
            _db.Partners.Add(new Partner { Title = record.Title!, Role = parsedRole, TaxNo = record.TaxNo });
            count++;
        }
        
        await _db.SaveChangesAsync();
        return count;
    }

    public async Task<int> ImportOpeningBalancesFromCsvAsync(string filePath)
    {
        int count = 0;
        int skipped = 0;
        // P1 Fix v1.0.17: Read CSV completely into memory BEFORE any DB operations
        var records = new System.Collections.Generic.List<OpeningCsvRecord>();
        
        // v1.0.21: Use retry logic to handle race condition (file lock after OpenFileDialog)
        using (var reader = await OpenFileWithRetryAsync(filePath))
        using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,        // v1.0.23: Don't fail on missing optional fields
            IgnoreBlankLines = true,
            TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
            Delimiter = ";",                 // v1.0.23: Turkish CSV format
            HeaderValidated = null,          // v1.0.23: CRITICAL - Ignore unknown columns like "Kategori"
        }))
        {
            csv.Context.RegisterClassMap<OpeningCsvRecordMap>();
            await foreach (var record in csv.GetRecordsAsync<OpeningCsvRecord>())
            {
                records.Add(record);
            }
        } // File is now CLOSED before any DB operations
        
        // Process records AFTER file is closed
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Sku))
            {
                skipped++;
                continue;
            }
            
            var prod = await _db.Products.FirstOrDefaultAsync(p => p.Sku == record.Sku);
            if (prod == null)
            {
                skipped++;
                continue; // Product not found - skip silently
            }
            
            var qty = record.Qty ?? 0m;
            if (qty == 0m)
            {
                skipped++;
                continue; // Zero quantity - skip
            }
            
            decimal? unitCost = record.UnitCost;
            _db.StockMoves.Add(new StockMove 
            { 
                ItemId = prod.Id, 
                Date = System.DateTime.Today, 
                QtySigned = qty, 
                UnitCost = unitCost, 
                Note = "OPENING" 
            });
            count++;
        }
        
        await _db.SaveChangesAsync();
        
        // v1.0.18: Better error message if no records imported
        if (count == 0 && records.Count > 0)
        {
            throw new System.Exception($"0 stok hareketi içe aktarıldı. {records.Count} satır okundu, {skipped} satır atlandı. Önce 'CSV'den Ürün İçe Aktar' ile ürünleri içe aktardığınızdan emin olun.");
        }
        
        return count;
    }

    // v1.0.18: Custom converter for percentage strings like "20%" -> 20
    // R-343: Enhanced Smart VAT Parser - handles 20, 0.20, %20, 20%
    private sealed class PercentageConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            
            text = text.Trim();
            
            // Remove percentage sign if present (handles both %20 and 20%)
            if (text.StartsWith("%"))
                text = text.TrimStart('%').Trim();
            if (text.EndsWith("%"))
                text = text.TrimEnd('%').Trim();
            
            // Try parse as decimal first (to handle 0.20 format)
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decResult))
            {
                int finalVal;
                
                // R-343: Smart detection - if value is between 0 and 1 (exclusive), it's a decimal percentage
                if (decResult > 0 && decResult < 1)
                {
                    // Case: 0.18 or 0.20 -> Multiply by 100
                    finalVal = (int)(decResult * 100);
                }
                else
                {
                    // Case: 1, 8, 18, 20 -> Take as is
                    finalVal = (int)decResult;
                }
                
                // R-343: Safety clamp (0-100)
                if (finalVal < 0) finalVal = 0;
                if (finalVal > 100) finalVal = 20; // Fallback for crazy numbers
                
                return finalVal;
            }
            
            return null;
        }
    }

    // v1.0.19: Custom converter for Turkish boolean strings like "Aktif" -> true
    private sealed class TurkishBooleanConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            
            text = text.Trim();
            
            // Turkish boolean values
            if (string.Equals(text, "Aktif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if (string.Equals(text, "Pasif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Passive", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            // Default to true if unrecognized (safer for "Aktif" variations)
            return null;
        }
    }

    private sealed class ProductCsvRecord
    {
        public string? Sku { get; init; }
        public string? Name { get; init; }
        public string? BaseUom { get; init; }
        public int? VatRate { get; init; }
        public bool? Active { get; init; }
        public string? Category { get; init; } // R-033: Product category
        public string? Brand { get; init; }    // R-323
        public string? Barcode { get; init; }  // R-323
        public decimal? SalesPrice { get; init; } // R-323
        public decimal? Cost { get; init; }       // R-323
        public decimal? InitialQty { get; init; } // R-323: Single-file import support
    }

    private sealed class ProductCsvRecordMap : ClassMap<ProductCsvRecord>
    {
        public ProductCsvRecordMap()
        {
            // R-033: Product Master Data Import (Ürün Kartı)
            // R-323: Unified Import (Added Brand, Barcode, Prices, Initial Stock)
            Map(m => m.Sku).Name("Sku", "Ürün Kodu", "Stok Kodu");
            Map(m => m.Name).Name("Name", "Ürün Adı", "Stok Adı");
            Map(m => m.BaseUom).Name("BaseUom", "Birim", "Stok Türü");
            Map(m => m.VatRate).Name("VatRate", "KDV Oranı", "Alış Kdv").Optional().TypeConverter<PercentageConverter>(); 
            Map(m => m.Active).Name("Active", "Aktif").Optional().TypeConverter<TurkishBooleanConverter>(); 
            Map(m => m.Category).Name("Category", "Kategori").Optional();
            
            // New R-323 Fields
            Map(m => m.Brand).Name("Brand", "Marka").Optional();
            Map(m => m.Barcode).Name("Barcode", "Barkod").Optional();
            Map(m => m.SalesPrice).Name("SalesPrice", "Satış Fiyatı", "Fiyat").Optional();
            Map(m => m.Cost).Name("Cost", "Maliyet", "Alış Fiyatı").Optional();
            Map(m => m.InitialQty).Name("InitialQty", "Açılış Stoğu", "Stok Adedi", "Miktar").Optional();
        }
    }

    private sealed class PartnerCsvRecord
    {
        public string? Title { get; init; }
        public string? Role { get; init; }
        public string? TaxNo { get; init; }
    }

    private sealed class PartnerCsvRecordMap : ClassMap<PartnerCsvRecord>
    {
        public PartnerCsvRecordMap()
        {
            Map(m => m.Title).Name("Title", "Ünvan");
            Map(m => m.Role).Name("Role", "Rol").Optional();
            Map(m => m.TaxNo).Name("TaxNo", "Vergi No").Optional();
        }
    }

    private sealed class OpeningCsvRecord
    {
        public string? Sku { get; init; }
        public decimal? Qty { get; init; }
        public decimal? UnitCost { get; init; }
    }

    private sealed class OpeningCsvRecordMap : ClassMap<OpeningCsvRecord>
    {
        public OpeningCsvRecordMap()
        {
            // R-033.A: Opening Stock Import from user's Stok_Aktarım.csv
            Map(m => m.Sku).Name("Sku", "Ürün Kodu");
            Map(m => m.Qty).Name("Qty", "Miktar", "Stok Adedi").Optional(); // R-033.A: User's "Stok Adedi" header
            Map(m => m.UnitCost).Name("UnitCost", "Birim Maliyet", "Ürün Maliyeti").Optional(); // R-033.A: User's "Ürün Maliyeti" header
        }
    }
}
