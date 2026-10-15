# Changelog

## [3.0.1] - 2026-10-16

### Fixed
- Subscriptions are keyed by reference identity instead of `Equals`, so two distinct lockables that compare equal (records, overridden `Equals`) both subscribe, and `Unsubscribe`/`IsLocked` never act on the wrong one. A destroyed `UnityEngine.Object` lockable is still found.
- A `Lock`, `LockAllExcept` or `LockAll` whose callback throws no longer leaves a lock without a handle; the lock is rolled back and the exception rethrown.
- An exception thrown by `HandleLocking`/`HandleUnlocking` no longer skips the remaining lockables; every lockable is notified, then the exception is rethrown.
- A `Subscribe` whose `HandleLocking` throws is rolled back, so it leaves no subscription behind. A lockable whose `HandleLocking` threw still receives the matching `HandleUnlocking`.
- A `LockSwitch` turned off and on again by a callback during its own `Set(true)` no longer leaks a lock, and a switch whose lock callback throws stays off instead of reporting on without a lock.
- Closing the Services debug window now stops lock origin capture once no debug window is open.

### Changed
- When several lockable callbacks throw during one notification, they are rethrown together as one `AggregateException` in notification order; a single exception is rethrown as-is with its original stack trace.
- The debug Unlock All keeps its own snapshot of active locks and releases all of them even when a callback throws.

## [3.0.0] - 2026-10-15

### Breaking changes
- `Lock`, `LockAllExcept` and `LockAll` return a `LockHandle<TLockTags>` struct instead of `ILock<TLockTags>`. Disposing a released handle, a stale copy or a `default` handle does nothing, so a handle can never release a newer lock.
- `ILockService<TLockTags>.TryLocking` and `TryUnlocking` are removed.
- `ILock`, `ILock<T>`, `IDebuggableLock<T>`, `ILockPool<T>`, `LockPool<T>`, `ILockableData<T>`, `LockableData<T>`, `ILockableDataPool<T>`, `LockableDataPool<T>` and the `Migs.MLock.Pools` and `Migs.MLock.Data` namespaces are removed.
- `BaseLockService(int poolsInitialCapacity)` is replaced by `BaseLockService(int lockCapacity = 8, int lockableCapacity = 32)`.
- `Lock(None)` locks nothing (it used to lock everything). `LockAll()` locks everything, and `LockAllExcept(None)` too.
- A lockable's `LockTags` are read once, at `Subscribe`. Changing them while subscribed has no effect; unsubscribe and subscribe again instead.
- `Unsubscribe` calls `HandleUnlocking` only when the lockable is locked.
- `ILockService<TLockTags>.Subscribe` returns a `LockSubscription<TLockTags>` instead of `void`.
- `LockableBehaviourAttribute` and the `Migs.MLock.Attributes` namespace are removed; the attribute was never used.

### Fixed
- Disposing a lock twice could release a newer lock that reused the pooled instance.
- Enums with values outside the `uint` range (negative members, `~0`, `ulong` flags above bit 31) threw or were truncated; every underlying type is now supported.
- Locking, unlocking, subscribing and unsubscribing no longer allocate after warm-up.
- Lock origins (stack traces) are captured only while a debug window is open.
- Subscribing, unsubscribing, locking or releasing from inside `HandleLocking`/`HandleUnlocking` no longer double-counts, skips or unpairs notifications: each lockable gets alternating calls and the last one matches its final state. A lockable unsubscribed before its pending `HandleLocking` is delivered gets neither call.

### Changed
- `ILockable<TLockTags>` is no longer declared covariant (`out`). Variance never applies to enum arguments, so no code can observe the change.
- The runtime APIs carry nullable annotations.
- `WithDebug<TLockTags, TCast>` and `WithoutDebug<TLockTags, TCast>` throw `InvalidCastException` when the service is not a `TCast`, instead of returning null.

### Added
- `LockHandle<TLockTags>.IsActive`.
- `LockGate<TLockTags>`: a ready-made lockable with `IsLocked` and an optional `onChanged` callback, disposed to unsubscribe.
- `LockSubscription<TLockTags>`, returned by `Subscribe`: disposing it unsubscribes the lockable, and disposing twice, a stale copy or a `default` subscription does nothing.
- `DebugDataHandler.IsEnabled` and `LockDebugInfo.Mode`.
- `LockSwitch<TLockTags>`, a lock toggled by `Set(bool)`, created with the `CreateSwitch`, `CreateSwitchAllExcept` and `CreateSwitchAll` extensions on `ILockService<TLockTags>`.
- `WithoutDebug()` on `ILockService<TLockTags>`, so `lockService.WithoutDebug()` compiles like `lockService.WithDebug()`.

### Upgrading from 2.x
- Change `ILock<T>` fields and variables to `LockHandle<T>`. Store a handle as a `LockHandle<T>`, never as `IDisposable`: that boxes it.
- Replace `_lock?.Dispose(); _lock = null;` with `_lock.Dispose();`. Disposing twice is safe.
- Remove calls to `TryLocking`/`TryUnlocking`, and their forwards in custom `ILockService<T>` implementations.
- Rename `poolsInitialCapacity` to `lockCapacity` (and set `lockableCapacity` if you subscribe many lockables).
- If you relied on `Lock(None)` locking everything, call `LockAll()`.
- If a lockable changes its `LockTags` at runtime, unsubscribe it before the change and subscribe it again after.
- Custom `ILockService<T>` implementations return the `LockSubscription<T>` from `Subscribe` (forward it from the wrapped service). Callers that ignore the result need no change, and `Unsubscribe(lockable)` still works.
- Remove `[LockableBehaviour(...)]` attributes.
