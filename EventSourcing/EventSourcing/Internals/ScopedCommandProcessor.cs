using EventSourcing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Internals;

public sealed class ScopedCommandProcessor : IDisposable
{
    private readonly IServiceScope scope;

    public ScopedCommandProcessor(CommandProcessor? processor, IServiceScope scope)
    {
        this.Processor = processor;
        this.scope = scope;
    }

    public CommandProcessor? Processor { get; }

    public void Dispose() => this.scope.Dispose();
}