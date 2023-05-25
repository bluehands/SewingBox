using FunicularSwitch.Generators;

namespace DoubleCheckedLock;

[EnumType]
public enum MyEnum
{
	One,
	Two,
	Three,
	Four
}

public static class EnumSwitcher
{
	public static string ToStringSwitchExpression(this MyEnum value) => value switch
	{
		MyEnum.One => "One",
		MyEnum.Two => "Two",
		MyEnum.Three => "Three",
		MyEnum.Four => "Four",
		_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
	};

	public static string ToStringMatch(this MyEnum value) => value.Match(
		one: () => "One",
		two: () => "Two",
		three: () => "Three",
		four: () => "Four"
	);
}

