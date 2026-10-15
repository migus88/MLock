using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        /// <summary>
        /// Captures the call stack outside MLock that created a lock (at most 10 frames), or null when none is found
        /// </summary>
        internal static List<DebugOrigin> CaptureOrigin()
        {
            try
            {
                var mlockAssembly = typeof(DebugUtils).Assembly;
                var st = new StackTrace(true);
                var frames = new List<DebugOrigin>(10);
                for (var i = 0; i < st.FrameCount; i++)
                {
                    var frame = st.GetFrame(i);
                    var method = frame.GetMethod();
                    var declaring = method?.DeclaringType;
                    
                    if (declaring == null || declaring.Assembly == mlockAssembly)
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

                return frames.Count > 0 ? frames : null;
            }
            catch
            {
                return null;
            }
        }

        internal static string FormatLockable(object lockable)
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