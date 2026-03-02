using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ecommerce.Core.Utils.Threading;

public sealed class KeyedLock
{
    private static readonly TimeSpan NoTimeout = TimeSpan.FromMilliseconds(-1);
    private static readonly ConcurrentDictionary<object, KeyedLock> Locks = new();

    private int _waiterCount;

    private object Key { get; }

    private SemaphoreSlim Semaphore { get; }

    public bool HasWaiters => _waiterCount > 1;

    public int WaiterCount => _waiterCount;

    private KeyedLock(object key)
    {
        Key = key;
        Semaphore = new SemaphoreSlim(1, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementCount()
    {
        Interlocked.Increment(ref _waiterCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecrementCount()
    {
        Interlocked.Decrement(ref _waiterCount);
    }

    public static bool IsLockHeld(object key)
    {
        return Locks.ContainsKey(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable Lock(object key)
    {
        return Lock(key, NoTimeout);
    }

    public static IDisposable Lock(object key, TimeSpan timeout)
    {
        var keyedLock = GetOrCreateLock(key);
        keyedLock.Semaphore.Wait(timeout);
        return new Releaser(keyedLock);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<IDisposable> LockAsync(object key)
    {
        return LockAsync(key, NoTimeout, CancellationToken.None);
    }

    public static Task<IDisposable> LockAsync(object key, TimeSpan timeout)
    {
        return LockAsync(key, timeout, CancellationToken.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<IDisposable> LockAsync(object key, CancellationToken cancellationToken)
    {
        return LockAsync(key, NoTimeout, cancellationToken);
    }

    public static Task<IDisposable> LockAsync(object key, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var keyedLock = GetOrCreateLock(key);
        var wait = keyedLock.Semaphore.WaitAsync(timeout, cancellationToken);

        return wait.IsCompleted
            ? Task.FromResult(new Releaser(keyedLock) as IDisposable)
            : wait.ContinueWith(
                (t, s) => new Releaser(s as KeyedLock) as IDisposable,
                keyedLock,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
    }

    private static KeyedLock GetOrCreateLock(object key)
    {
        var item = Locks.GetOrAdd(key, k => new KeyedLock(key));
        item.IncrementCount();
        return item;
    }

    private sealed class Releaser : IDisposable
    {
        private KeyedLock? _item;

        public Releaser(KeyedLock? item)
        {
            _item = item;
        }

        public void Dispose()
        {
            if (_item == null)
            {
                return;
            }

            _item.DecrementCount();
            if (_item.WaiterCount == 0)
            {
                // Remove from dict
                Locks.TryRemove(_item.Key, out _);
            }

            if (_item.Semaphore.CurrentCount == 0)
            {
                _item.Semaphore.Release();
            }

            //_item.Semaphore.Dispose();
            _item = null;
        }
    }
}