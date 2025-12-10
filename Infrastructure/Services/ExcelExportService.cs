using System;
using System.Collections.Generic;
using System.Linq;
using InventoryERP.Application.Export;
using ClosedXML.Excel;

namespace InventoryERP.Infrastructure.Services;

/// <summary>
/// R-095: Excel export service implementation using ClosedXML
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public void ExportToExcel<T>(IEnumerable<T> data, string filePath, string sheetName = "Data") where T : class
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));
        
        if (!filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File path must end with .xlsx", nameof(filePath));
        
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            
            var dataList = data.ToList();
            
            // R-122 FIX 3: Get properties in user-friendly order (not alphabetical)
            var properties = GetOrderedProperties<T>();
            
            // R-122 FIX 2: Handle empty data - show headers + message
            if (dataList.Count == 0)
            {
                // Add headers
                for (int i = 0; i < properties.Count; i++)
                {
                    var header = worksheet.Cell(1, i + 1);
                    header.Value = GetFriendlyPropertyName(properties[i].Name);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                
                // Add "Veri bulunamadı" message in first data row
                worksheet.Cell(2, 1).Value = "Veri bulunamadı";
                worksheet.Cell(2, 1).Style.Font.Italic = true;
            }
            else
            {
                // Add headers
                for (int i = 0; i < properties.Count; i++)
                {
                    var header = worksheet.Cell(1, i + 1);
                    header.Value = GetFriendlyPropertyName(properties[i].Name);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                
                // Add data rows
                for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
                {
                    var item = dataList[rowIndex];
                    for (int colIndex = 0; colIndex < properties.Count; colIndex++)
                    {
                        var value = properties[colIndex].GetValue(item);
                        var cell = worksheet.Cell(rowIndex + 2, colIndex + 1);
                        
                        if (value != null)
                        {
                            cell.Value = XLCellValue.FromObject(value);
                        }
                    }
                }
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
            }
            
            workbook.SaveAs(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Excel export failed: {ex.Message}", ex);
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
        
        // Define preferred column order for Partner entities (R-123: Added Currency for complete export template)
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
        // R-123: Turkish translations aligned with import template headers
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
