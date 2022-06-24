using System.Collections.Immutable;
using FunicularSwitch.Generators;

namespace Demo;

class Program
{
    public static async Task<int> Main(string name, string name2)
    {
        var chickens1 = GetPersonWithChickens(name);
        var chickens2 = GetPersonWithChickens(name2);

        var combined = chickens1.Aggregate(chickens2, (t1, t2) => (person1: t1.person, person2: t2.person, chickens: t1.chickens.Concat(t2.chickens)));

        var message = combined
            .Map(t => 
                $"{t.person1.Name} and {t.person2} live happily with {string.Join(", ", t.chickens.Select(c => c.Name))}")
            .Match(m => m, PrintError);

        Console.WriteLine(message);

        return 0;
    }

    static Result<(Person person, IReadOnlyCollection<Chicken> chickens)> GetPersonWithChickens(string name)
    {
        return MyRepo.GetByName(name)
            .Bind(person => MyRepo.GetChickens(person)
                .Map(chickens => (person, chickens)));
    }

    static string PrintError(Error error) =>
        error
            .Match(
                notFound => $"Not found: {notFound.Message}",
                failure => $"Ups, something went wrong: {failure.Message} - {failure.Exception}",
                invalidInput => $"Name was invalid: {invalidInput.Message}",
                aggregated => $"Oh multiple errors: {aggregated.Message}"
            );
}

public abstract partial class Result
{
    public static Result<T> NotFound<T>(string message) => Error<T>(Demo.Error.NotFound(message));
    public static Result<T> Failure<T>(Exception exception, string message) => Error<T>(Demo.Error.Failure(exception, message));
    public static Result<T> InvalidInput<T>(string message) => Error<T>(Demo.Error.InvalidInput(message));
}

[ResultType(errorType: typeof(Error))]
public abstract partial class Result<T>
{
}

[ResultType(errorType: typeof(int))]
public abstract partial class DomainResult<T>
{
}

public static class ErrorMergeExtension
{
    [MergeError]
    public static Error Merge(this Error error, Error other) => Error.Aggregated(ImmutableList.Create(error, other));
}

public static class MyRepo
{
    public static Result<Person> GetByName(string name) => name switch
    {
        "nobody" => Result.NotFound<Person>($"{name} not in db"),
        "evil" => Result.Failure<Person>(new("Database failure"), "Database broken"),
        _ => string.IsNullOrWhiteSpace(name)
            ? Result.InvalidInput<Person>("name is empty")
            : name.Contains("DROP TABLE")
                ? Result.InvalidInput<Person>("malicious name") 
                : new Person(name, true)
    };

    public static Result<IReadOnlyCollection<Chicken>> GetChickens(Person person) => ImmutableArray.Create<Chicken>(new Hen("Annette"), new Hen("Mascha"));
}

public record Person(string Name, bool LovesChickens);