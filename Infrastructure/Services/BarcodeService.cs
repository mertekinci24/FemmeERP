using System;
using System.Text;
using System.Threading.Tasks;
using InventoryERP.Application.Products;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace InventoryERP.Infrastructure.Services
{
    public class BarcodeService : IBarcodeService
    {
        private readonly AppDbContext _db;
        private const string BarcodePrefix = "69";
        private const int BarcodeRandomDigits = 10;
        private const int BarcodeMaxGenerationAttempts = 6;

        public BarcodeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateUniqueBarcodeAsync()
        {
            for (int i = 0; i < BarcodeMaxGenerationAttempts; i++)
            {
                var sb = new StringBuilder();
                sb.Append(BarcodePrefix);
                for (int j = 0; j < BarcodeRandomDigits; j++)
                {
                    sb.Append(Random.Shared.Next(0, 10));
                }
                var candidate = sb.ToString();

                var exists = await _db.Products.AnyAsync(p => p.Barcode == candidate);
                if (!exists)
                {
                    return candidate;
                }
            }

            throw new Exception("Barkod üretilemedi. Maksimum deneme sayısına ulaşıldı.");
        }
    }
}
