# MLock Debug Window

The MLock Debug Window provides real-time debugging capabilities for the MLock system, allowing you to:

- View all active locks in the system
- See which objects are affected by each lock
- Unlock specific locks or all locks at once

## How to Use

### Opening the Window

1. In Unity, go to `Window > MLock > Locks Debug` to open the locks window, or `Window > MLock > Services Debug` to list the registered services.

### Registering Lock Services

For the debug window to work, you need to register your lock services with the debug system. There are two ways to do this:

#### 1. Using the extension method (recommended):

```csharp
using Migs.MLock.Debugging;

// Create and register your lock service in one step
var lockService = new BaseLockService<MyLockTags>().WithDebug();

// Unregister when no longer needed
lockService.WithoutDebug();
```

#### 2. Manual registration:

```csharp
using Migs.MLock.Debugging;

// Create your lock service
var lockService = new BaseLockService<MyLockTags>();

// Register with debug system
DebugDataHandler.RegisterLockService(lockService);

// Unregister when no longer needed
DebugDataHandler.UnregisterLockService(lockService);
```

### Window Features

- **Auto Refresh**: Automatically refreshes the lock data (can be toggled off)
- **Refresh Button**: Manually refresh the lock data
- **Unlock All**: Unlock all active locks in the system
- **Lock Details**: Expand each lock to see details about included/excluded tags and affected lockables
- **Unlock Button**: Unlock a specific lock

## Notes

- Debug features only work in Editor mode and have no impact on builds
- Registered services are cleared when Play mode starts and when it exits, so services from earlier sessions never show up
- Lock origins (the call stack that created a lock) are captured only while a debug window is open; `DebugDataHandler.SetEnabled(true)` turns capture on without a window 