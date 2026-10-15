# MLock

[![Unity Version](https://img.shields.io/badge/Unity-2022.3+-blue.svg)](https://unity.com/releases/editor/whats-new/2022.3.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.1.0-blue.svg)](src/mlock-unity-project/Packages/MLock/package.json)

A flexible and efficient lock system for Unity that manages access to gameplay elements based on configurable tags.

## Overview

MLock is a powerful lock management system designed specifically for Unity that provides a structured way to control access to game objects and features. It uses a tag-based approach to determine which objects should be locked or unlocked at any given time, making it ideal for managing game state, feature access, UI interactions, and more.

## Features

- **Tag-based Locking**: Lock and unlock objects based on customizable enum tags
- **Efficient Object Pooling**: Built-in object pooling for optimal performance
- **Flexible API**: Simple but powerful interface for integration into any project
- **Inclusion/Exclusion Logic**: Lock specific tags or lock everything except specific tags
- **Lightweight**: Minimal overhead with a focus on performance
- **Generic Implementation**: Works with any custom enum type for maximum flexibility
- **[Debug Windows](#debug-windows)**: Built-in tools to monitor and control locks in real-time

## Installation

<details>
<summary><b>Using OpenUPM (Recommended)</b></summary>

#### Option 1: Via Command Line

```bash
# Install openupm-cli if you don't have it
npm install -g openupm-cli

# Navigate to your Unity project directory
cd path/to/your/unity/project

# Add the package
openupm add com.migsweb.mlock
```

#### Option 2: Via Package Manager UI

1. Open your Unity project
2. Add the OpenUPM scoped registry to your project:
   - Open `Edit > Project Settings > Package Manager`
   - Add a new Scoped Registry:
     - Name: `OpenUPM`
     - URL: `https://package.openupm.com`
     - Scope(s): `com.migsweb`
3. Click `Save`
4. Open `Window > Package Manager`
5. Change the package source to `My Registries`
6. Find `MLock` and click `Install`

#### Option 3: Via Manual JSON Editing

Add the OpenUPM scoped registry and the package to your `manifest.json` file in the `Packages` folder of your Unity project:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.migsweb"
      ]
    }
  ],
  "dependencies": {
    "com.migsweb.mlock": "1.2.0",
    // other dependencies...
  }
}
```
</details>

<details>
<summary><b>Using Git URL (Alternative)</b></summary>

1. Open your Unity project
2. Go to `Window > Package Manager`
3. Click the `+` button > `Add package from git URL`
4. Enter: `https://github.com/migus88/mlock.git?path=/src/mlock-unity-project/Packages/MLock`
5. Click `Add`

To use a specific version, add a tag to the URL:
```
https://github.com/migus88/mlock.git?path=/src/mlock-unity-project/Packages/MLock#1.2.0
```
</details>

<details>
<summary><b>Manual Installation</b></summary>

1. Go to the [Releases](https://github.com/migus88/mlock/releases) page
2. Download the latest `.unitypackage` file
3. Import it into your Unity project via `Assets > Import Package > Custom Package`
</details>

## Quick Start

1. Define your lock tags enum (power-of-two values required as well as a zero state):

```csharp
[System.Flags] // Optional
public enum GameplayFeatures
{
    None = 0, // Required
    Movement = 1 << 0,
    Inventory = 1 << 1,
    Dialog = 1 << 2,
    Combat = 1 << 3,
    // Add more as needed
}
```

2. Create a lock service:

```csharp
using Migs.MLock;
using Migs.MLock.Interfaces;

// Create service instance (should be shared across a domain)
private ILockService<GameplayFeatures> _lockService;

void Awake()
{
    _lockService = new BaseLockService<GameplayFeatures>();
}
```

3. Implement the ILockable interface on objects that can be locked:

```csharp
using Migs.MLock.Interfaces;
using UnityEngine;

public class PlayerController : MonoBehaviour, ILockable<GameplayFeatures>
{
    public GameplayFeatures LockTags => GameplayFeatures.Movement;
    
    private bool _isLocked = false;

    public void Start()
    {
        // Don't actually do that - use a proper DI framework
        GameManager.Instance.LockService.Subscribe(this);
    }

    public void OnDestroy()
    {
        GameManager.Instance.LockService.Unsubscribe(this);
    }
    
    public void HandleLocking()
    {
        _isLocked = true;
        // Disable movement logic here
    }
    
    public void HandleUnlocking()
    {
        _isLocked = false;
        // Enable movement logic here
    }
    
    void Update()
    {
        if (!_isLocked)
        {
            // Movement logic here
        }
    }
}
```

4. Create and use locks:

```csharp
// Lock specific features
LockHandle<GameplayFeatures> movementLock = _lockService.Lock(GameplayFeatures.Movement);

// Lock everything except specific features
LockHandle<GameplayFeatures> lockAllButDialog = _lockService.LockAllExcept(GameplayFeatures.Dialog);

// Lock everything
LockHandle<GameplayFeatures> lockAll = _lockService.LockAll();

// Unlock by disposing the lock
movementLock.Dispose();
```

## Examples

The repository includes a Car Example that demonstrates the MLock system in action. The example shows a simple car interface with menu systems where different UI elements need to be locked based on the current context. The same functionality is implemented using two different UI approaches:

### Available Examples:

- [Shared Components](src/mlock-unity-project/Assets/Examples/Shared)
- [Car with Unity Canvas UI](src/mlock-unity-project/Assets/Examples/Car_Canvas)
- [Car with UI Toolkit (UI Elements)](src/mlock-unity-project/Assets/Examples/Car_UiElements)

## Advanced Usage

### Using Locks with Using Statements

1. With predefined scope
```csharp
// Locks are automatically disposed at the end of the block
using (_lockService.Lock(GameplayFeatures.Combat))
{
    // Combat is locked within this block
    // Run combat sequence or cutscene
}
// Combat is automatically unlocked when exiting the block
```
2. Until the end of the current scope
```csharp
// Locks are automatically disposed at the end of the method
public void SomeComplexMethod()
{
    using var combatLock = _lockService.Lock(GameplayFeatures.Combat);

    // Do a lot of things here
    // ...
} // Combat is automatically unlocked when exiting the method
```

### Holding a lock

`Lock`, `LockAllExcept` and `LockAll` return a `LockHandle<T>` struct. Keep it in a `LockHandle<T>` field (not an `IDisposable`, which boxes it) and dispose it to release the lock. Disposing twice, disposing a copy or disposing a `default` handle is safe and never releases another lock; `IsActive` tells whether the lock is still held.

```csharp
private LockHandle<GameplayFeatures> _menuLock;

void OpenMenu() => _menuLock = _lockService.Lock(GameplayFeatures.Movement);
void CloseMenu() => _menuLock.Dispose();
```

### State-driven locks (LockSwitch)

When a lock follows a state (a menu is open, a console is visible), a `LockSwitch<T>` replaces the hand-written "lock if not locked yet, release if held" bookkeeping. Create it once, store it in a field, and toggle it with `Set(bool)`: setting the current state again does nothing, so it never holds more than one lock, and toggling allocates nothing. Dispose it at teardown; after that `Set` does nothing.

```csharp
private readonly LockSwitch<GameplayFeatures> _menuLock;

public MenuController(ILockService<GameplayFeatures> lockService)
{
    _menuLock = lockService.CreateSwitch(GameplayFeatures.Movement); // or CreateSwitchAllExcept(...), CreateSwitchAll()
}

void OnMenuVisibilityChanged(bool isVisible) => _menuLock.Set(isVisible);
void OnDestroy() => _menuLock.Dispose();
```

### Contracts

- A lockable's `LockTags` are read once, at `Subscribe`, and must not change while it is subscribed: unsubscribe and subscribe again to change them.
- `Unsubscribe` calls `HandleUnlocking` once if the lockable is locked, and nothing otherwise.
- `Lock(None)` locks nothing, `LockAllExcept(None)` and `LockAll()` lock everything.

Upgrading from 2.x: see the [CHANGELOG](src/mlock-unity-project/Packages/MLock/CHANGELOG.md).

### Check Lock Status

```csharp
if (_lockService.IsLocked(playerController))
{
    // Player is currently locked
}
```

## Debug Windows

MLock comes with built-in debug windows that provide real-time monitoring and control of your lock system directly within the Unity Editor.

### Available Debug Windows

- **Locks Debug Window**: Shows all active locks, affected objects, and lock details
- **Services Debug Window**: Displays registered lock services and their status

### Opening Debug Windows

1. In Unity, go to `Window > MLock > Locks Debug` or `Window > MLock > Services Debug`

### Registering Services for Debugging

For the debug windows to work, you need to register your lock services:

```csharp
// Using extension method (recommended)
var lockService = new BaseLockService<MyLockTags>().WithDebug();

// Unregister when no longer needed
lockService.WithoutDebug();
```

### Debug Window Features

- **Real-time monitoring** of all active locks in your game
- **Search functionality** to filter locks by lockables, tags, or other criteria
- **Unlock buttons** to release specific locks during gameplay
- **Unlock All** to quickly reset all locks in the system
- **Auto-refresh** to keep the display updated with the latest information

### Debug Window Benefits

- **Troubleshooting**: Quickly identify which locks are active when unexpected behavior occurs
- **Development**: Test locking/unlocking features without modifying code
- **QA**: Verify lock system behavior and relationships between game objects
- **Performance Monitoring**: See how many locks are active at any given time

Note: Debug windows only work in the Unity Editor and have no impact on your game builds.

## Architecture

The MLock system is built around these core components:

- **ILockService**: Manages lockable objects and locks
- **LockHandle**: A handle to an active lock; disposing it releases the lock
- **ILockable**: Interface for objects that can be locked
- **Slot tables**: Pre-sized storage, so locking and subscribing allocate nothing after warm-up

## Best Practices

- Use power-of-two values for enum tags to support bitwise operations
- Dispose locks when they're no longer needed
- Consider using a dependency injection system to provide lock services
- Group related features under single tags for easier management

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contact

Yuri Sokolov - [GitHub](https://github.com/migus88) 