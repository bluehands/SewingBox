using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace ObjectDiff;

public record Difference(
	PropertyPath PropertyPath,
	object? OldValue,
	object? NewValue
)
{
	public PropertyPath PropertyPath { get; } = PropertyPath;
	public object? OldValue { get; } = OldValue;
	public object? NewValue { get; } = NewValue;
}

public record PropertyPath
{
	readonly PropertyPath? _tail;
	string? _pathString;
	public string PathString => _pathString ??= JoinPath();

	public static readonly PropertyPath Empty = new(ImmutableStack<PathSegment>.Empty, null);

	public ImmutableStack<PathSegment> Path { get; }

	PropertyPath(ImmutableStack<PathSegment> path, PropertyPath? tail)
	{
		_tail = tail;
		Path = path;
	}

	string JoinPath() => string.Join("", Path.Reverse().Select((p, i) => i == 0 || p is PathSegment.Index_ or PathSegment.Key_ ? p.ToString() : $".{p}"));

	[Pure]
	public PropertyPath Push(string segment)
	{
		var pathSegment = new PathSegment.Property_(segment);
		return Push(pathSegment);
	}

	[Pure]
	public PropertyPath PushKey(object value) => Push(new PathSegment.Key_(value));

	[Pure]
	public PropertyPath PushIndex(int value) => Push(new PathSegment.Index_(value));

	[Pure]
	PropertyPath Push(PathSegment pathSegment) => new(Path.Push(pathSegment), this);

	[Pure]
	public PropertyPath Pop() => _tail ?? throw new InvalidOperationException("Path is empty. Pop not allowed.");

	public override string ToString() => PathString;

	public virtual bool Equals(PropertyPath? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return PathString == other.PathString;
	}

	public override int GetHashCode() => PathString.GetHashCode();
}

public abstract record PathSegment
{
	public static PathSegment Property(string name) => new Property_(name);
	public static PathSegment Key(object value) => new Key_(value);
	public static PathSegment Index(int value) => new Index_(value);

	public sealed record Property_(string Name) : PathSegment
	{
		public string Name { get; } = Name;

		public override string ToString() => Name;
	}

	public sealed record Key_(object Value) : PathSegment
	{
		public object Value { get; } = Value;

		public override string ToString() => $"[{Value}]";
	}

	public sealed record Index_(int Value) : PathSegment
	{
		public int Value { get; } = Value;

		public override string ToString() => $"[{Value}]";
	}
}