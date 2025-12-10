using Microsoft.Data.Sqlite; using Persistence;
namespace Tests.Infrastructure;
public abstract class BaseIntegrationTest:IDisposable
{
  protected AppDbContext Ctx{get;} protected readonly SqliteConnection Conn;
  protected BaseIntegrationTest(){ (Ctx,Conn)=TestDbContextFactory.Create(); }
  public void Dispose(){ Ctx.Dispose(); Conn.Close(); Conn.Dispose(); }
}
