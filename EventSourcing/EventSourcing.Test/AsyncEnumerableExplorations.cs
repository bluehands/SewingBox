using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventSourcing.Test;

[TestClass]
public class AsyncEnumerableExplorations
{
    [TestMethod]
    public async Task Then_assertion()
    {
        var enumerable = MyAsyncProducer();
        try
        {
            await foreach (var x in enumerable)
            {
                Console.WriteLine(x);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    async IAsyncEnumerable<int> MyAsyncProducer()
    {
        yield return 1;
        await Task.Delay(10);
        yield return 2;
        await Task.Delay(10);
        throw new Exception("Boom");
    }
}