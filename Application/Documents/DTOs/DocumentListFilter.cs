using System;

namespace InventoryERP.Application.Documents.DTOs
{
    public class DocumentListFilter
    {
        public string? SearchText { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public int? PartnerId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        // Optional sorting
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } // ASC or DESC
        public decimal? TotalMin { get; set; }
        public decimal? TotalMax { get; set; }
    }
}
