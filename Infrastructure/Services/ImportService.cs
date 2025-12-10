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
    
    public ImportService(AppDbContext db, Serilog.ILogger logger)
    {
        _db = db;
        _logger = logger;
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

    public async Task<int> ImportProductsFromCsvAsync(string filePath)
    {
        try
        {
            _logger.Information("R-037: ImportService: Ürün import işlemi başladı: {FilePath}", filePath);
            
            int count = 0;
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
                    _db.Products.Add(new Product
                    {
                        Sku = record.Sku!,
                        Name = record.Name!,
                        BaseUom = record.BaseUom!,
                        VatRate = record.VatRate ?? 1,
                        Active = record.Active ?? true,
                        Category = record.Category // R-033: Product category
                        // NOTE: Cost removed - will be set via Opening Stock import (R-033.A)
                    });
                    count++;
                }
                else
                {
                    // UPDATE: Product exists, update fields from CSV
                    existingProduct.Name = record.Name!;
                    existingProduct.BaseUom = record.BaseUom!;
                    existingProduct.VatRate = record.VatRate ?? 1;
                    existingProduct.Active = record.Active ?? true;
                    existingProduct.Category = record.Category;
                    // Note: Sku is NOT updated (it's the unique key)
                    count++;
                }
            }
            
            _logger.Debug("R-037: ImportService: SaveChangesAsync çağrılıyor...");
            await _db.SaveChangesAsync();
            _logger.Information("R-037: ImportService: Ürün import tamamlandı. {Count} ürün eklendi", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "R-037: CSV Import servisinde hata oluştu! FilePath: {FilePath}", filePath);
            throw; // Re-throw to let ViewModel handle it
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
    private sealed class PercentageConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            
            text = text.Trim();
            
            // Remove percentage sign if present
            if (text.EndsWith("%"))
                text = text.TrimEnd('%').Trim();
            
            // Try parse as integer
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            
            // Try parse as decimal and convert to int
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var decResult))
                return (int)decResult;
            
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
    }

    private sealed class ProductCsvRecordMap : ClassMap<ProductCsvRecord>
    {
        public ProductCsvRecordMap()
        {
            // R-033: Product Master Data Import (Ürün Kartı)
            // v1.0.22 "Super Fix": Strategy includes Type Converters (Hata 2)
            // Strategy: Ignore unknown columns like "Stok Adedi" (handled by Opening Stock import)
            Map(m => m.Sku).Name("Sku", "Ürün Kodu", "Stok Kodu");
            Map(m => m.Name).Name("Name", "Ürün Adı", "Stok Adı");
            Map(m => m.BaseUom).Name("BaseUom", "Birim", "Stok Türü");
            Map(m => m.VatRate).Name("VatRate", "KDV Oranı", "Alış Kdv").Optional().TypeConverter<PercentageConverter>(); // v1.0.22: Parse "20%" -> 20 (Hata 2)
            Map(m => m.Active).Name("Active", "Aktif").Optional().TypeConverter<TurkishBooleanConverter>(); // v1.0.22: Parse "Aktif" -> true (Hata 2)
            Map(m => m.Category).Name("Category", "Kategori").Optional(); // R-033: Product category
            // NOTE: Cost removed - moved to Opening Stack import (R-033.A)
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
