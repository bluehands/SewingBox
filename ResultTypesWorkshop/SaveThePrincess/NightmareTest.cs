using FunicularSwitch;
using SaveThePrincess.Adventure;

namespace SaveThePrincess;

public class NightmareTest
{
    [Fact]
    public void TestCase()
    {
        var fairyTale = new NightmareFairyTaleImpl();

        var r = fairyTale.TellStory();

        r.Match(
            ok => Console.WriteLine($"Fine! Hero '{ok.Hero.Name}' and princess '{ok.Princesses.GetValueOrDefault()?.Name}' and loot '{ok.Loot.Sum(l => l.Value)}'"),
            Console.WriteLine);

        r.Should().BeOk();
    }
}