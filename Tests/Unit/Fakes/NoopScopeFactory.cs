using System;
using Microsoft.Extensions.DependencyInjection;

namespace Fakes;

public sealed class NoopScopeFactory : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new NoopScope();

    private sealed class NoopScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
        public void Dispose() { }
    }
}
