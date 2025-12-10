namespace InventoryERP.Application.Documents.DTOs
{
    public class DocumentLineDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public decimal Qty { get; set; }
        public string Uom { get; set; } = "";
    public decimal Coefficient { get; set; } = 1m;
        public decimal UnitPrice { get; set; }
        public int VatRate { get; set; }
        public decimal LineNet { get; set; }
        public decimal LineVat { get; set; }
        public decimal LineGross { get; set; }
            public int? ProductVariantId { get; set; }
            public int? LotId { get; set; }
            public string? LotNumber { get; set; }
            public System.DateTime? ExpiryDate { get; set; }
            // R-013: Multi-warehouse transfer support
            public int? SourceLocationId { get; set; }
            public int? DestinationLocationId { get; set; }
    }
}
