using System;
using System.Collections.Generic;
using System.Linq;
using InventoryERP.Application.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventoryERP.Infrastructure.Services;

/// <summary>
/// R-095: PDF list export service implementation using QuestPDF
/// </summary>
public class ListPdfExportService : IListPdfExportService
{
    public void ExportToPdf<T>(IEnumerable<T> data, string filePath, string title = "Liste") where T : class
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));
        
        if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File path must end with .pdf", nameof(filePath));
        
        try
        {
            var dataList = data.ToList();
            // R-122 FIX 3: Get properties in user-friendly order (not alphabetical)
            var properties = GetOrderedProperties<T>();
            
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4); // R-123: Portrait (dikey) for better readability
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8)); // Reduced font for more columns
                    
                    page.Header().AlignCenter().Text(title).FontSize(14).Bold();
                    
                    page.Content().PaddingVertical(5).Table(table =>
                    {
                        // Define columns
                        var columnCount = (uint)properties.Count;
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < columnCount; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });
                        
                        // Header row
                        table.Header(header =>
                        {
                            foreach (var prop in properties)
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                    .Text(GetFriendlyPropertyName(prop.Name)).Bold();
                            }
                        });
                        
                        // R-122 FIX 2: Handle empty data
                        if (dataList.Count == 0)
                        {
                            table.Cell().ColumnSpan(columnCount).Padding(10)
                                .Text("Veri bulunamadı").Italic();
                        }
                        else
                        {
                            // Data rows
                            foreach (var item in dataList)
                            {
                                foreach (var prop in properties)
                                {
                                    var value = prop.GetValue(item);
                                    var displayValue = value?.ToString() ?? string.Empty;
                                    
                                    // Handle boolean display
                                    if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                                    {
                                        displayValue = value is bool b ? (b ? "✓" : "✗") : string.Empty;
                                    }
                                    
                                    table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
                                        .Text(displayValue);
                                }
                            }
                        }
                    });
                    
                    page.Footer()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Sayfa ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                            text.Span($" - {DateTime.Now:dd.MM.yyyy HH:mm}");
                        });
                });
            }).GeneratePdf(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PDF export failed: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// R-122 FIX 3: Get properties in user-friendly order (Name, Type, TaxId... before Id)
    /// </summary>
    private static List<System.Reflection.PropertyInfo> GetOrderedProperties<T>() where T : class
    {
        var type = typeof(T);
        var allProperties = type.GetProperties()
            .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
            .ToList();
        
        // Define preferred column order for Partner entities (R-123: Added Currency, reordered for template alignment)
        var preferredOrder = new[] 
        { 
            "Name", "PartnerType", "TaxId", "NationalId", "Phone", "Email", 
            "PaymentTermDays", "CreditLimitTry", "IsActive", "Id" 
        };
        
        var orderedProperties = new List<System.Reflection.PropertyInfo>();
        
        // Add properties in preferred order
        foreach (var propName in preferredOrder)
        {
            var prop = allProperties.FirstOrDefault(p => p.Name == propName);
            if (prop != null)
            {
                orderedProperties.Add(prop);
            }
        }
        
        // Add any remaining properties not in preferred order
        foreach (var prop in allProperties)
        {
            if (!orderedProperties.Contains(prop))
            {
                orderedProperties.Add(prop);
            }
        }
        
        return orderedProperties;
    }
    
    private static bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType.IsPrimitive
               || underlyingType == typeof(string)
               || underlyingType == typeof(decimal)
               || underlyingType == typeof(DateTime)
               || underlyingType == typeof(DateTimeOffset)
               || underlyingType == typeof(TimeSpan)
               || underlyingType == typeof(Guid)
               || underlyingType.IsEnum;
    }
    
    private static string GetFriendlyPropertyName(string propertyName)
    {
        // R-123: Turkish translations aligned with import template headers (matching ExcelExportService)
        return propertyName switch
        {
            "Id" => "ID",
            "Name" => "Cari Adı",
            "PartnerType" => "Cari Tipi",
            "TaxId" => "VKN",
            "NationalId" => "TCKN",
            "Email" => "E-posta",
            "Phone" => "Telefon",
            "IsActive" => "Aktif",
            "Address" => "Adres",
            "PaymentTermDays" => "Vade",
            "CreditLimitTry" => "Risk Durumu",
            _ => propertyName
        };
    }
}
