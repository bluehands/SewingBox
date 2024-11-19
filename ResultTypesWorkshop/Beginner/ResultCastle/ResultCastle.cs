using Beginner.World;

namespace Beginner.ResultCastle;

abstract class Price;
class SleepingPrince : Price
{
    public AwakePrince Awake()
    {
        if (Karma.IsGood())
            return new();

        throw new InvalidOperationException("Cannot wake him up :(");
    }
}

class AwakePrince : Price
{
    public AwakePrince Shave(Razor razor) => this;

    public Picture TakePicture() => new();
}


static class Castle
{
    public static Razor? FindRazor()
    {
        if (Karma.IsGood())
            return new();

        return null;
    }

    public static Price FindPrince()
    {
        if (Karma.IsGood())
            return Karma.Decide() ? new SleepingPrince() : new AwakePrince();

        throw new InvalidOperationException("Where is this f*** prince???");
    }
}

record Razor;