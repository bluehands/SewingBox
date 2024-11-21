using System.Collections.Immutable;
using FluentAssertions;
using FunicularSwitch;
using FunicularSwitch.Generators;
using Xunit.Abstractions;

namespace Advanced;

public class ResultAggregationTest(ITestOutputHelper testOutputHelper)
{
    public ImmutableList<Hero> Heroes =
    [
        new("Alice", 10, Option.Some(new Sword()), Option.None<Bow>()),
        new("Bob", 20, Option.None<Sword>(), Option.Some(new Bow())),
        new("Charlie", 30, Option.Some(new Sword()), Option.Some(new Bow())),
        new("Richard", 70, Option.None<Sword>(), Option.None<Bow>())
    ];

    [Fact]
    public void AggregateResults()
    {
        var validator = new HeroValidator();

        // Base EXAMPLE - this is how you can validate all heroes which might not fit the use case
        var results =
            Heroes
                .SelectMany(validator.Validate)
                .ToImmutableList();

        // TODO Split the heroes into two lists: valid and invalid
        // TODO Print the valid hero names
        // TODO Print the invalid hero names and the reasons they are invalid
        var validHeroes = results;
        var invalidHeroes = results;

        validHeroes.ForEach(PrintHero(testOutputHelper));
        validHeroes.Should().NotIntersectWith(invalidHeroes);
    }

    private Action<HeroValidationResult<Hero>> PrintHero(ITestOutputHelper @out) =>
        heroResult => heroResult.Match(ok => @out.WriteLine($"OK: {ok}"),
            error => @out.WriteLine(error.ToString()));
}

public class HeroValidator
{
    public ImmutableList<HeroValidationResult<Hero>> Validate(Hero hero) =>
    [
        VerifyAge(hero),
        VerifyEquipment(hero)
    ];

    private HeroValidationResult<Hero> VerifyEquipment(Hero hero)
    {
        return hero.MaybeSword.Match(
            some => HeroValidationResult.Ok(hero),
            () => hero.MaybeBow.Match(
                some => HeroValidationResult.Ok(hero),
                () => HeroValidationResult<Hero>.Error(new HeroValidationError.HeroHasNoSwordOrBow(hero))));
    }

    private static HeroValidationResult<Hero> VerifyAge(Hero hero)
    {
        return hero switch
        {
            { Age: < 18 } => HeroValidationResult<Hero>.Error(new HeroValidationError.HeroIsTooYoung(hero)),
            { Age: > 67 } => HeroValidationResult<Hero>.Error(new HeroValidationError.HeroIsTooOld(hero)),
            _ => HeroValidationResult.Ok(hero)
        };
    }
}

[UnionType]
public abstract partial record HeroValidationError
{
    public record HeroHasNoSwordOrBow(Hero Hero) : HeroValidationError;

    public record HeroIsTooYoung(Hero Hero) : HeroValidationError;

    public record HeroIsTooOld(Hero Hero) : HeroValidationError;
}

[ResultType(errorType: typeof(HeroValidationError))]
public partial class HeroValidationResult<T>;

// public static class ErrorExtensions
// {
//     [MergeError]
//     public static HeroValidationError Merge(this HeroValidationError error, HeroValidationError other)
//     {
//         throw new Exception("TODO implement this");
//     }
// }

public record Hero(string Name, int Age, Option<Sword> MaybeSword, Option<Bow> MaybeBow);

public record Bow;