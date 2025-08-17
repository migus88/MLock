using System;
using Migs.MLock.Interfaces;

namespace Migs.MLock.Debugging
{
    /// <summary>
    /// Extension methods for MLock classes to easily enable debugging
    /// Follows Open/Closed principle by extending functionality without modifying original classes
    /// </summary>
    public static class DebugExtensions
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
        /// <param name="service">The lock service to register</param>
        /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
        /// <returns>The lock service for method chaining</returns>
        public static ILockService<TLockTags> WithDebug<TLockTags>(this IDebuggableLockService service)
            where TLockTags : Enum => service.WithDebug<TLockTags, ILockService<TLockTags>>();
        
        /// <summary>
        /// Registers this lock service with the debug system
        /// </summary>
        /// <param name="service">The lock service to register</param>
        /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
        /// <typeparam name="TCast">Type to cast the lock service into</typeparam>
        /// <returns>The lock service for method chaining</returns>
        public static TCast WithDebug<TLockTags, TCast>(this IDebuggableLockService service)
            where TLockTags : Enum 
            where TCast : class, ILockService<TLockTags>
        {
#if UNITY_EDITOR
            DebugDataHandler.RegisterLockService(service);
#endif

            return service as TCast;
        }
        
        /// <summary>
        /// Unregisters this lock service from the debug system
        /// </summary>
        /// <param name="service">The lock service to unregister</param>
        /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
        /// <returns>The lock service for method chaining</returns>
        public static ILockService<TLockTags> WithoutDebug<TLockTags>(this IDebuggableLockService service)
            where TLockTags : Enum => service.WithoutDebug<TLockTags, ILockService<TLockTags>>();

        /// <summary>
        /// Unregisters this lock service from the debug system
        /// </summary>
        /// <param name="service">The lock service to unregister</param>
        /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
        /// <typeparam name="TCast">Type to cast the lock service into</typeparam>
        /// <returns>The lock service for method chaining</returns>
        public static TCast WithoutDebug<TLockTags, TCast>(this IDebuggableLockService service)
            where TLockTags : Enum 
            where TCast : class, ILockService<TLockTags>
        {
#if UNITY_EDITOR
            DebugDataHandler.UnregisterLockService(service);
#endif

            return service as TCast;
        }
    }
}