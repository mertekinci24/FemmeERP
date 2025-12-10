| Feature | Exists in ViewModel? | Exists in XAML? | Status |
| --- | --- | --- | --- |
| Partner: Düzenle | Yes (EditCommand) | Yes (row context menu) | Wired |
| Partner: Sil | Yes (DeleteCommand) | Yes (row context menu) | Wired |
| Partner: Aç/Hareketler (ledger/statement) | Yes (OpenStatementCommand/OpenLedgerCommand) | No | Missing in XAML |
| Partner: Ýçeri Aktar | Yes (ImportFromExcelCommand) | No context menu | Toolbar only |
| Partner: Listeyi Excel’e aktar | Yes (ExportListToExcelCommand) | No context menu (dynamic VM list unused) | Missing |
| Partner: Listeyi PDF’e aktar | Yes (ExportListToPdfCommand) | No context menu (dynamic VM list unused) | Missing |
| Partner: Tek cari Excel/PDF export | No single-partner export implemented | No | Not implemented |
| Partner: Dynamic context menu items (RebuildContextMenuItems) | Yes (MenuItemViewModel list) | Not bound anywhere | Missing/unused |
| Stocks: Düzenle | Yes (EditItemCmd) | Yes (row context menu) | Wired |
| Stocks: Stok Hareketleri | Yes (ShowMovementsCmd) | Yes (row context menu) | Wired |
| Stocks: Sil | Yes (DeleteStockCmd) | Yes (row context menu) | Wired |
| Stocks: Yeni stok | Yes (NewStockCmd) | Toolbar only | No context menu |
| Stocks: Excel export (moves) | Yes (ExportExcelCmd) | No context menu | Missing |
| Stocks: CSV import | Yes (ImportCsvCmd) | Toolbar only | No context menu |
| Stocks: Stok bilgi diyaloðu | Yes (ShowStockInfoCmd) | No | Missing |
| Stocks: Stok düzeltme fiþi (Adjust) | Yes (CreateAdjustmentDocumentCmd) | No | Missing |
| Stocks: Print/Barcode | Not present | No | Not implemented |
