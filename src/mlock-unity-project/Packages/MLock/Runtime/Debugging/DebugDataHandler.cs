#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using Migs.MLock.Interfaces;
using UnityEngine;

namespace Migs.MLock.Debugging
{
    /// <summary>
    /// Static class that holds debug data for MLock system
    /// Follows the Single Responsibility principle by only handling debug data collection and storage
    /// </summary>
    public static class DebugDataHandler
    {
        public static IReadOnlyList<LockDebugInfo> ActiveLocks => _activeLocks;
        public static IReadOnlyCollection<IDebuggableLockService> LockServices => _lockServices;
        /// <summary>
        /// True while debug data collection is on: while at least one debug window is open, or after <see cref="SetEnabled"/> turned it on
        /// </summary>
        public static bool IsEnabled => _isEnabled || _windowCount > 0;
        
        // Dictionary to store all lock services by type
        private static readonly HashSet<IDebuggableLockService> _lockServices = new();
        
        // Lock data for the editor window
        private static readonly List<LockDebugInfo> _activeLocks = new();
        
        // Lock data time cache
        private static double _lastUpdateTime;
        
        // Is debug data collection enabled
        private static bool _isEnabled;
        
        // Number of open debug windows that need debug data collection
        private static int _windowCount;
        
        /// <summary>
        /// Register a lock service for debugging
        /// </summary>
        /// <param name="service">The lock service to register</param>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterLockService(IDebuggableLockService? service)
        {
            if (service == null)
            {
                return;
            }
            
            _lockServices.Add(service);
        }
        
        /// <summary>
        /// Unregister a lock service
        /// </summary>
        /// <param name="service">The lock service to unregister</param>
        /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
        [Conditional("UNITY_EDITOR")]
        public static void UnregisterLockService(IDebuggableLockService? service)
        {
            if (service == null)
            {
                return;
            }

            _lockServices.Remove(service);
        }
        
        /// <summary>
        /// Enable or disable debug data collection
        /// </summary>
        /// <param name="enabled">Whether to enable debug data collection</param>
        [Conditional("UNITY_EDITOR")]
        public static void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
        
        /// <summary>
        /// Called by a debug window when it opens. Collection stays on while at least one window has retained it
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        internal static void RetainCapture()
        {
            _windowCount++;
        }
        
        /// <summary>
        /// Called by a debug window when it closes. Collection stops once no window retains it, unless <see cref="SetEnabled"/> turned it on
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        internal static void ReleaseCapture()
        {
            if (_windowCount > 0)
            {
                _windowCount--;
            }
        }
        
        /// <summary>
        /// Update debug data by scanning all registered lock services
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void UpdateData()
        {
            if (!IsEnabled || Time.realtimeSinceStartup - _lastUpdateTime < 1f)
            {
                return;
            }
            
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            _activeLocks.Clear();
            
            foreach (var pair in _lockServices)
            {
                pair.PopulateDebugInfo(_activeLocks);
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void UnlockById(int lockId)
        {
            foreach (var service in new List<IDebuggableLockService>(_lockServices))
            {
                service.UnlockById(lockId);
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void UnlockAll()
        {
            foreach (var service in new List<IDebuggableLockService>(_lockServices))
            {
                service.UnlockAll();
            }
        }

        /// <summary>
        /// Clears the registered services and the cached lock data.
        /// Called when Play mode starts and when it exits, so services from earlier sessions are never listed.
        /// </summary>
        internal static void Reset()
        {
            _lockServices.Clear();
            _activeLocks.Clear();
            _lastUpdateTime = double.NegativeInfinity;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            Reset();
        }
    }
    
    /// <summary>
    /// Debug information for a lock
    /// </summary>
    public class LockDebugInfo
    {
        /// <summary>
        /// The Id of the lock
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// String representation of the lock type
        /// </summary>
        public string LockType { get; set; } = string.Empty;
        /// <summary>
        /// How the lock selects lockables: "Include", "Exclude" or "All"
        /// </summary>
        public string Mode { get; set; } = string.Empty;
        /// <summary>
        /// String representation of all include tags
        /// </summary>
        public string? IncludeTags { get; set; }
        /// <summary>
        /// String representation of all exclude tags
        /// </summary>
        public string? ExcludeTags { get; set; }
        /// <summary>
        /// Full origin frames (filtered stack trace). First frame corresponds to Origin/OriginFile/OriginLine.
        /// </summary>
        public List<DebugOrigin>? OriginFrames { get; set; }
        /// <summary>
        /// String representation of all lockables affected by this lock
        /// </summary>
        public List<string> AffectedLockables { get; set; } = new();
    }
}