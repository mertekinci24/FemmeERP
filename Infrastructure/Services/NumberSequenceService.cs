using System;
using System.Data;
using System.Threading.Tasks;
using InventoryERP.Application.Documents;
using InventoryERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // Added for GetDbTransaction
using Persistence;

namespace InventoryERP.Infrastructure.Services
{
    public class NumberSequenceService : INumberSequenceService
    {
        private readonly AppDbContext _db;

        public NumberSequenceService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextNumberAsync(string documentType)
        {
            var prefix = GetPrefix(documentType);
            var year = DateTime.UtcNow.Year;
            
            int nextVal = 0;
            int retry = 0;
            const int maxRetries = 5;

            while (true)
            {
                try
                {
                    nextVal = await GetNextSequenceValueAsync(documentType, year);
                    break;
                }
                catch (Exception ex)
                {
                    if (IsTransientDbLockException(ex))
                    {
                        retry++;
                        if (retry >= maxRetries) throw;
                        await Task.Delay(50 * retry);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Format: PREFIX + NextVal.ToString("D5") + "_" + Date (e.g., SO00001_20251130)
            return $"{prefix}{nextVal:D5}_{DateTime.Today:yyyyMMdd}";
        }

        private async Task<int> GetNextSequenceValueAsync(string type, int year)
        {
            var connection = _db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var currentTx = _db.Database.CurrentTransaction;
            if (currentTx != null)
            {
                return await ExecuteAtomicUpdateAsync(connection, currentTx.GetDbTransaction(), type, year);
            }
            else
            {
                using var tx = await connection.BeginTransactionAsync();
                try 
                {
                    var val = await ExecuteAtomicUpdateAsync(connection, tx, type, year);
                    await tx.CommitAsync();
                    return val;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
        }

        private async Task<int> ExecuteAtomicUpdateAsync(System.Data.Common.DbConnection conn, System.Data.Common.DbTransaction tx, string type, int year)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            
            // Step 1: Upsert (Ensure row exists)
            cmd.CommandText = @"
                INSERT INTO DocumentSequences (DocumentType, Year, CurrentValue, CreatedAt, Version, IsDeleted)
                SELECT @type, @year, 0, @now, 1, 0
                WHERE NOT EXISTS (SELECT 1 FROM DocumentSequences WHERE DocumentType = @type AND Year = @year);
            ";
            AddParam(cmd, "@type", type);
            AddParam(cmd, "@year", year);
            AddParam(cmd, "@now", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();

            // Step 2: Atomic Increment + Returning
            cmd.CommandText = @"
                UPDATE DocumentSequences
                SET CurrentValue = CurrentValue + 1, ModifiedAt = @now
                WHERE DocumentType = @type AND Year = @year
                RETURNING CurrentValue;
            ";
            // Parameters are already added and can be reused
            
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                 throw new InvalidOperationException("Failed to generate sequence number.");
            }
            return Convert.ToInt32(result);
        }

        private void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
        {
            if (!cmd.Parameters.Contains(name))
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                cmd.Parameters.Add(p);
            }
        }

        private bool IsTransientDbLockException(Exception ex)
        {
            var msg = ex.Message.ToLowerInvariant();
            return msg.Contains("database is locked") || msg.Contains("busy");
        }

        private string GetPrefix(string documentType)
        {
            return documentType.ToUpperInvariant() switch
            {
                "SALES_ORDER" => "SO",
                "SEVK_IRSALIYESI" => "IR",
                "SALES_INVOICE" => "SI",
                "PURCHASE_INVOICE" => "PI",
                "QUOTE" => "QU",
                "RECEIPT" => "RC",
                "PAYMENT" => "PY",
                "TRANSFER_FISI" => "TR",
                "SAYIM_FISI" => "CNT",
                "URETIM_FISI" => "MFG",
                "ADJUSTMENT_IN" => "ADJ",
                "ADJUSTMENT_OUT" => "ADJ",
                _ => "DOC"
            };
        }
    }
}
