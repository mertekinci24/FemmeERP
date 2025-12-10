using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using InventoryERP.Application.Backup;
using InventoryERP.Application.Stocks; // R-032: IInventoryValuationService

namespace InventoryERP.Presentation.ViewModels
{
	public sealed class ReportsViewModel : ViewModelBase
	{
		private readonly IBackupService _backupSvc;
		private readonly IServiceScopeFactory _scopeFactory; // R-032: For scoped services
		public RelayCommand BackupCmd { get; }
		public RelayCommand RestoreCmd { get; }
		public RelayCommand StockValuationReportCmd { get; } // R-032: Stok DeÄŸer Raporu

		public ReportsViewModel(IBackupService backupSvc, IServiceScopeFactory scopeFactory)
		{
			_backupSvc = backupSvc;
			_scopeFactory = scopeFactory;
			BackupCmd = new RelayCommand(async _ => await BackupAsync());
			RestoreCmd = new RelayCommand(async _ => await RestoreAsync());
			StockValuationReportCmd = new RelayCommand(async _ => await GenerateStockValuationReportAsync()); // R-032
		}

		private async Task BackupAsync()
		{
			try
			{
				var sfd = new SaveFileDialog { Filter = "Backup Zip (*.zip)|*.zip", FileName = $"inventory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip" };
				if (sfd.ShowDialog() != true) return;
				// create backup in temp dir then move
				var tmpDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "InventoryERP_Backup");
				var created = await _backupSvc.BackupAsync(tmpDir);
				System.IO.File.Move(created, sfd.FileName, true);
				MessageBox.Show($"Yedek oluÅŸturuldu: {sfd.FileName}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Yedekleme hatasÄ±: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async Task RestoreAsync()
		{
			try
			{
				var ofd = new OpenFileDialog { Filter = "Backup Zip (*.zip)|*.zip" };
				if (ofd.ShowDialog() != true) return;
				await _backupSvc.RestoreAsync(ofd.FileName);
				MessageBox.Show("Geri yÃ¼kleme tamamlandÄ±. LÃ¼tfen uygulamayÄ± yeniden baÅŸlatÄ±n.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Geri yÃ¼kleme hatasÄ±: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// R-032: Generate stock valuation report
		private async Task GenerateStockValuationReportAsync()
		{
			try
			{
				using var scope = _scopeFactory.CreateScope();
				var valuationSvc = scope.ServiceProvider.GetRequiredService<IInventoryValuationService>();
				
				var asOfDate = DateTime.Today;
				var totalValue = await valuationSvc.GetTotalInventoryValueAsync(asOfDate);
				
				MessageBox.Show(
					$"Stok DeÄŸer Raporu\n" +
					$"Tarih: {asOfDate:dd.MM.yyyy}\n" +
					$"Toplam Stok DeÄŸeri: {totalValue:N2} TRY\n\n" +
					$"(Hesaplama yÃ¶ntemi: Hareketli AÄŸÄ±rlÄ±klÄ± Ortalama - MWA)",
					"Stok DeÄŸer Raporu",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Stok deÄŸer raporu oluÅŸturulamadÄ±: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}



