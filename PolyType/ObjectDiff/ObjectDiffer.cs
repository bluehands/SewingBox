using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using PolyType;
using PolyType.Abstractions;
using PolyType.Utilities;

namespace ObjectDiff
{
    public delegate ImmutableList<Difference> Diff<in T>(T? item, T? other, PropertyPath? path);

    public static partial class Compare
    {
        public static ImmutableList<Difference> Diff<T>(T? item, T? other) where T : IShapeable<T> => 
            DiffCache<T, T>.Value(item, other, null);

        public static ImmutableList<Difference> Diff(object? item, object? other, ITypeShapeProvider provider) =>
            ((Diff<object>)Cache.GetOrAdd(typeof(object), provider))(item, other, null);

        public static Diff<T> Create<T>(ITypeShapeProvider shapeProvider)
            => (Diff<T>)Cache.GetOrAdd(typeof(T), shapeProvider)!;

        public static Diff<T> Create<T>(ITypeShape<T> shape)
            => (Diff<T>)Cache.GetOrAdd(shape)!;

        static readonly MultiProviderTypeCache Cache = new()
        {
            DelayedValueFactory = new DelayedDiffFactory(),
            ValueBuilderFactory = ctx => new Builder(ctx),
        };

        private static class DiffCache<T, TProvider> where TProvider : IShapeable<T>
        {
            public static Diff<T> Value => MyValue ??= Create(TProvider.GetShape());
            static Diff<T>? MyValue;
        }

        sealed class DelayedDiffFactory : IDelayedValueFactory
        {
            DelayedValue IDelayedValueFactory.Create<T>(ITypeShape<T> typeShape) =>
                new DelayedValue<Diff<T>>(self => (item, other, path) => self.Result(item, other, path));
        }

        class Builder(TypeGenerationContext generationContext) : TypeShapeVisitor, ITypeShapeFunc
        {
            static readonly FrozenSet<Type> SimpleTypes = new[]
            {
                typeof(string),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(Encoding),
            }.ToFrozenSet();

            public Diff<T> GetOrAddDiff<T>(ITypeShape<T> typeShape) =>
                (Diff<T>)generationContext.GetOrAdd(typeShape)!;

            public object? Invoke<T>(ITypeShape<T> typeShape, object? state = null)
            {
                return typeShape.Accept(this);
            }

            public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
            {
                if (objectShape.Type == typeof(object) || objectShape.Type.IsAbstract)
                {
                    return new Diff<T>((item, other, path) =>
                    {
                        if (ReferenceEquals(item, other))
                            return ImmutableList<Difference>.Empty;

                        path ??= PropertyPath.Empty;
                        if (item == null || other == null)
                            return [new(path, item, other)];

                        var itemType = item.GetType();
                        if (itemType != other.GetType())
                            return [new(path, item, other)];

                        var diff = (Delegate)generationContext.ParentCache!.GetOrAdd(itemType)!;
                        return (ImmutableList<Difference>)diff.DynamicInvoke(item, other, path)!;
                    });
                }
                
                var propertyDiffers = objectShape.Properties
                    .Where(p => p.HasGetter)
                    .Select(p => (propertyName: p.Name, diff: (Diff<T>)p.Accept(this)!))
                    .ToArray();

                var isSimpleType = SimpleTypes.Contains(objectShape.Type);
                if (!objectShape.Type.IsAbstract && (propertyDiffers.Length == 0 || isSimpleType))
                {
                    if (objectShape.Type.IsValueType)
                        return new Diff<T>((item, other, path) => 
                            !item!.Equals(other) 
                            ? [new(path ?? PropertyPath.Empty, item, other)] 
                            : ImmutableList<Difference>.Empty);

                    if (isSimpleType)
                    {
                        return new Diff<T>((item, other, path) => 
                            !Equals(item, other) 
                                ? [new(path ?? PropertyPath.Empty, item, other)] 
                                : ImmutableList<Difference>.Empty);
                    }

                    return new Diff<T>((_, _, _) => ImmutableList<Difference>.Empty);
                }

                return new Diff<T>((item, other, path) =>
                {
                    if (ReferenceEquals(item, other))
                        return ImmutableList<Difference>.Empty;

                    path ??= PropertyPath.Empty;
                    if (item == null || other == null)
                        return [new(path, item, other)];

                    var differences = ImmutableList<Difference>.Empty;
                    for (var i = 0; i < propertyDiffers.Length; i++)
                    {
                        var propertyDiffer = propertyDiffers[i];
                        path = path.Push(propertyDiffer.propertyName);
                        differences = differences.AddRange(propertyDiffer.diff(item, other, path));
                        path = path.Pop();
                    }

                    return differences;
                });
            }

            public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
            {
                var getter = propertyShape.GetGetter();
                var diff = GetOrAddDiff(propertyShape.PropertyType);

                return new Diff<TDeclaringType>((item, other, path) =>
                {
                    DebugExt.Assert(item != null && other != null && path != null);
                    var differences = diff(getter(ref item), getter(ref other), path);
                    return differences;
                });
            }

            public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
            {
                var diff = GetOrAddDiff(enumerableShape.ElementType);
                var getter = enumerableShape.GetGetEnumerable();

                return new Diff<TEnumerable>((item, other, path) =>
                {
                    if (ReferenceEquals(item, other))
                        return ImmutableList<Difference>.Empty;

                    path ??= PropertyPath.Empty;
                    if (item == null || other == null)
                        return [new(path, item, other)];

                    var itemEnumerable = getter(item);
                    var otherEnumerable = getter(other);

                    using var enumeratorItem = itemEnumerable.GetEnumerator();
                    using var enumeratorOther = otherEnumerable.GetEnumerator();

                    var diffs = ImmutableList<Difference>.Empty;
                    var index = 0;
                    while (true)
                    {
                        var oldHasValue = enumeratorItem.MoveNext();
                        var newHasValue = enumeratorOther.MoveNext();

                        if (!oldHasValue && !newHasValue)
                            break;

                        path = path.PushIndex(index);
                        if (!oldHasValue)
                            diffs = diffs.Add(new(path, null, enumeratorOther.Current));
                        else if (!newHasValue)
                            diffs = diffs.Add(new(path, enumeratorItem.Current, null));
                        else
                            diffs = diffs.AddRange(diff(enumeratorItem.Current, enumeratorOther.Current, path));
                        path = path.Pop();

                        index++;
                    }

                    return diffs;
                });
            }
        }
    }


    public interface IDiff<in T>
    {
        public ImmutableList<Difference> Diff(T? item, T? other, PropertyPath? path);
    }

    sealed class ObjectDiff<T> : IDiff<T>
    {
        public required IDiff<T>[] PropertyDiffers { get; init; }

        public ImmutableList<Difference> Diff(T? item, T? other, PropertyPath? path)
        {
            if (ReferenceEquals(item, other))
                return ImmutableList<Difference>.Empty;

            path ??= PropertyPath.Empty;
            if (item == null || other == null)
                return [new(path, item, other)];

            var differences = ImmutableList<Difference>.Empty;
            foreach (var propertyDiffer in PropertyDiffers)
            {
                differences = differences.AddRange(propertyDiffer.Diff(item, other, path));
            }

            return differences;
        }
    }

    sealed class PropertyDiff<TDeclaringType, TPropertyType> : IDiff<TDeclaringType>
    {
        public required string PropertyName { get; init; }
        public required Getter<TDeclaringType, TPropertyType> Getter { get; init; }
        public required IDiff<TPropertyType> PropertyTypeDiffer { get; init; }

        public ImmutableList<Difference> Diff(TDeclaringType? item, TDeclaringType? other, PropertyPath? path)
        {
            DebugExt.Assert(item != null && other != null && path != null);
            return PropertyTypeDiffer.Diff(Getter(ref item), Getter(ref other), path.Push(PropertyName));
        }
    }
}

static class DebugExt
{
    /// <summary>
    /// A replacement for <see cref="Debug.Assert(bool, string?)"/> that has the appropriate annotations for netstandard2.0.
    /// </summary>
    /// <param name="condition">The conditional expression to evaluate. If the condition is true, the specified messages are not sent and the message box is not displayed.</param>
    /// <param name="message">The message to send to the Listeners collection.</param>
    [Conditional("DEBUG")]
    public static void Assert(
        [DoesNotReturnIf(false)] bool condition,
        [CallerArgumentExpression(nameof(condition))] string? message = null)
    {
        Debug.Assert(condition, message, string.Empty);
    }
}
