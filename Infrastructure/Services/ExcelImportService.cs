using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Import;
using InventoryERP.Application.Partners;
using ClosedXML.Excel;
using InventoryERP.Domain.Enums;

namespace InventoryERP.Infrastructure.Services
{
    /// <summary>
    /// R-093/R-119: Excel import service for partners (cari) using ClosedXML
    /// Expected Excel format (Worksheet 1):
    /// Column A: Cari Adı (required)
    /// Column B: Cari Tipi (Müşteri/Satıcı/BOTH - required, Turkish or English)
    /// Column C: VKN (optional, 10 digits)
    /// Column D: TCKN (optional, 11 digits) - at least one of VKN/TCKN required
    /// Column E: Telefon (optional)
    /// Column F: e-posta (optional)
    /// Column G: İlgili Kişi Adı (optional)
    /// Column H: Açıklama (optional)
    /// Column I: Vade (optional: 1 hafta, 15 gün, 30 gün, 45 gün, 60 gün, 90 gün, 120 gün)
    /// Column J: Ödeme Türü (optional: Nakit, Kredi Kartı, Çek)
    /// Column K: Risk Durumu (optional, decimal - credit limit)
    /// Column L: İş Yeri Adresi (optional)
    /// Column M: Sevkiyat Adresi (optional)
    /// </summary>
    public class ExcelImportService : IExcelImportService
    {
        private readonly IPartnerService _partnerService;

        public ExcelImportService(IPartnerService partnerService)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
        }

        public async Task<ExcelImportResult> ImportPartnersAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Dosya yolu boş olamaz.", nameof(filePath));
            }

            var errors = new List<string>();
            var successCount = 0;
            var failureCount = 0;

            try
            {
                if (!File.Exists(filePath))
                {
                    return new ExcelImportResult
                    {
                        Success = false,
                        SuccessCount = 0,
                        FailureCount = 0,
                        Errors = new List<string> { $"Excel dosyası bulunamadı: '{filePath}'" }
                    };
                }

                // Open workbook in a way that tolerates the file being open in Excel
                // Strategy: open with FileShare.ReadWrite and copy to a MemoryStream snapshot
                using var workbook = OpenWorkbookSnapshot(filePath);
                var worksheet = workbook.Worksheet(1);

                // Kullanılan satırlar (başlık dahil)
                var usedRows = worksheet.RowsUsed().ToList();
                if (usedRows.Count == 0)
                {
                    return new ExcelImportResult
                    {
                        Success = false,
                        SuccessCount = 0,
                        FailureCount = 0,
                        Errors = new List<string> { "Excel sayfasında hiç veri bulunamadı." }
                    };
                }

                // İlk satır başlık varsayılıyor
                var dataRows = usedRows.Skip(1);

                // 1. Faz: Tüm satırları parse + validasyon (henüz DB'ye dokunmuyoruz)
                var parsedRows = new List<PartnerRowResult>();

                foreach (var row in dataRows)
                {
                    // Tamamen boş satırları sessizce atla (hata sayma)
                    if (RowIsEmpty(row))
                        continue;

                    var parsed = ParseRow(row);
                    parsedRows.Add(parsed);
                }

                // Hiç veri satırı yoksa (sadece başlık varsa)
                if (parsedRows.Count == 0)
                {
                    return new ExcelImportResult
                    {
                        Success = false,
                        SuccessCount = 0,
                        FailureCount = 0,
                        Errors = new List<string> { "Başlık satırı dışında veri satırı bulunamadı." }
                    };
                }

                // 2. Faz: Geçerli satırları kaydet, hatalıları raporla
                foreach (var rowResult in parsedRows)
                {
                    // Parse/validasyon hatası olan satırlar
                    if (rowResult.Errors.Count > 0 || rowResult.Dto == null)
                    {
                        failureCount++;
                        foreach (var err in rowResult.Errors)
                        {
                            errors.Add($"Satır {rowResult.RowNumber}: {err}");
                        }

                        continue;
                    }

                    // DB kaydı
                    try
                    {
                        await _partnerService.SaveAsync(rowResult.Dto);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        errors.Add($"Satır {rowResult.RowNumber}: Kayıt sırasında beklenmeyen bir hata oluştu: {ex.Message}");
                    }
                }

                return new ExcelImportResult
                {
                    Success = failureCount == 0,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                // Excel açma/okuma seviyesindeki hatalar
                var errMsg = $"Excel dosyası okunurken beklenmeyen bir hata oluştu: {ex.Message}";
                if (ex is IOException || ex.Message.Contains("another process", StringComparison.OrdinalIgnoreCase))
                {
                    errMsg += " (Not: Dosya Excel'de açık olabilir. Lütfen dosyayı kapatıp tekrar deneyin.)";
                }

                return new ExcelImportResult
                {
                    Success = false,
                    SuccessCount = 0,
                    FailureCount = 0,
                    Errors = new List<string> { errMsg }
                };
            }
        }

        /// <summary>
        /// Opens the Excel workbook as a memory snapshot so that imports work even if the file
        /// is currently open in Excel (which may hold a write lock). We open the file stream with
        /// FileShare.ReadWrite, copy to MemoryStream, and initialize ClosedXML from the snapshot.
        /// </summary>
        private static XLWorkbook OpenWorkbookSnapshot(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Position = 0;
                return new XLWorkbook(ms);
            }
            catch (IOException)
            {
                // As a fallback, try copying to a temp file (in case sharing still prevents direct open)
                var tempPath = Path.Combine(Path.GetTempPath(), $"partner_import_{Guid.NewGuid():N}.xlsx");
                File.Copy(filePath, tempPath, overwrite: true);
                try
                {
                    // Load from temp, then delete temp immediately after loading
                    using var fs2 = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var ms2 = new MemoryStream();
                    fs2.CopyTo(ms2);
                    ms2.Position = 0;
                    return new XLWorkbook(ms2);
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
                }
            }
        }

        private static bool RowIsEmpty(IXLRow row)
        {
            // İlk 13 sütuna bakmamız yeterli (A..M)
            return row.Cells(1, 13).All(c => c.IsEmpty());
        }

        private static PartnerRowResult ParseRow(IXLRow row)
        {
            var result = new PartnerRowResult(row.RowNumber());

            // Ham string okumalar (trim'li)
            var name = row.Cell(1).GetString().Trim();               // A: Cari Adı
            var partnerTypeRaw = row.Cell(2).GetString().Trim();     // B: Cari Tipi
            var taxId = row.Cell(3).GetString().Trim();              // C: VKN
            var nationalId = row.Cell(4).GetString().Trim();         // D: TCKN
            var phone = row.Cell(5).GetString().Trim();              // E: Telefon
            var email = row.Cell(6).GetString().Trim();              // F: e-posta
            var description = row.Cell(8).GetString().Trim();        // H: Açıklama
            var paymentTermCell = row.Cell(9);                       // I: Vade
            var paymentMethod = row.Cell(10).GetString().Trim();     // J: Ödeme Türü
            var creditLimit = GetDecimalOrNull(row.Cell(11), "Risk Durumu (Kredi limiti)", result.Errors);  // K: Risk Durumu

            // Zorunlu alan: Name
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add("İsim alanı boş olamaz.");
            }

            // Zorunlu alan: PartnerType (Türkçe veya İngilizce kabul et)
            PartnerType partnerType = default;
            if (string.IsNullOrWhiteSpace(partnerTypeRaw))
            {
                result.Errors.Add("Cari tipi alanı boş olamaz. Geçerli değerler: Müşteri, Satıcı, BOTH.");
            }
            else
            {
                partnerType = ParsePartnerType(partnerTypeRaw);
                if (partnerType == default && !Enum.TryParse(partnerTypeRaw, true, out partnerType))
                {
                    result.Errors.Add($"Geçersiz cari tipi: '{partnerTypeRaw}'. Geçerli değerler: Müşteri, Satıcı, BOTH (veya CUSTOMER, SUPPLIER).");
                }
            }

            // Parse Vade (Payment Term) - Türkçe seçenekler
            int? paymentTermDays = ParsePaymentTerm(paymentTermCell, result.Errors);

            // Ödeme Türü artık modele yansıtılmadığı için sadece serbest metin olarak bırakıyoruz

            // Buraya kadar bir hata varsa DTO oluşturmadan satırı hatalı kabul et
            if (result.Errors.Count > 0)
            {
                return result;
            }

            // Başarılı parse edilen DTO
            result.Dto = new PartnerCrudDetailDto
            {
                Id = 0, // Yeni cari
                PartnerType = partnerType.ToString(),
                Name = name,
                TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId,
                NationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId,
                PaymentTermDays = paymentTermDays,
                CreditLimitTry = creditLimit,
                IsActive = true
            };

            return result;
        }

        /// <summary>
        /// Parse Turkish PartnerType values (Müşteri, Satıcı, BOTH)
        /// </summary>
        private static PartnerType ParsePartnerType(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "müşteri" => PartnerType.Customer,
                "musteri" => PartnerType.Customer,
                "satıcı" => PartnerType.Supplier,
                "satici" => PartnerType.Supplier,
                "both" => PartnerType.Both,
                _ => default
            };
        }

        /// <summary>
        /// Parse Turkish payment term options (text or numeric)
        /// </summary>
        private static int? ParsePaymentTerm(IXLCell cell, List<string> errors)
        {
            if (cell == null || cell.IsEmpty())
                return null;

            if (cell.TryGetValue<double>(out var numericValue))
            {
                var rounded = (int)Math.Round(numericValue);
                if (rounded < 0)
                {
                    errors.Add("Vade değeri negatif olamaz.");
                    return null;
                }

                return rounded;
            }

            var value = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.ToLowerInvariant() switch
            {
                "1 hafta" => 7,
                "7 gün" => 7,
                "7 gun" => 7,
                "7" => 7,
                "15 gün" => 15,
                "15 gun" => 15,
                "15" => 15,
                "30 gün" => 30,
                "30 gun" => 30,
                "30" => 30,
                "45 gün" => 45,
                "45 gun" => 45,
                "45" => 45,
                "60 gün" => 60,
                "60 gun" => 60,
                "60" => 60,
                "90 gün" => 90,
                "90 gun" => 90,
                "90" => 90,
                "120 gün" => 120,
                "120 gun" => 120,
                "120" => 120,
                _ => HandleInvalidPaymentTerm(value, errors)
            };
        }

        private static int? HandleInvalidPaymentTerm(string value, List<string> errors)
        {
            // Sayı olarak girilmişse direkt kullan
            if (int.TryParse(value, out var days) && days >= 0)
            {
                return days;
            }

            errors.Add($"Geçersiz vade değeri: '{value}'. Geçerli değerler: 1 hafta, 15 gün, 30 gün, 45 gün, 60 gün, 90 gün, 120 gün (veya sayı olarak gün sayısı).");
            return null;
        }

        private static decimal? GetDecimalOrNull(IXLCell cell, string fieldDisplayName, List<string> errors)
        {
            if (cell == null || cell.IsEmpty())
                return null;

            // Önce gerçek Excel numeric tipi olarak dene
            if (cell.TryGetValue<decimal>(out var numericValue))
            {
                if (numericValue < 0)
                {
                    errors.Add($"{fieldDisplayName} negatif olamaz.");
                    return null;
                }

                return numericValue;
            }

            // Sonra text olarak kültürlü parse dene
            var text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            decimal parsed;

            // TR kültürü
            var cultureTr = CultureInfo.GetCultureInfo("tr-TR");
            if (decimal.TryParse(text, NumberStyles.Any, cultureTr, out parsed) ||
                decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                if (parsed < 0)
                {
                    errors.Add($"{fieldDisplayName} negatif olamaz.");
                    return null;
                }

                return parsed;
            }

            errors.Add($"{fieldDisplayName} sayısal bir değer olmalıdır.");
            return null;
        }

        private static int? GetIntOrNull(IXLCell cell, string fieldDisplayName, List<string> errors)
        {
            if (cell == null || cell.IsEmpty())
                return null;

            if (cell.TryGetValue<int>(out var numericValue))
            {
                if (numericValue < 0)
                {
                    errors.Add($"{fieldDisplayName} negatif olamaz.");
                    return null;
                }

                return numericValue;
            }

            var text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            int parsed;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ||
                int.TryParse(text, NumberStyles.Integer, CultureInfo.GetCultureInfo("tr-TR"), out parsed))
            {
                if (parsed < 0)
                {
                    errors.Add($"{fieldDisplayName} negatif olamaz.");
                    return null;
                }

                return parsed;
            }

            errors.Add($"{fieldDisplayName} sayısal bir değer olmalıdır.");
            return null;
        }

        // Tek satırın parse + validasyon sonucu
        private sealed class PartnerRowResult
        {
            public int RowNumber { get; }
            public PartnerCrudDetailDto? Dto { get; set; }
            public List<string> Errors { get; }

            public PartnerRowResult(int rowNumber)
            {
                RowNumber = rowNumber;
                Errors = new List<string>();
            }
        }
    }
}
