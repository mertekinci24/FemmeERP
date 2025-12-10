using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Cash;
using InventoryERP.Application.Cash.DTOs;
using InventoryERP.Presentation.Commands;

namespace InventoryERP.Presentation.ViewModels.Cash
{
    public class CashStatementDialogViewModel : ViewModelBase
    {
        private readonly ICashService _cashService;
        private readonly int _cashAccountId;

        private DateTime? _fromDate;
        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                    _ = LoadDataAsync();
            }
        }

        private DateTime? _toDate;
        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                    _ = LoadDataAsync();
            }
        }

        private string _accountName = string.Empty;
        public string AccountName
        {
            get => _accountName;
            set => SetProperty(ref _accountName, value);
        }

        private decimal _totalIn;
        public decimal TotalIn
        {
            get => _totalIn;
            set => SetProperty(ref _totalIn, value);
        }

        private decimal _totalOut;
        public decimal TotalOut
        {
            get => _totalOut;
            set => SetProperty(ref _totalOut, value);
        }

        private decimal _endingBalance;
        public decimal EndingBalance
        {
            get => _endingBalance;
            set => SetProperty(ref _endingBalance, value);
        }

        public ObservableCollection<CashLedgerDto> Entries { get; } = new();

        public RelayCommand RefreshCmd { get; }

        public CashStatementDialogViewModel(int cashAccountId, string accountName, ICashService cashService)
        {
            _cashAccountId = cashAccountId;
            _accountName = accountName;
            _cashService = cashService;

            // Default to current month
            var now = DateTime.Today;
            _fromDate = new DateTime(now.Year, now.Month, 1);
            _toDate = _fromDate.Value.AddMonths(1).AddDays(-1);

            RefreshCmd = new RelayCommand(async _ => await LoadDataAsync());

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var entries = await _cashService.GetLedgerEntriesAsync(_cashAccountId, FromDate, ToDate);
                
                Entries.Clear();
                decimal runningBalance = 0;

                // If filtering by date, we need the opening balance before the FromDate
                if (FromDate.HasValue)
                {
                    runningBalance = await _cashService.GetBalanceAsync(_cashAccountId, FromDate.Value.AddDays(-1));
                    // Add an "Opening Balance" row? Or just start the running balance?
                    // Let's add a fake row for Opening Balance if it's not zero
                    if (runningBalance != 0)
                    {
                        Entries.Add(new CashLedgerDto
                        {
                            Date = FromDate.Value,
                            Description = "DEVİR (Açılış Bakiyesi)",
                            Balance = runningBalance
                        });
                    }
                }

                decimal totalIn = 0;
                decimal totalOut = 0;

                foreach (var entry in entries)
                {
                    // Recalculate running balance for display purposes (though service might have it, it's safer to calc if we filter)
                    // Wait, service returns balance snapshot. But if we filter, the snapshot is correct for that point in time.
                    // However, if we want to show "Movement" within the period, we should trust the service's Balance field 
                    // BUT verify if it aligns with our "Opening Balance" logic.
                    // Actually, CashLedgerEntry.Balance is the balance AFTER the transaction.
                    // So we can just use it.
                    
                    totalIn += entry.Debit;
                    totalOut += entry.Credit;
                    Entries.Add(entry);
                }

                TotalIn = totalIn;
                TotalOut = totalOut;
                
                // Ending balance is the balance of the last entry, OR if no entries, the opening balance.
                if (entries.Any())
                {
                    EndingBalance = entries.Last().Balance;
                }
                else
                {
                    EndingBalance = runningBalance;
                }
            }
            catch (Exception ex)
            {
                // Handle error (maybe show message box via service if available, or just log)
                System.Diagnostics.Debug.WriteLine($"Error loading cash statement: {ex.Message}");
            }
        }
    }
}
