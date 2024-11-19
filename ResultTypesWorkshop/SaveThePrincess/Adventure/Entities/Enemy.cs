namespace SaveThePrincess.Adventure.Entities;

internal record Loot(int Value);

internal abstract class Enemy
{
    public bool IsAlive { get; private set; } = true;

    public Loot Kill()
    {
        IsAlive = false;

        return GetLoot();
    }

    protected abstract Loot GetLoot();
}

internal class Orc : Enemy
{
    protected override Loot GetLoot() => new(50);
}

internal class Troll : Enemy
{
    protected override Loot GetLoot() => new(200);
}

internal class Balrog : Enemy
{
    protected override Loot GetLoot() => new(1000);
}