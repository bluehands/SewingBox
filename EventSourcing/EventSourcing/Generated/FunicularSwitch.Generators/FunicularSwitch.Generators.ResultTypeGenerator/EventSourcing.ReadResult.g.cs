#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FunicularSwitch;

namespace EventSourcing
{
#pragma warning disable 1591
    public abstract partial class ReadResult
    {
        public static ReadResult<T> Error<T>(ReadFailure details) => new ReadResult<T>.Error_(details);
        public static ReadResult<T> Ok<T>(T value) => new ReadResult<T>.Ok_(value);
        public bool IsError => GetType().GetGenericTypeDefinition() == typeof(ReadResult<>.Error_);
        public bool IsOk => !IsError;
        public abstract ReadFailure? GetErrorOrDefault();

        public static ReadResult<T> Try<T>(Func<T> action, Func<Exception, ReadFailure> formatError)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return Error<T>(formatError(e));
            }
        }

        public static async Task<ReadResult<T>> Try<T>(Func<Task<T>> action, Func<Exception, ReadFailure> formatError)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                return Error<T>(formatError(e));
            }
        }
    }

    public abstract partial class ReadResult<T> : ReadResult, IEnumerable<T>
    {
        public static ReadResult<T> Error(ReadFailure message) => Error<T>(message);
        public static ReadResult<T> Ok(T value) => Ok<T>(value);

        public static implicit operator ReadResult<T>(T value) => ReadResult.Ok(value);

        public static bool operator true(ReadResult<T> result) => result.IsOk;
        public static bool operator false(ReadResult<T> result) => result.IsError;

        public static bool operator !(ReadResult<T> result) => result.IsError;

        //just here to suppress warning, never called because all subtypes (Ok_, Error_) implement Equals and GetHashCode
        bool Equals(ReadResult<T> other) => this switch
        {
            Ok_ ok => ok.Equals((object)other),
            Error_ error => error.Equals((object)other),
            _ => throw new InvalidOperationException($"Unexpected type derived from {nameof(ReadResult<T>)}")
        };

        public override int GetHashCode() => this switch
        {
            Ok_ ok => ok.GetHashCode(),
            Error_ error => error.GetHashCode(),
            _ => throw new InvalidOperationException($"Unexpected type derived from {nameof(ReadResult<T>)}")
        };

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ReadResult<T>)obj);
        }

        public static bool operator ==(ReadResult<T>? left, ReadResult<T>? right) => Equals(left, right);

        public static bool operator !=(ReadResult<T>? left, ReadResult<T>? right) => !Equals(left, right);

        public void Match(Action<T> ok, Action<ReadFailure>? error = null) => Match(
            v =>
            {
                ok.Invoke(v);
                return 42;
            },
            err =>
            {
                error?.Invoke(err);
                return 42;
            });

        public T1 Match<T1>(Func<T, T1> ok, Func<ReadFailure, T1> error)
        {
            return this switch
            {
                Ok_ okReadResult => ok(okReadResult.Value),
                Error_ errorReadResult => error(errorReadResult.Details),
                _ => throw new InvalidOperationException($"Unexpected derived result type: {GetType()}")
            };
        }

        public async Task<T1> Match<T1>(Func<T, Task<T1>> ok, Func<ReadFailure, Task<T1>> error)
        {
            return this switch
            {
                Ok_ okReadResult => await ok(okReadResult.Value).ConfigureAwait(false),
                Error_ errorReadResult => await error(errorReadResult.Details).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unexpected derived result type: {GetType()}")
            };
        }

        public Task<T1> Match<T1>(Func<T, Task<T1>> ok, Func<ReadFailure, T1> error) =>
            Match(ok, e => Task.FromResult(error(e)));

        public async Task Match(Func<T, Task> ok)
        {
            if (this is Ok_ okReadResult) await ok(okReadResult.Value).ConfigureAwait(false);
        }

        public T Match(Func<ReadFailure, T> error) => Match(v => v, error);

        public ReadResult<T1> Bind<T1>(Func<T, ReadResult<T1>> bind)
        {
            switch (this)
            {
                case Ok_ ok:
	                try
	                {
		                return bind(ok.Value);
	                }
	                // ReSharper disable once RedundantCatchClause
#pragma warning disable CS0168 // Variable is declared but never used
	                catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
	                {
		                throw; //createGenericErrorResult
	                }
                case Error_ error:
                    return error.Convert<T1>();
                default:
                    throw new InvalidOperationException($"Unexpected derived result type: {GetType()}");
            }
        }

        public async Task<ReadResult<T1>> Bind<T1>(Func<T, Task<ReadResult<T1>>> bind)
        {
            switch (this)
            {
                case Ok_ ok:
	                try
	                {
		                return await bind(ok.Value).ConfigureAwait(false);
	                }
	                // ReSharper disable once RedundantCatchClause
#pragma warning disable CS0168 // Variable is declared but never used
	                catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
	                {
		                throw; //createGenericErrorResult
	                }
                case Error_ error:
                    return error.Convert<T1>();
                default:
                    throw new InvalidOperationException($"Unexpected derived result type: {GetType()}");
            }
        }

        public ReadResult<T1> Map<T1>(Func<T, T1> map)
            => Bind(value => Ok(map(value)));

        public Task<ReadResult<T1>> Map<T1>(Func<T, Task<T1>> map)
            => Bind(async value => Ok(await map(value).ConfigureAwait(false)));

        public T? GetValueOrDefault()
	        => Match(
		        v => (T?)v,
		        _ => default
	        );

        public T GetValueOrDefault(Func<T> defaultValue)
	        => Match(
		        v => v,
		        _ => defaultValue()
	        );

        public T GetValueOrDefault(T defaultValue)
	        => Match(
		        v => v,
		        _ => defaultValue
	        );

        public T GetValueOrThrow()
            => Match(
                v => v,
                details => throw new InvalidOperationException($"Cannot access error result value. Error: {details}"));

        public IEnumerator<T> GetEnumerator() => Match(ok => new[] { ok }, _ => Enumerable.Empty<T>()).GetEnumerator();

        public override string ToString() => Match(ok => $"Ok {ok?.ToString()}", error => $"Error {error}");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public sealed partial class Ok_ : ReadResult<T>
        {
            public T Value { get; }

            public Ok_(T value) => Value = value;

            public override ReadFailure? GetErrorOrDefault() => null;

            public bool Equals(Ok_? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return EqualityComparer<T>.Default.Equals(Value, other.Value);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Ok_ other && Equals(other);
            }

            public override int GetHashCode() => Value == null ? 0 : EqualityComparer<T>.Default.GetHashCode(Value);

            public static bool operator ==(Ok_ left, Ok_ right) => Equals(left, right);

            public static bool operator !=(Ok_ left, Ok_ right) => !Equals(left, right);
        }

        public sealed partial class Error_ : ReadResult<T>
        {
            public ReadFailure Details { get; }

            public Error_(ReadFailure details) => Details = details;

            public ReadResult<T1>.Error_ Convert<T1>() => new ReadResult<T1>.Error_(Details);

            public override ReadFailure? GetErrorOrDefault() => Details;

            public bool Equals(Error_? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Details, other.Details);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Error_ other && Equals(other);
            }

            public override int GetHashCode() => Details.GetHashCode();

            public static bool operator ==(Error_ left, Error_ right) => Equals(left, right);

            public static bool operator !=(Error_ left, Error_ right) => !Equals(left, right);
        }

    }

    public static partial class ReadResultExtension
    {
        #region bind

        public static async Task<ReadResult<T1>> Bind<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, ReadResult<T1>> bind)
            => (await result.ConfigureAwait(false)).Bind(bind);

        public static async Task<ReadResult<T1>> Bind<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, Task<ReadResult<T1>>> bind)
            => await (await result.ConfigureAwait(false)).Bind(bind).ConfigureAwait(false);

        #endregion

        #region map

        public static async Task<ReadResult<T1>> Map<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, T1> map)
            => (await result.ConfigureAwait(false)).Map(map);

        public static Task<ReadResult<T1>> Map<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, Task<T1>> bind)
            => Bind(result, async v => ReadResult.Ok(await bind(v).ConfigureAwait(false)));

        public static ReadResult<T> MapError<T>(this ReadResult<T> result, Func<ReadFailure, ReadFailure> mapError) =>
            result.Match(ok => ok, error => ReadResult.Error<T>(mapError(error)));

        #endregion

        #region match

        public static async Task<T1> Match<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, Task<T1>> ok,
            Func<ReadFailure, Task<T1>> error)
            => await (await result.ConfigureAwait(false)).Match(ok, error).ConfigureAwait(false);

        public static async Task<T1> Match<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, Task<T1>> ok,
            Func<ReadFailure, T1> error)
            => await (await result.ConfigureAwait(false)).Match(ok, error).ConfigureAwait(false);

        public static async Task<T1> Match<T, T1>(
            this Task<ReadResult<T>> result,
            Func<T, T1> ok,
            Func<ReadFailure, T1> error)
            => (await result.ConfigureAwait(false)).Match(ok, error);

        #endregion

        public static ReadResult<T> Flatten<T>(this ReadResult<ReadResult<T>> result) => result.Bind(r => r);

        public static ReadResult<T1> As<T, T1>(this ReadResult<T> result, Func<ReadFailure> errorTIsNotT1) =>
            result.Bind(r =>
            {
                if (r is T1 converted)
                    return converted;
                return ReadResult.Error<T1>(errorTIsNotT1());
            });

        public static ReadResult<T1> As<T1>(this ReadResult<object> result, Func<ReadFailure> errorIsNotT1) =>
            result.As<object, T1>(errorIsNotT1);
        
        #region query-expression pattern
        
        public static ReadResult<T1> Select<T, T1>(this ReadResult<T> result, Func<T, T1> selector) => result.Map(selector);
        public static Task<ReadResult<T1>> Select<T, T1>(this Task<ReadResult<T>> result, Func<T, T1> selector) => result.Map(selector);
        
        public static ReadResult<T2> SelectMany<T, T1, T2>(this ReadResult<T> result, Func<T, ReadResult<T1>> selector, Func<T, T1, T2> resultSelector) => result.Bind(t => selector(t).Map(t1 => resultSelector(t, t1)));
        public static Task<ReadResult<T2>> SelectMany<T, T1, T2>(this Task<ReadResult<T>> result, Func<T, Task<ReadResult<T1>>> selector, Func<T, T1, T2> resultSelector) => result.Bind(t => selector(t).Map(t1 => resultSelector(t, t1)));
        public static Task<ReadResult<T2>> SelectMany<T, T1, T2>(this Task<ReadResult<T>> result, Func<T, ReadResult<T1>> selector, Func<T, T1, T2> resultSelector) => result.Bind(t => selector(t).Map(t1 => resultSelector(t, t1)));
        public static Task<ReadResult<T2>> SelectMany<T, T1, T2>(this ReadResult<T> result, Func<T, Task<ReadResult<T1>>> selector, Func<T, T1, T2> resultSelector) => result.Bind(t => selector(t).Map(t1 => resultSelector(t, t1)));

        #endregion
    }
}

namespace EventSourcing.Extensions
{
    public static partial class ReadResultExtension
    {
        public static IEnumerable<T1> Choose<T, T1>(
            this IEnumerable<T> items,
            Func<T, ReadResult<T1>> choose,
            Action<ReadFailure> onError)
            => items
                .Select(i => choose(i))
                .Choose(onError);

        public static IEnumerable<T> Choose<T>(
            this IEnumerable<ReadResult<T>> results,
            Action<ReadFailure> onError)
            => results
                .Where(r =>
                    r.Match(_ => true, error =>
                    {
                        onError(error);
                        return false;
                    }))
                .Select(r => r.GetValueOrThrow());

        public static ReadResult<T> As<T>(this object item, Func<ReadFailure> error) =>
            !(item is T t) ? ReadResult.Error<T>(error()) : t;

        public static ReadResult<T> NotNull<T>(this T? item, Func<ReadFailure> error) =>
            item ?? ReadResult.Error<T>(error());

        public static ReadResult<string> NotNullOrEmpty(this string? s, Func<ReadFailure> error)
            => string.IsNullOrEmpty(s) ? ReadResult.Error<string>(error()) : s!;

        public static ReadResult<string> NotNullOrWhiteSpace(this string? s, Func<ReadFailure> error)
            => string.IsNullOrWhiteSpace(s) ? ReadResult.Error<string>(error()) : s!;

        public static ReadResult<T> First<T>(this IEnumerable<T> candidates, Func<T, bool> predicate, Func<ReadFailure> noMatch) =>
            candidates
                .FirstOrDefault(i => predicate(i))
                .NotNull(noMatch);
    }
#pragma warning restore 1591
}
