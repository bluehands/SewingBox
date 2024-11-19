using FunicularSwitch;

namespace SaveThePrincess.Adventure.Entities;

internal abstract class Hero
{
    public string Skill { get; }
    public string Name { get; }

    protected Hero(string skill, string name)
    {
        Skill = skill;
        Name = name;
    }

    public abstract Result<Loot> KillWithSword(Enemy enemy);
}

internal class SeasonedHero : Hero
{

    public SeasonedHero(string name) : base("seasoned hero", name)
    {
    }

    public override Result<Loot> KillWithSword(Enemy enemy)
    {
        if (enemy is Orc orc)
        {
            return orc.Kill();
        }
        if (enemy is Troll troll)
        {
            return troll.Kill();
        }

        return Result.Error<Loot>($"Aaaahh it's a {enemy.GetType().Name}! Run away!!");
    }
}

internal class YoungHero : Hero
{

    public YoungHero(string name) : base("young hero", name)
    {
    }

    public override Result<Loot> KillWithSword(Enemy enemy)
    {
        if (enemy is Orc orc)
        {
            return orc.Kill();
        }

        return Result.Error<Loot>($"Aaaahh it's a {enemy.GetType().Name}! Run away!!");
    }
}