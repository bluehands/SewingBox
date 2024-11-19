using SaveThePrincess.Adventure;

namespace SaveThePrincess;

public class LullabyTest
{
    [Fact]
    public void TestCase()
    {
        var fairyTale = new LullabyFairyTaleImpl();

        var r = fairyTale.TellStory();

        r.Match(
            ok => Console.WriteLine($"Fine! Hero '{ok.Hero.Name}' and princess '{ok.Princesses.GetValueOrDefault()?.Name}' and loot '{ok.Loot.Value}'"),
            Console.WriteLine);

        Assert.True(r.IsOk);
    }
}