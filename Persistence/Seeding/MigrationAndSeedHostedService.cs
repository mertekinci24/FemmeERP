using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Persistence.Seeding;

public sealed class MigrationAndSeedHostedService(IServiceProvider sp) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);           // güvence
        await DatabaseSeeder.SeedAsync(db, ct);
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
