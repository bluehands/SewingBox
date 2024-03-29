﻿namespace ObservableExplorations.Infrastructure;

public static class SemaphoreSlimExtension
{
	public static async Task<T> ExecuteGuarded<T>(this SemaphoreSlim semaphore, Func<Task<T>> f)
	{
		await semaphore.WaitAsync().ConfigureAwait(false);
		try
		{
			return await f().ConfigureAwait(false);
		}
		finally
		{
			semaphore.Release();
		}
	}

	public static async Task<T> ExecuteGuarded<T>(this SemaphoreSlim semaphore, Func<T> f)
	{
		await semaphore.WaitAsync().ConfigureAwait(false);
		try
		{
			return f();
		}
		finally
		{
			semaphore.Release();
		}
	}

	public static async Task ExecuteGuarded(this SemaphoreSlim semaphore, Action action)
	{
		await semaphore.WaitAsync().ConfigureAwait(false);
		try
		{
			action();
		}
		finally
		{
			semaphore.Release();
		}
	}

	public static async Task ExecuteGuarded(this SemaphoreSlim semaphore, Func<Task> f)
	{
		await semaphore.WaitAsync().ConfigureAwait(false);
		try
		{
			await f().ConfigureAwait(false);
		}
		finally
		{
			semaphore.Release();
		}
	}
}