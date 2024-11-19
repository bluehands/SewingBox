using System.Collections.Immutable;
using SaveThePrincess.Adventure.Entities;

namespace SaveThePrincess.Adventure.FairyTales;

internal abstract class NightmareFairyTale
{
    protected Hero CallForAHero() =>
        FairyTaleFactory.PickHero().Match(
            hero =>
            {
                Console.WriteLine($"Once upon a time there was a {hero.Skill} named {hero.Name}.");
                return hero;
            },
            () => throw new Exception("Once upon a time there was no hero to find to save the princess ..."));

    protected Castle TravelToCastle(Hero hero)
    {
        var castle = FairyTaleFactory.PickCastle();
        Console.WriteLine($"{hero.Name} begins his adventure to travel to a faraway castle to rescue the princess from evil monsters.");
        return castle;
    }

    protected ImmutableList<Enemy> EnterCastle(Hero hero, Castle castle)
    {
        Console.WriteLine($"When {hero.Name} tried to enter the castle, he was confronted by {castle.Enemies.Count} enemies");
        return castle.Enemies;
    }

    protected Loot DefeatEnemies(Hero hero, ImmutableList<Enemy> enemies) =>
        enemies.Any()
            ? enemies.Select(e => DefeatEnemy(hero, e)).Aggregate(GatheringLoot)
            : new Loot(0);

    Loot DefeatEnemy(Hero hero, Enemy enemy)
    {
        Console.WriteLine($"{hero.Name} is fighting against {enemy.GetType().Name}");

        return hero.KillWithSword(enemy).Match(l =>
        {
            Console.WriteLine($"{enemy.GetType().Name} was defeated and dropped {l.Value}");
            return l;
        }, error => throw new Exception(error));
    }

    static Loot GatheringLoot(Loot a, Loot b) => new(a.Value + b.Value);

    protected Princess? FreeThePrincess(Hero hero, Castle castle)
    {
        if (castle.HasEnemies)
            throw new Exception($"Hero {hero.Name} cannot free the princess, there are still enemies in the castle!");

        return castle.Princess.GetValueOrDefault();
    }

    protected FairyTaleResult TravelingHome(Hero hero, Princess? princess, Loot loot)
    {
        if (princess != null)
        {
            Console.WriteLine(
                $"Hero {hero.Name} found princess {princess.Name} in the castle, they traveled home and together they lived happily ever after.");
            return new FairyTaleResult(hero, princess, loot);
        }

        Console.WriteLine(
            $"Hero {hero.Name} didn't find a princess in the castle but he earned a shitload of many ({loot.Value}).");
        return new FairyTaleResult(hero, null, loot);
    }
}