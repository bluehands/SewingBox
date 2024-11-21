using Microsoft.Extensions.Logging;
using SaveThePrincess.Adventure;
using Xunit.Abstractions;

namespace SaveThePrincess;

public class LullabyTest
{
    private readonly ITestOutputHelper testOutputHelper;

    public LullabyTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestCase()
    {
        var logger =
            new Meziantou.Extensions.Logging.Xunit.XUnitLogger(this.testOutputHelper, new LoggerExternalScopeProvider(),
                null);
        var fairyTale = new LullabyFairyTaleImpl(logger);

        var r = fairyTale.TellStory();

        r.Match(
            ok => Console.WriteLine($"Fine! Hero '{ok.Hero.Name}' and princess '{ok.Princesses.GetValueOrDefault()?.Name}' and loot '{ok.Loot.Sum(l => l.Value)}'"),
            Console.WriteLine);

        Assert.True(r.IsOk);
    }
}