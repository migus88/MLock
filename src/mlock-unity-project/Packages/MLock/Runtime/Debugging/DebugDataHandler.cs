using System;
using System.Collections.Generic;
using System.Linq;
using Migs.MLock.Interfaces;
using UnityEngine;
using UnityEngine.Scripting;

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
        public static void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
        
        /// <summary>
        /// Update debug data by scanning all registered lock services
        /// </summary>
        public static void UpdateData()
        {
            if (!_isEnabled)
            {
                return;
            }
            
            // Only update at reasonable intervals
            if (Time.realtimeSinceStartup - _lastUpdateTime < 1f) return;
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            _activeLocks.Clear();
            
            foreach (var pair in _lockServices)
            {
                pair.PopulateDebugInfo(_activeLocks);
            }
        }
        
        /// <summary>
        /// Unlock a specific lock
        /// </summary>
        /// <param name="lockId">The ID of the lock to unlock</param>
        /// <returns>True if the lock was found and unlocked, false otherwise</returns>
        public static bool UnlockById(int lockId)
        {
            return _lockServices.Select(service => service.TryUnlockById(lockId)).FirstOrDefault();
        }
        
        /// <summary>
        /// Unlock all locks
        /// </summary>
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
        /// String representation of all lockables affected by this lock
        /// </summary>
        public List<string> AffectedLockables { get; set; }
    }
} 