namespace FunicularVsLanguageExt;

using LanguageExt;
using static LanguageExt.Prelude;

[TestClass]
public class LanguageExtSideEffects
{
    [TestMethod]
    public async Task TestMethod()
    {
        const int x = 1;
        const int y = 0;

        static int Div(int a, int b) => a / b;

        static Eff<string> DivideAndMapToMessage(int a, int b) =>
            from div in Eff(() => Div(a, b))
            select $"Result is {div}";

        var awesome =
            from result in DivideAndMapToMessage(x, y)
                           | @catch(err => err is DivideByZeroException, exception => exception.Message)
            from _ in Aff(() => Task.Delay(3000).ToUnit().ToValue())
            select result;

        Console.WriteLine(await awesome.Run());
    }
}