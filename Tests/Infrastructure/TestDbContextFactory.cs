using Microsoft.Data.Sqlite; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore.Diagnostics; 
using Persistence;

namespace Tests.Infrastructure;

public static class TestDbContextFactory
{
  public static (AppDbContext Ctx, SqliteConnection Conn) Create()
  {
    var conn=new SqliteConnection("DataSource=:memory:"); 
    conn.Open();
    
    // R-038: Enable FK constraints in SQLite (required for production parity)
    using (var cmd = conn.CreateCommand())
    {
      cmd.CommandText = "PRAGMA foreign_keys = ON;";
      cmd.ExecuteNonQuery();
    }
    
  var opt=new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(conn)
    .Options;
  var ctx=new AppDbContext(opt);
  // CRITICAL: Use Migrate() instead of EnsureCreated() to match production schema
  ctx.Database.EnsureDeleted();
  ctx.Database.Migrate(); // Apply all migrations (including Coefficient column)
  return (ctx,conn);
  }
}
