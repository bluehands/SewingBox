using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace EventSourcing.Internals;

public class WakeUp
{
	readonly AsyncManualResetEvent _resetEvent  = new(true);
	readonly TimeSpan _maxWaitTime;
	readonly ILogger _logger;
	readonly TimeSpan _minWaitTime;
	TimeSpan _currentWaitTime;

	public WakeUp(TimeSpan minWaitTime, TimeSpan maxWaitTime, ILogger logger)
	{
		_minWaitTime = minWaitTime;
		_maxWaitTime = maxWaitTime;
		_logger = logger;
		_currentWaitTime = maxWaitTime;
	}

	public void WorkIsScheduled() => _resetEvent.Reset();

	public async Task WaitForSignalOrUntilTimeout(bool wakeMeUpSoon, CancellationToken cancellationToken)
	{
		var timeout = Task.Delay(_currentWaitTime, cancellationToken);
		var signal = _resetEvent.WaitAsync(cancellationToken);
		var completedTask = await Task.WhenAny(timeout, signal).ConfigureAwait(false);
		if (completedTask == signal || wakeMeUpSoon)
			_currentWaitTime = _minWaitTime;
		else
		{
			var nextWaitTime = _currentWaitTime.Add(_currentWaitTime);
			_currentWaitTime = nextWaitTime < _maxWaitTime ? nextWaitTime : _maxWaitTime;
		}
		if (_logger.IsEnabled(LogLevel.Debug))
			_logger.LogDebug($"Wait time set to {_currentWaitTime}");
	}

	public void ThereIsWorkToDo()
	{
		_resetEvent.Set();
		if (_logger.IsEnabled(LogLevel.Debug))
			_logger.LogDebug("Signaled");
	}
}