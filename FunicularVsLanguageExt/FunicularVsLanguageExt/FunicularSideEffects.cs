using FunicularSwitch;

namespace FunicularVsLanguageExt;

[TestClass]
public class FunicularSideEffects
{
    [TestMethod]
    public async Task TestMethod()
    {
        const int x = 1;
        const int y = 1;

        static int Div(int a, int b) => a / b;

        var awesome = (await Result
            .Try(() => Div(x, y), e => e.Message)
            .Map(num => $"Result is {num}")
            .Map(async num =>
            {
                await Task.Delay(3000);
                return num;
            }))
            .Match(err => err);

        Console.WriteLine(awesome);
    }
}