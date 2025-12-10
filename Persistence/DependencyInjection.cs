using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence;
public static class DependencyInjection
{
  public static IServiceCollection AddPersistence(this IServiceCollection s, IConfiguration cfg)
  {
    // Use a single, absolute DB path: %LOCALAPPDATA%/InventoryERP/inventory.db
    var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var dbDir = Path.Combine(root, "InventoryERP");
    Directory.CreateDirectory(dbDir);
    var dbPath = Path.Combine(dbDir, "inventory.db");
    var cs = $"Data Source={dbPath}";
    s.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));
  s.AddScoped<InventoryERP.Domain.Interfaces.IInventoryQueries, InventoryERP.Persistence.Services.InventoryQueriesEf>();
  s.AddScoped<InventoryERP.Persistence.Services.Ui.IProductsReadService, InventoryERP.Persistence.Services.Ui.ProductsReadService>();
    return s;
  }
}
