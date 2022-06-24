using System.Collections.Immutable;
using System.Diagnostics;

namespace Demo;

[FunicularSwitch.Generators.UnionType]
public abstract class Error
{
    public static Error NotFound(string message) => new NotFound_(message);
    public static Error Failure(Exception exception, string message) => new Failure_(exception, message);
    public static Error InvalidInput(string message) => new InvalidInput_(message);
    public static Error Aggregated(ImmutableList<Error> errors) => new Aggregated_(errors);

    public string Message { get; }

    public class NotFound_ : Error
    {
        public NotFound_(string message) : base(UnionCases.NotFound, message)
        {
        }
    }

    public class Failure_ : Error
    {
        public Exception Exception { get; }

        public Failure_(Exception exception, string message) : base(UnionCases.Failure, message)
        {
            Exception = exception;
        }
    }

    public class InvalidInput_ : Error
    {
        public InvalidInput_(string message) : base(UnionCases.InvalidInput, message)
        {
        }
    }

    public class Aggregated_ : Error
    {
        public ImmutableList<Error> Errors { get; }

        public Aggregated_(ImmutableList<Error> errors) : base(UnionCases.Aggregated, string.Join(Environment.NewLine, errors.Select(e => e.Message))) => Errors = errors;
    }

    internal enum UnionCases
    {
        NotFound,
        Failure,
        InvalidInput,
        Aggregated
    }

    internal UnionCases UnionCase { get; }
    Error(UnionCases unionCase, string message)
    {
        UnionCase = unionCase;
        Message = message;
    }

    public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
    bool Equals(Error other) => UnionCase == other.UnionCase;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Error)obj);
    }

    public override int GetHashCode() => (int)UnionCase;
}