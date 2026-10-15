# MLock Debug Window

The MLock Debug Window provides real-time debugging capabilities for the MLock system, allowing you to:

- View all active locks in the system
- See which objects are affected by each lock
- Unlock specific locks or all locks at once

## How to Use

### Opening the Window

1. In Unity, go to `Window > MLock > Locks Debug` to open the debug window.

### Registering Lock Services

For the debug window to work, you need to register your lock services with the debug system. There are two ways to do this:

#### 1. Using the extension method (recommended):

```csharp
// Create and register your lock service in one step
var lockService = new BaseLockService<MyLockTags>().WithDebug();

// You can also open the debug window directly
var lockService = new BaseLockService<MyLockTags>().WithDebug().OpenDebugWindow();

// Unregister when no longer needed
lockService.WithoutDebug();
```

#### 2. Manual registration:

```csharp
// Create your lock service
var lockService = new BaseLockService<MyLockTags>();

// Register with debug system
MLockDebugData.RegisterLockService(lockService);

// Unregister when no longer needed
MLockDebugData.UnregisterLockService(lockService);
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
- The debug window uses reflection to access internal data from the lock service, so it may need to be updated if the internal structure changes 