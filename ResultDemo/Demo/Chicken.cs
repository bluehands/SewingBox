using FunicularSwitch.Generators;

namespace Demo;

[UnionType]
public abstract record Chicken(string Name);

public record Hen(string Name) : Chicken(Name);
public record Rooster(string Name) : Chicken(Name);

public static class ChickenExtension
{
    public static bool CanIExpectEggs(this IEnumerable<Chicken> chickens) => chickens
        .Any(c => c.Match(
        hen => true,
        rooster => false
            )
    );
}