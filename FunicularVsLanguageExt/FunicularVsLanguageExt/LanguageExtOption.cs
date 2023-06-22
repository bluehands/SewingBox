using LanguageExt;
using static LanguageExt.Prelude;

namespace FunicularVsLanguageExt;

[TestClass]
public class LanguageExtOption
{
    [TestMethod]
    public void Nullability()
    {
        Option<int> option = default!;
        var message = option.Match(some => "got some", () => "got none"); // this works
        Console.WriteLine(message);
    }
    
    [TestMethod]
    public void FluentVsQuery()
    {
        var aNumber = Some(42);
        var anotherNumber = Some(1);
        var yetAnotherNumber = Some(2);

        Option<int> result = aNumber.Bind(a => anotherNumber.Bind(b => yetAnotherNumber.Map(c => a + b + c)));
        Console.WriteLine(result);
        
        // a different approach that does the same thing:

        Option<int> sameResult =
            from a in aNumber
            from b in anotherNumber
            from c in yetAnotherNumber
            select a + b + c;
        
        Console.WriteLine(sameResult); // this looks better now
    }
}