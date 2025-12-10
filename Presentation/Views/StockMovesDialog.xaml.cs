using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using InventoryERP.Application.Stocks;
using Microsoft.Win32;

namespace InventoryERP.Presentation.Views;

public partial class StockMovesDialog : Window
{
    private readonly int _productId;
    private readonly IStockQueries _queries;
    public StockMovesDialog(int productId, IStockQueries queries)
    {
        InitializeComponent();
        _productId = productId;
        _queries = queries;
        RefreshBtn.Click += async (_,__) => await LoadAsync();
        ExportBtn.Click += async (_,__) => await ExportAsync();
        CloseBtn.Click += (_,__) => Close();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        DateOnly? f = null, t = null;
        if (FromDate.SelectedDate is DateTime fd) f = DateOnly.FromDateTime(fd);
        if (ToDate.SelectedDate is DateTime td) t = DateOnly.FromDateTime(td);
        var rows = await _queries.ListMovesAsync(_productId, f, t);
        Grid.ItemsSource = rows;
    }

    private async Task ExportAsync()
    {
        var sfd = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"StockMoves_{_productId}.xlsx" };
        if (sfd.ShowDialog() != true) return;
        DateOnly? f = null, t = null;
        if (FromDate.SelectedDate is DateTime fd) f = DateOnly.FromDateTime(fd);
        if (ToDate.SelectedDate is DateTime td) t = DateOnly.FromDateTime(td);
        // we don't have direct export service here; use queries to get rows and write simple workbook
        var rows = await _queries.ListMovesAsync(_productId, f, t);
        // create excel
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Hareketler");

        ws.Cell(1,1).Value = "Tarih"; 
        ws.Cell(1,2).Value = "Belge Türü"; 
        ws.Cell(1,3).Value = "Belge No"; 
        ws.Cell(1,4).Value = "Cari"; // R-280: Added Partner Column
        ws.Cell(1,5).Value = "Miktar"; 
        ws.Cell(1,6).Value = "Birim Maliyet"; 
        ws.Cell(1,7).Value = "Açıklama";
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r,1).Value = x.Date;
            ws.Cell(r,2).Value = x.DocType;
            ws.Cell(r,3).Value = x.DocNo;
            ws.Cell(r,4).Value = x.PartnerName; // R-280: Map Partner
            ws.Cell(r,5).Value = x.Qty;
            ws.Cell(r,6).Value = x.UnitCost;
            ws.Cell(r,7).Value = x.Ref;
            r++;
        }
        using var ms = new System.IO.MemoryStream();
        wb.SaveAs(ms);
        await System.IO.File.WriteAllBytesAsync(sfd.FileName, ms.ToArray());
        MessageBox.Show("Excel oluÅŸturuldu.");
    }
}


