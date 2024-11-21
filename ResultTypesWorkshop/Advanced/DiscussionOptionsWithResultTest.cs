using FunicularSwitch;
using FunicularSwitch.Generators;
using Xunit.Abstractions;

namespace Advanced;

public class OptionsWithResults(ITestOutputHelper testOutputHelper)
{
    private const SwordRepository.State State = SwordRepository.State.SomeoneFellWhenTryingToGetSword;

    [Fact]
    public async Task Test1()
    {
        var swordRepository = new SwordRepository(State);
        testOutputHelper.WriteLine((await swordRepository.GetSword()).ToString());
    }

    [Fact]
    public async Task Test2()
    {
        var swordRepository = new SwordRepository(State);
        testOutputHelper.WriteLine((await swordRepository.TryGetSword()).ToString());
    }

    [Fact]
    public async Task Test3()
    {
        var swordRepository = new SwordRepository(State);
        testOutputHelper.WriteLine((await swordRepository.MaybeGetSword()).ToString());
    }

    [Fact]
    public async Task Test4()
    {
        var swordRepository = new SwordRepository(State);
        testOutputHelper.WriteLine((await swordRepository.GetSwordUnion()).ToString());
    }
}

public record Sword;

[UnionType]
public abstract partial record SwordResult()
{
    public record GotSword(Sword Sword) : SwordResult;

    public record NoSwordsLeft : SwordResult;

    public record SomeoneFellWhenTryingToGetSword : SwordResult;
}

public class SwordRepository(SwordRepository.State state)
{
    public enum State
    {
        SomeoneFellWhenTryingToGetSword,
        GotSword,
        NoSwordsLeft
    }

    public Task<Result<Sword>> GetSword()
    {
        return state switch
        {
            State.NoSwordsLeft => Task.FromResult(Result.Error<Sword>("No swords left")),
            State.GotSword => Task.FromResult(Result.Ok(new Sword())),
            State.SomeoneFellWhenTryingToGetSword => Task.FromResult(Result.Error<Sword>("Someone fell")),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    public Task<Result<Option<Sword>>> TryGetSword()
    {
        return state switch
        {
            State.NoSwordsLeft => Task.FromResult(Result.Ok(Option.None<Sword>())),
            State.GotSword => Task.FromResult(Result.Ok(Option.Some(new Sword()))),
            State.SomeoneFellWhenTryingToGetSword => Task.FromResult(Result.Error<Option<Sword>>("Someone fell")),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    public Task<Option<Result<Sword>>> MaybeGetSword()
    {
        return state switch
        {
            State.NoSwordsLeft => Task.FromResult(Option.None<Result<Sword>>()),
            State.GotSword => Task.FromResult(Option.Some(Result.Ok(new Sword()))),
            State.SomeoneFellWhenTryingToGetSword => Task.FromResult(Option.Some(Result.Error<Sword>("Someone fell"))),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    public Task<SwordResult> GetSwordUnion()
    {
        return state switch
        {
            State.NoSwordsLeft => Task.FromResult<SwordResult>(new SwordResult.NoSwordsLeft()),
            State.GotSword => Task.FromResult<SwordResult>(new SwordResult.GotSword(new Sword())),
            State.SomeoneFellWhenTryingToGetSword => Task.FromResult<SwordResult>(
                new SwordResult.SomeoneFellWhenTryingToGetSword()),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
}