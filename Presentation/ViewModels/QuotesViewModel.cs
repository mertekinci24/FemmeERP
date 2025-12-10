// ReSharper disable once All
#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Application.Documents.DTOs;
using InventoryERP.Presentation.Commands;
using DocumentRowDto = InventoryERP.Application.Documents.DocumentRowDto;  // R-060: Resolve ambiguity

namespace InventoryERP.Presentation.ViewModels
{
    /// <summary>
    /// R-060: Quotes List ViewModel - Shows only QUOTE documents
    /// </summary>
    public sealed class QuotesViewModel : ViewModelBase
    {
        private readonly IDocumentQueries _queries;
        private readonly InventoryERP.Application.Documents.IDocumentCommandService _cmdSvc;
        private readonly InventoryERP.Application.Products.IProductsReadService _productsSvc;
        private readonly Abstractions.IDialogService _dialogService;

        public ObservableCollection<DocumentRowDto> Rows { get; } = new();
        
        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewQuoteCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCmd { get; }
        public RelayCommand ApproveCommand { get; }

        private string? _searchText;
        public string? SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) _ = RefreshAsync(); } }
        
        public int RowCount => Rows.Count;
        
        private DocumentRowDto? _selected;
        public DocumentRowDto? Selected { get => _selected; set => SetProperty(ref _selected, value); }

        public QuotesViewModel(
            IDocumentQueries queries, 
           InventoryERP.Application.Documents.IDocumentCommandService cmdSvc, 
           InventoryERP.Application.Products.IProductsReadService productsSvc,
            Abstractions.IDialogService dialogService)
        {
            _queries = queries;
            _cmdSvc = cmdSvc;
            _productsSvc = productsSvc;
            _dialogService = dialogService;
            
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            NewQuoteCommand = new RelayCommand(async _ => await NewQuoteAsync());
            EditCommand = new RelayCommand(async _ => await EditSelectedAsync());
            DeleteCmd = new RelayCommand(async _ => await DeleteSelectedAsync());
            ApproveCommand = new RelayCommand(async _ => await ApproveSelectedAsync());

            // Initial load
            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            try
            {
                Rows.Clear();
                
                // Query only QUOTE documents using ListAsync
                var filter = new DocumentListFilter
                {
                    SearchText = SearchText,
                    Type = "QUOTE"  // Filter to QUOTE type only
                };
                
                var res = await _queries.ListAsync(filter, 1, 1000);  // Page 1, get up to 1000 quotes
                
                foreach (var item in res.Items)
                {
                    Rows.Add(item);
                }
                
                OnPropertyChanged(nameof(RowCount));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Teklif listesi yÃ¼klenemedi: {ex.Message}", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task NewQuoteAsync()
        {
            try
            {
                var dto = new DocumentDetailDto
                {
                    Type = "QUOTE",  // R-060: Set document type to QUOTE
                    Number = $"QT-{DateTime.Now:yyyyMMddHHmmss}",
                    Date = DateTime.Today,
                    Currency = "TRY",
                    Lines = new System.Collections.Generic.List<DocumentLineDto>()
                };

                var draftId = await _cmdSvc.CreateDraftAsync(dto);

                // Open DocumentEditDialog using IDialogService
                var result = await _dialogService.ShowDocumentEditDialogAsync(draftId);
                
                if (result)
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Yeni teklif oluÅŸturulamadÄ±: {ex.Message}", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task EditSelectedAsync()
        {
            if (Selected is null) return;

            try
            {
                // Open DocumentEditDialog using IDialogService
                var result = await _dialogService.ShowDocumentEditDialogAsync(Selected.Id);
                
                if (result)
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Teklif dÃ¼zenlenemedi: {ex.Message}", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected is null) return;

            var confirmation = System.Windows.MessageBox.Show(
                $"'{Selected.Number}' numaralÄ± teklifi silmek istediÄŸinize emin misiniz?",
                "Onay",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmation != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                await _cmdSvc.DeleteDraftAsync(Selected.Id);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Teklif silinemedi: {ex.Message}", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task ApproveSelectedAsync()
        {
            if (Selected is null) return;

            try
            {
                await _cmdSvc.ApproveAsync(Selected.Id);
                await RefreshAsync();
                
                System.Windows.MessageBox.Show("Teklif onaylandÄ±.", "BaÅŸarÄ±lÄ±", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Teklif onaylanamadÄ±: {ex.Message}", "Hata", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}




