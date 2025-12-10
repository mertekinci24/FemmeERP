using System;
using System.Threading.Tasks;
using System.Windows;
using InventoryERP.Application.Partners;
using Microsoft.Win32;

namespace InventoryERP.Presentation.Views;

public partial class PartnerStatementDialog : Window
{
    private readonly int _partnerId;
    private readonly IPartnerReadService _readSvc;
    private readonly IPartnerExportService _exportSvc;

    public PartnerStatementDialog(int partnerId, IPartnerReadService readSvc, IPartnerExportService exportSvc)
    {
        InitializeComponent();
        _partnerId = partnerId;
        _readSvc = readSvc;
        _exportSvc = exportSvc;
        RefreshBtn.Click += async (_,__) => await LoadAsync();
        PdfBtn.Click += async (_,__) => await ExportPdfAsync();
        ExcelBtn.Click += async (_,__) => await ExportExcelAsync();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        DateOnly? f = null, t = null;
        if (FromDate.SelectedDate is DateTime fd) f = DateOnly.FromDateTime(fd);
        if (ToDate.SelectedDate is DateTime td) t = DateOnly.FromDateTime(td);
        var dto = await _readSvc.BuildStatementAsync(_partnerId, f, t);
        Grid.ItemsSource = dto.Rows;
        Totals.Text = $"Toplam BorÃ§: {dto.TotalDebit:N2}   Toplam Alacak: {dto.TotalCredit:N2}   Bakiye: {dto.EndingBalance:N2}";
    }

    private async Task ExportPdfAsync()
    {
        DateOnly? f = null, t = null;
        if (FromDate.SelectedDate is DateTime fd) f = DateOnly.FromDateTime(fd);
        if (ToDate.SelectedDate is DateTime td) t = DateOnly.FromDateTime(td);
        var sfd = new SaveFileDialog { Filter = "PDF File (*.pdf)|*.pdf", FileName = $"cari_ekstre_{_partnerId}_{DateTime.UtcNow:yyyyMMdd}.pdf" };
        if (sfd.ShowDialog() != true) return;
        var bytes = await _exportSvc.ExportStatementPdfAsync(_partnerId, f, t, includeClosed: true);
        try
        {
            if (bytes is not null && bytes.Length > 0) await System.IO.File.WriteAllBytesAsync(sfd.FileName, bytes);
            MessageBox.Show("PDF oluÅŸturuldu.");
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF oluÅŸturulurken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        DateOnly? f = null, t = null;
        if (FromDate.SelectedDate is DateTime fd) f = DateOnly.FromDateTime(fd);
        if (ToDate.SelectedDate is DateTime td) t = DateOnly.FromDateTime(td);
        var sfd = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"cari_ekstre_{_partnerId}_{DateTime.UtcNow:yyyyMMdd}.xlsx" };
        if (sfd.ShowDialog() == true)
        {
            try
            {
                var bytes = await _exportSvc.ExportStatementExcelAsync(_partnerId, f, t);
                await System.IO.File.WriteAllBytesAsync(sfd.FileName, bytes);
                MessageBox.Show("Excel oluÅŸturuldu.");
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true }); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel oluÅŸturulurken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


