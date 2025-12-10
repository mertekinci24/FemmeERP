using System;
using System.Text.RegularExpressions;

namespace InventoryERP.Presentation.Helpers
{
    public static class ExportFileNameHelper
    {
        // Build base name without extension
        public static string BuildBaseName(DateTime? from, DateTime? to, string partnerName, string? search)
        {
            var parts = new System.Collections.Generic.List<string>();
            var fromStr = from?.ToString("yyyy-MM-dd") ?? "";
            var toStr = to?.ToString("yyyy-MM-dd") ?? "";
            if (!string.IsNullOrEmpty(fromStr) || !string.IsNullOrEmpty(toStr)) parts.Add($"{fromStr}_to_{toStr}");
            if (!string.IsNullOrWhiteSpace(partnerName)) parts.Add($"Partner-{partnerName}");
            if (!string.IsNullOrWhiteSpace(search)) parts.Add($"Search-{search}");
            var middle = parts.Count > 0 ? string.Join("_", parts) : DateTime.Today.ToString("yyyy-MM-dd");
            return $"Documents_{middle}";
        }

        // Remove invalid filename chars, replace whitespace with '-', remove other non-alphanum except '-', limit length to 100
        public static string SanitizeFileName(string name, int maxLen = 100)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Documents";
            // replace invalid path chars with '-'
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in invalid) name = name.Replace(c, '-');
            // normalize whitespace
            name = Regex.Replace(name, @"\s+", "-");
            // remove characters except letters, digits, '-', '_', '.'
            name = Regex.Replace(name, @"[^A-Za-z0-9\-_.]", "");
            if (name.Length > maxLen) name = name.Substring(0, maxLen);
            return name;
        }
    }
}
