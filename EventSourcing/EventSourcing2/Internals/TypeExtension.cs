using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EventSourcing2.Internals;

static partial class TypeExtension
{
	public static Type[] GetConcreteDerivedTypes(this Type type, IEnumerable<Assembly> assemblies) =>
		assemblies
			.SelectMany(a => a.GetTypes())
			.Where(t => !t.IsAbstract && t.IsSubclassOf(type))
			.ToArray();

	public static Type GetArgumentOfFirstGenericBaseType(this Type type, int argumentIndex = 0)
		=> type.GetArgumentsOfFirstGenericBaseType()[argumentIndex];

	public static Type[] GetArgumentsOfFirstGenericBaseType(this Type type, Func<Type, bool>? predicate = null)
	{
		predicate ??= _ => true;

		var baseType = type.BaseType;
		while (baseType != null)
		{
			if (baseType.IsGenericType && predicate(baseType))
			{
				return baseType.GetGenericArguments();
			}
			baseType = baseType.BaseType;
		}
		throw new InvalidOperationException($"Type {type.Name} does not have generic base type");
	}

	public static string BeautifulName(this Type t)
	{
		if (!t.IsGenericType)
			return t.Name;
		try
		{
			var seed = new StringBuilder();
			var length = t.Name.LastIndexOf("`", StringComparison.Ordinal);
			if (length < 0)
				return t.Name;
			seed.Append(t.Name.Substring(0, length));
			var i = 0;
			t.GetGenericArguments().Aggregate(seed, (a, type) => a.Append(i++ == 0 ? "<" : ",").Append(type.BeautifulName()));
			seed.Append(">");
			return seed.ToString();
		}
		catch (Exception)
		{
			return t.Name;
		}
	}

	public static Type GetBaseType(this Type type, Func<Type, bool>? predicate = null)
	{
		predicate ??= (_ => true);

		var baseType = type.BaseType;
		while (baseType != null)
		{
			if (predicate(baseType))
			{
				return baseType;
			}
			baseType = baseType.BaseType;
		}
		throw new InvalidOperationException($"Type {type.Name} has no matching base type");
	}
}