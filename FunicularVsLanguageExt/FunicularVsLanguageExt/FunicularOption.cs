using FunicularSwitch;

namespace FunicularVsLanguageExt;

[TestClass]
public class FunicularOption
{
    [TestMethod]
    public void Nullability()
    {
        Option<int> option = default!;
        var message = option.Match(some => "got some", () => "got none"); // this fails
        Console.WriteLine(message);
    }

    [TestMethod]
    public void FluentVsQuery()
    {
        var aNumber = Option.Some(42);
        var anotherNumber = Option.Some(1);
        var yetAnotherNumber = Option.Some(2);

        Option<int> result = aNumber.Bind(a => anotherNumber.Bind(b => yetAnotherNumber.Map(c => a + b + c)));
        Console.WriteLine(result);
        
        // a different approach that does almost the same thing:

        IEnumerable<int> sameResult =
            from a in aNumber
            from b in anotherNumber
            from c in yetAnotherNumber
            select a + b + c;
        
        Console.WriteLine(sameResult.FirstOrDefault()); // well it could be better (it could be an option) 
    }
}