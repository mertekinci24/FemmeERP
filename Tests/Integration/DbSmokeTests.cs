using FluentAssertions; using Tests.Infrastructure;
namespace Tests.Integration;
public class DbSmokeTests:BaseIntegrationTest
{
  [Fact] public void CanConnect()=>Ctx.Database.CanConnect().Should().BeTrue();
}
