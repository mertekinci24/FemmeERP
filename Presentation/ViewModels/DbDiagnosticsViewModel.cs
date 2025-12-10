using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

namespace InventoryERP.Presentation.ViewModels
{
    public class DbDiagnosticsViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _provider;
        public string ConnectionString { get; private set; } = string.Empty;
        public string DatabasePath { get; private set; } = string.Empty;
        public string Status { get; private set; } = string.Empty;
        public ObservableCollection<string> AppliedMigrations { get; } = new();
        public ObservableCollection<string> PendingMigrations { get; } = new();
        public ObservableCollection<string> PartnerColumns { get; } = new();
    public ObservableCollection<string> DiscoveredMigrations { get; } = new();

        public ICommand ApplyMigrationsCmd { get; }
        public ICommand OpenFolderCmd { get; }

        public DbDiagnosticsViewModel(IServiceProvider provider)
        {
            _provider = provider;
            ApplyMigrationsCmd = new RelayCommand(_ => ApplyMigrations());
            OpenFolderCmd = new RelayCommand(_ => OpenFolder());
            // initial load
            LoadAsync();
        }

        private async void LoadAsync()
        {
            try
            {
                var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Connection string and DB path
                var cs = db.Database.GetDbConnection().ConnectionString;
                ConnectionString = cs;
                OnPropertyChanged(nameof(ConnectionString));

                DatabasePath = ExtractDataSourcePath(cs);
                OnPropertyChanged(nameof(DatabasePath));

                // Migrations
                var applied = await db.Database.GetAppliedMigrationsAsync();
                var pending = await db.Database.GetPendingMigrationsAsync();
                AppliedMigrations.Clear(); foreach (var m in applied) AppliedMigrations.Add(m);
                PendingMigrations.Clear(); foreach (var m in pending) PendingMigrations.Add(m);

                // Discovered (compiled) migrations from assembly
                DiscoveredMigrations.Clear();
                var migAsm = db.GetService<IMigrationsAssembly>();
                foreach (var kv in migAsm.Migrations)
                {
                    DiscoveredMigrations.Add(kv.Key);
                }

                // Partner schema columns via PRAGMA
                PartnerColumns.Clear();
                var conn = db.Database.GetDbConnection();
                await conn.OpenAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA table_info('Partner')";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var name = reader.GetString(reader.GetOrdinal("name"));
                        var type = reader.GetString(reader.GetOrdinal("type"));
                        PartnerColumns.Add($"{name} ({type})");
                    }
                }
                finally
                {
                    await conn.CloseAsync();
                }

                // Status summary
                var exists = !string.IsNullOrWhiteSpace(DatabasePath) && File.Exists(DatabasePath);
                var size = exists ? new FileInfo(DatabasePath).Length : 0L;
                Status = $"DB Exists: {exists}, Size: {Math.Round(size/1024.0, 2)} KB, Applied: {AppliedMigrations.Count}, Pending: {PendingMigrations.Count}";
                OnPropertyChanged(nameof(Status));
            }
            catch (Exception ex)
            {
                Status = "Hata: " + ex.Message;
                OnPropertyChanged(nameof(Status));
            }
        }

        private void ApplyMigrations()
        {
            try
            {
                var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
                LoadAsync();
            }
            catch (Exception ex)
            {
                Status = "Migration HatasÄ±: " + ex.Message;
                OnPropertyChanged(nameof(Status));
            }
        }

        private void OpenFolder()
        {
            try
            {
                var path = string.IsNullOrWhiteSpace(DatabasePath) ? null : Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            catch { /* ignore */ }
        }

        private static string ExtractDataSourcePath(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;
            // naive parse for Data Source=...
            var key = "Data Source=";
            var idx = connectionString.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var rest = connectionString.Substring(idx + key.Length);
                var semi = rest.IndexOf(';');
                return semi >= 0 ? rest.Substring(0, semi) : rest;
            }
            return connectionString;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
