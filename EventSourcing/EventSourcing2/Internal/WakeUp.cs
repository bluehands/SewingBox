using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace EventSourcing2.Internal;

public class WakeUp
{
	readonly AsyncManualResetEvent _resetEvent  = new(true);
	readonly TimeSpan _maxWaitTime;
	readonly ILogger? _logger;
	readonly TimeSpan _minWaitTime;
	TimeSpan _currentWaitTime;

	public WakeUp(TimeSpan minWaitTime, TimeSpan maxWaitTime, ILogger? logger)
	{
		_minWaitTime = minWaitTime;
		_maxWaitTime = maxWaitTime;
		_logger = logger;
		_currentWaitTime = maxWaitTime;
	}

	public async Task WaitForSignalOrUntilTimeout(bool wakeMeUpSoon, CancellationToken cancellationToken)
    {
        if (wakeMeUpSoon)
            _currentWaitTime = _minWaitTime;

        var timeout = Task.Delay(_currentWaitTime, cancellationToken);
        // ReSharper disable once MethodSupportsCancellation -> do not use overload with cancellation, it collects CancellationTaskTokenSource objects. Cancellation works anyway because timeout task supports cancellation, that's enough because of WhenAny
        var signal = _resetEvent.WaitAsync();
        var completedTask = await Task.WhenAny(timeout, signal).ConfigureAwait(false);
        if (completedTask == signal)
            _currentWaitTime = _minWaitTime;
        else
        {
            var nextWaitTime = _currentWaitTime.Add(_currentWaitTime == TimeSpan.Zero ? TimeSpan.FromTicks(_maxWaitTime.Ticks / 10) : _currentWaitTime);
            _currentWaitTime = nextWaitTime < _maxWaitTime ? nextWaitTime : _maxWaitTime;
        }
        if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
            _logger.LogDebug($"Wait time set to {_currentWaitTime}");
    }

    public void WorkIsScheduled() => _resetEvent.Reset();

	public void ThereIsWorkToDo()
	{
		_resetEvent.Set();
		if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
			_logger.LogDebug("Signaled");
	}
}