﻿using EventSourcing.Commands;

namespace EventSourcing.Internals;

public sealed class ScopedCommandProcessor : IDisposable
{
	readonly IDisposable _scope;

    public ScopedCommandProcessor(CommandProcessor processor, IDisposable scope)
    {
        Processor = processor;
        _scope = scope;
    }

    public CommandProcessor Processor { get; }

    public void Dispose() => _scope.Dispose();
}