using System;

namespace InventoryERP.Application.Documents.DTOs
{
    public class DocumentRowDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Number { get; set; } = "";
        public DateTime Date { get; set; }
        public string PartnerTitle { get; set; } = "";
        public string Status { get; set; } = "";
        public string Currency { get; set; } = "TRY";
        public decimal TotalNet { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalGross { get; set; }
    }
}
