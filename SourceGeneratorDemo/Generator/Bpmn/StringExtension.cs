namespace Generator.Bpmn;

public static class StringExtension
{
	public static IEnumerable<string> SplitOmitEmptyTrim(this string value, string separator = ",")
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Enumerable.Empty<string>();
		}

		return value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
	}

	public static string ToSeparatedString<T>(this IEnumerable<T> values, string separator = ",") => string.Join(separator, values);   

	public static string FirstToUpper(this string name) => char.ToUpperInvariant(name[0]) + name.Substring(1);
}