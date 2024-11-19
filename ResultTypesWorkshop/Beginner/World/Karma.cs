namespace Beginner.World;

static class Karma
{
    const int Level = 8;

    static readonly Random Random = new();

    public static bool IsGood() => Random.Next(10) < Level;

    public static bool Decide() => Random.Next(10) < 5;
}