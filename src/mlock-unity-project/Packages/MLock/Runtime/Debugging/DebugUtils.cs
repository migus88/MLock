using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Migs.MLock.Interfaces;
using UnityEngine;

namespace Migs.MLock.Debugging
{
    /// <summary>
    /// Extension methods for MLock classes to easily enable debugging.
    /// In player builds these are no-ops to keep API calls compile-safe.
    /// </summary>
    public static class DebugUtils
    {
        public static ILockService<TLockTags> WithDebug<TLockTags>(this ILockService<TLockTags> service)
            where TLockTags : Enum
        {
            if (service is IDebuggableLockService debuggableService)
            {
                return debuggableService.WithDebug<TLockTags>();
            }

            throw new Exception($"Service is not implementing {nameof(IDebuggableLockService)}");
        }

        /// <summary>
        /// Registers this lock service with the debug system
        /// </summary>
        public static ILockService<TLockTags> WithDebug<TLockTags>(this IDebuggableLockService service)
            where TLockTags : Enum => service.WithDebug<TLockTags, ILockService<TLockTags>>();

        /// <summary>
        /// Registers this lock service with the debug system
        /// </summary>
        public static TCast WithDebug<TLockTags, TCast>(this IDebuggableLockService service)
            where TLockTags : Enum
            where TCast : class, ILockService<TLockTags>
        {
            DebugDataHandler.RegisterLockService(service);
            return service as TCast;
        }

        /// <summary>
        /// Unregisters this lock service from the debug system
        /// </summary>
        public static ILockService<TLockTags> WithoutDebug<TLockTags>(this IDebuggableLockService service)
            where TLockTags : Enum => service.WithoutDebug<TLockTags, ILockService<TLockTags>>();

        /// <summary>
        /// Unregisters this lock service from the debug system
        /// </summary>
        public static TCast WithoutDebug<TLockTags, TCast>(this IDebuggableLockService service)
            where TLockTags : Enum
            where TCast : class, ILockService<TLockTags>
        {
            DebugDataHandler.UnregisterLockService(service);
            return service as TCast;
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void Populate<TLockTags>(this List<LockDebugInfo> debugInfo, IEnumerable<ILock<TLockTags>> activeLocks, Dictionary<ILockable<TLockTags>, ILockableData<TLockTags>> lockableToDataMap) where TLockTags : Enum
        {
            foreach (var @lock in activeLocks)
            {
                var affected = lockableToDataMap
                    .Where(p => p.Value.Locks.Contains(@lock))
                    .Select(p => FormatLockable(p.Key))
                    .ToList();

                List<DebugOrigin> originFrames = null;
                
                if (@lock is IDebuggableLock<TLockTags> { DebugOriginFrames: not null } debuggableLock)
                {
                    originFrames = new List<DebugOrigin>(debuggableLock.DebugOriginFrames);
                }

                var lockInfo = new LockDebugInfo
                {
                    Id = @lock.Id,
                    LockType = typeof(TLockTags).Name,
                    IncludeTags = @lock.IncludeTags?.ToString(),
                    ExcludeTags = @lock.ExcludeTags?.ToString(),
                    OriginFrames = originFrames,
                    AffectedLockables = affected
                };
                
                debugInfo.Add(lockInfo);
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void UnlockById<TLockTags>(this IEnumerable<ILock<TLockTags>> activeLocks, int lockId) where TLockTags : Enum
        {
            var lockToRemove = activeLocks.FirstOrDefault(l => l.Id == lockId);
            lockToRemove?.Dispose();
        }

        [Conditional("UNITY_EDITOR")]
        public static void UnlockAll<TLockTags>(this IEnumerable<ILock<TLockTags>> activeLocks) where TLockTags : Enum
        {
            foreach (var @lock in activeLocks)
            {
                @lock.Dispose();
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void PopulateLockOrigin<TLockTags>(this ILock<TLockTags> @lock) where TLockTags : Enum
        {
            if (@lock is not IDebuggableLock<TLockTags> debugLock)
            {
                return;
            }
            
            try
            {
                var st = new StackTrace(true);
                var frames = new List<DebugOrigin>(10);
                for (var i = 0; i < st.FrameCount; i++)
                {
                    var frame = st.GetFrame(i);
                    var method = frame.GetMethod();
                    var declaring = method?.DeclaringType;
                    
                    if (declaring == null)
                    {
                        continue;
                    }
                    
                    var @namespace = declaring.Namespace ?? string.Empty;
                    
                    if (@namespace.StartsWith("Migs.MLock"))
                    {
                        continue;
                    }

                    var className = declaring.Name;
                    var methodName = method.Name;
                    var file = frame.GetFileName();
                    var line = frame.GetFileLineNumber();
                    var hasLocation = !string.IsNullOrEmpty(file) && line > 0;
                    var display = $"{className}.{methodName}";

                    frames.Add(new DebugOrigin
                    {
                        Display = display,
                        File = hasLocation ? file : null,
                        Line = hasLocation ? line : null
                    });

                    if (frames.Count >= 10)
                    {
                        break;
                    }
                }

                if (frames.Count > 0)
                {
                    debugLock.DebugOriginFrames ??= new List<DebugOrigin>();
                    debugLock.DebugOriginFrames.Clear();
                    
                    foreach (var f in frames)
                    {
                        debugLock.DebugOriginFrames.Add(f);
                    }

                    var first = frames[0];
                    debugLock.DebugOrigin = first.Display;
                    debugLock.DebugOriginFile = first.File;
                    debugLock.DebugOriginLine = first.Line;
                }
                else
                {
                    debugLock.DebugOrigin = "Unknown";
                    debugLock.DebugOriginFile = null;
                    debugLock.DebugOriginLine = null;
                }
            }
            catch
            {
                debugLock.DebugOrigin = "Unknown";
                debugLock.DebugOriginFile = null;
                debugLock.DebugOriginLine = null;
            }
        }

        private static string FormatLockable(object lockable)
        {
            if (lockable == null)
            {
                return "Null";
            }

            if (lockable is not Component component)
            {
                return lockable.ToString();
            }
            
            var goName = component && component.gameObject ? component.gameObject.name : null;
            var typeName = component.GetType().Name;
            
            return goName != null 
                ? $"{typeName}.{goName}" 
                : typeName;
        }
    }
}