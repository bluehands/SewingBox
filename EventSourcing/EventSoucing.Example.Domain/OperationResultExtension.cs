namespace EventSourcing.Example.Domain;

public static class OperationResultExtension
{
	public static OperationResult<T> ToResult<T>(this FunicularSwitch.Option<T> option, Func<string> errorOnEmpty) =>
		option.Match(some => some, () => OperationResult.InvalidInput<T>(errorOnEmpty()));
}