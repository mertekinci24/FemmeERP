

// ReSharper disable once All
#nullable enable
using System;
using InventoryERP.Application.Common;
using InventoryERP.Presentation.Commands;

namespace InventoryERP.Presentation.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private object? _currentView;
        public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }

        // private readonly Func<StocksViewModel> _stocks;
        private readonly Func<DocumentsViewModel> _docs;
        private readonly Func<PartnersViewModel> _partners;
        private readonly Func<ReportsViewModel> _reports;
        private readonly IThemeService _theme;

        // public RelayCommand NavigateStocks { get; }
        public RelayCommand NavigateDocs { get; }
        public RelayCommand NavigatePartners { get; }
        public RelayCommand NavigateReports { get; }
        public RelayCommand ToggleThemeCmd { get; }

        public MainWindowViewModel(
            /*Func<StocksViewModel> stocks,*/
            Func<DocumentsViewModel> docs,
            Func<PartnersViewModel> partners,
            Func<ReportsViewModel> reports,
            IThemeService theme)
        {
            // _stocks = stocks;
            _docs = docs;
            _partners = partners;
            _reports = reports;
            _theme = theme;
            NavigateDocs = new RelayCommand(_ => CurrentView = _docs());
            NavigatePartners = new RelayCommand(_ => CurrentView = _partners());
            NavigateReports = new RelayCommand(_ => CurrentView = _reports());
            ToggleThemeCmd = new RelayCommand(_ => _theme.Toggle());
            CurrentView = _docs();
        }
    }
}



