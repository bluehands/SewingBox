using System.Collections.Immutable;
using FunicularSwitch;

namespace SaveThePrincess.Adventure.Entities;

class FairyTaleFactory
{
    static IList<Option<Hero>> GetHeroList() => new[]
    {
        Option.None<Hero>(),
        new YoungHero(PickHeroName()),
        new YoungHero(PickHeroName()),
        new SeasonedHero(PickHeroName())
    };

    static IList<Enemy> GetEnemyList => new Enemy[]
    {
        new Orc(),
        new Orc(),
        new Orc(),
        new Orc(),
        new Orc(),
        new Orc(),
        new Troll(),
        new Troll(),
        new Troll(),
        new Balrog()
    };

    static IList<string> GetHeroNameList() => new[]
    {
        "Olaf",
        "Horst",
        "Siegfired"
    };

    static IList<Option<Princess>> GetPrincessList() => new[]
    {
        Option.None<Princess>(),
        new Princess(PickPrincessName()),
        new Princess(PickPrincessName()),
        new Princess(PickPrincessName()),
        new Princess(PickPrincessName()),
    };

    static IList<string> GetPrincessNameList() => new[]
    {
        "Anna",
        "Elsa",
        "Lea",
    };

    public static Option<Hero> PickHero() => Pick(GetHeroList());


    static string PickHeroName() => Pick(GetHeroNameList());

    public static Castle PickCastle() => new(PickEnemies().ToImmutableList(), PickPrincess());

    static IEnumerable<Enemy> PickEnemies() => PickMany(GetEnemyList, 10);

    static Option<Princess> PickPrincess() => Pick(GetPrincessList());

    static string PickPrincessName() => Pick(GetPrincessNameList());

    static readonly Random Random = new();

    static T Pick<T>(IList<T> list) => list.ElementAt(Random.Next(0, list.Count));

    static IEnumerable<T> PickMany<T>(IList<T> list, int max)
    {
        for (var i = 0; i < Random.Next(0, max); i++)
        {
            yield return Pick(list);
        }
    }
}