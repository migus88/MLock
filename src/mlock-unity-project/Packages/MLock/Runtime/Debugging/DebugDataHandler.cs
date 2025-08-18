using System.Collections.Generic;
using System.Diagnostics;
using Migs.MLock.Interfaces;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        
        // Dictionary to store all lock services by type
        private static readonly HashSet<IDebuggableLockService> _lockServices = new();
        
        // Lock data for the editor window
        private static readonly List<LockDebugInfo> _activeLocks = new();
        
        // Lock data time cache
        private static double _lastUpdateTime;
        
        // Is debug data collection enabled
        private static bool _isEnabled;
        
        /// <summary>
        /// Register a lock service for debugging
        /// </summary>
        /// <param name="service">The lock service to register</param>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterLockService(IDebuggableLockService service)
        {
            if (service == null)
            {
                return;
            }
            
            if (_lockServices.Add(service))
            {
                Debug.Log($"[MLock Debug] Registered lock service for {service.GetType().Name}");
            }
        }
        
        /// <summary>
        /// Unregister a lock service
        /// </summary>
        /// <param name="service">The lock service to unregister</param>
        /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
        [Conditional("UNITY_EDITOR")]
        public static void UnregisterLockService(IDebuggableLockService service)
        {
            if (service == null)
            {
                return;
            }

            if (!_lockServices.Contains(service))
            {
                return;
            }
            
            _lockServices.Remove(service);
            Debug.Log($"[MLock Debug] Unregistered lock service for {service.GetType().Name}");
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
        /// Update debug data by scanning all registered lock services
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void UpdateData()
        {
            if (!Application.isPlaying && (ActiveLocks.Count > 0 || LockServices.Count > 0))
            {
                _activeLocks.Clear();
                _lockServices.Clear();
            }
            
            if (!_isEnabled || Time.realtimeSinceStartup - _lastUpdateTime < 1f)
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
            foreach (var service in _lockServices)
            {
                service.UnlockById(lockId);
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void UnlockAll()
        {
            foreach (var service in _lockServices)
            {
                service.UnlockAll();
            }
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
        public string LockType { get; set; }
        /// <summary>
        /// String representation of all include tags
        /// </summary>
        public string IncludeTags { get; set; }
        /// <summary>
        /// String representation of all exclude tags
        /// </summary>
        public string ExcludeTags { get; set; }
        /// <summary>
        /// Where the lock was created (class/method)
        /// </summary>
        public string Origin { get; set; }
        public string OriginFile { get; set; }
        public int? OriginLine { get; set; }
        /// <summary>
        /// String representation of all lockables affected by this lock
        /// </summary>
        public List<string> AffectedLockables { get; set; }
    }
}