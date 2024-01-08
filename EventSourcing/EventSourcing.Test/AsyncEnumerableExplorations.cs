using Microsoft.Extensions.DependencyInjection;
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

    static async IAsyncEnumerable<int> MyAsyncProducer()
    {
        yield return 1;
        await Task.Delay(10);
        yield return 2;
        await Task.Delay(10);
        throw new Exception("Boom");
    }
}

[TestClass]
public class DependencyScopesExplorations
{
    [TestMethod]
    public void Then_assertion()
    {
        var services = new ServiceCollection();

        services.AddScoped<Dependency>();
        services.AddTransient<Sample>();

        //services.AddSingleton(p =>
        //{
        //    var scope = p.CreateScope();
        //    var sample = p.GetRequiredService<Sample>();
        //    return new EventStream(sample, scope);
        //});

        services.AddSingleton<EventStream>();

        var provider = services.BuildServiceProvider();

        //var x = provider.GetService<EventStream>();
        using (var scope = provider.CreateScope())
        {
            var y = scope.ServiceProvider.GetService<EventStream>();
            Console.WriteLine(y);
        }

        var z = provider.GetService<EventStream>();
        Console.WriteLine(z);
    }

    class Sample(Dependency dependency) : IDisposable
    {
        public Dependency Dependency { get; } = dependency;

        public override string ToString() => $"{GetHashCode()}, Dependency: {Dependency}";

        public void Dispose()
        {
            Console.WriteLine($"{GetType().Name} disposed");
        }
    }

    class Dependency : IDisposable
    {
        public override string ToString() => GetHashCode().ToString();

        public void Dispose()
        {
            Console.WriteLine($"{GetType().Name} disposed");
        }
    }

    class EventStream(Sample sample, IServiceScope? scope = null) : IDisposable
    {
        public Sample Sample { get; } = sample;

        public void Dispose()
        {
            scope?.Dispose();
            Console.WriteLine($"{GetType().Name} disposed");
        }

        public override string ToString() => Sample.ToString();
    }
}