using System;
using System.Collections.Generic;

namespace InventoryERP.Application.Documents.DTOs
{
    public class DocumentDetailDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Number { get; set; } = "";
        public DateTime Date { get; set; }
        public int? PartnerId { get; set; }
    public int? CashAccountId { get; set; }
    public int? SourceWarehouseId { get; set; }
    public int? DestinationWarehouseId { get; set; }
        public string PartnerTitle { get; set; } = "";
        public string PartnerAddress { get; set; } = "";
        public string PartnerTaxId { get; set; } = "";
        public string PartnerTaxOffice { get; set; } = "";
        public string Status { get; set; } = "";
        public string Currency { get; set; } = "TRY";
        public string? Description { get; set; } // R-246: Added for invoice notes
        public List<DocumentLineDto> Lines { get; set; } = new();
        public decimal TotalNet { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalGross { get; set; }
    }
}
