using System.Collections.Immutable;
using FunicularSwitch;
using Microsoft.Extensions.Logging;
using SaveThePrincess.Adventure.Entities;

namespace SaveThePrincess.Adventure.FairyTales;

internal abstract class LullabyFairyTale(ILogger logger)
{
    protected Result<Hero> CallForAHero() =>
        FairyTaleFactory.PickHero().Match(
            hero =>
            {
                logger.LogInformation($"Once upon a time there was a {hero.Skill} named {hero.Name}.");
                return hero;
            },
            () => Result.Error<Hero>("Once upon a time there was no hero to find to save the princess ..."));

    protected Castle TravelToCastle(Hero hero)
    {
        var castle = FairyTaleFactory.PickCastle();
        logger.LogInformation($"{hero.Name} begins his adventure to travel to a faraway castle to rescue the princess from evil monsters.");
        return castle;
    }

    protected Result<ImmutableList<Enemy>> EnterCastle(Hero hero, Castle castle)
    {
        logger.LogInformation($"When {hero.Name} tried to enter the castle, he was confronted by {castle.Enemies.Count} enemies");
        return castle.Enemies;
    }

    protected Result<IReadOnlyCollection<Loot>> DefeatEnemies(Hero hero, ImmutableList<Enemy> enemies) =>
        enemies.Aggregate(
            Result.Ok<ImmutableList<Loot>>([]),
            (current, enemy) => 
                current
                    .Bind(loot => 
                        DefeatEnemy(hero, enemy)
                            .Map(loot.Add)))
            .Map<IReadOnlyCollection<Loot>>(l => l);

    Result<Loot> DefeatEnemy(Hero hero, Enemy enemy)
    {
        logger.LogInformation($"{hero.Name} is fighting against {enemy.GetType().Name}");

        return hero.KillWithSword(enemy).Map(l =>
        {
            logger.LogInformation($"{enemy.GetType().Name} was defeated and dropped {l.Value}");
            return l;
        });
    }

    protected Result<Option<Princess>> FreeThePrincess(Hero hero, Castle castle)
    {
        if (castle.HasEnemies)
            return Result.Error<Option<Princess>>($"Hero {hero.Name} cannot free the princess, there are still enemies in the castle!");

        return castle.Princess;
    }

    protected Result<FairyTaleResult> TravelingHome(Hero hero, Option<Princess> princess, IReadOnlyCollection<Loot> loot) =>
        princess.Match(p =>
        {
            logger.LogInformation($"Hero {hero.Name} found princess {p.Name} in the castle, they traveled home and together they lived happily ever after.");
            return new FairyTaleResult(hero, princess, loot);
        }, () =>
        {
            logger.LogInformation(
                $"Hero {hero.Name} didn't find a princess in the castle but he earned a shitload of looot ({loot.Sum(l => l.Value)}).");
            return new FairyTaleResult(hero, princess, loot);
        });
}