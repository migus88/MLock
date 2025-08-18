using System;

namespace Migs.MLock.Interfaces
{
    public interface ILock { }
    
    /// <summary>
    /// Interface representing a lock that can be applied to lockable objects
    /// </summary>
    /// <typeparam name="TLockTags">The enum type used for lock tags</typeparam>
    public interface ILock<out TLockTags> : ILock, IDisposable where TLockTags : Enum
    {
        /// <summary>
        /// Unique identifier for this lock
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// Tags that this lock applies to (if any)
        /// </summary>
        TLockTags IncludeTags { get; }
        
        /// <summary>
        /// Tags that this lock does not apply to (if any)
        /// </summary>
        TLockTags ExcludeTags { get; }
    }

    public interface IDebugLock<out TLockTags> : ILock<TLockTags>, IDisposable where TLockTags : Enum
    {
        string DebugOrigin { get; set; }
        string DebugOriginFile { get; set; }
        int? DebugOriginLine { get; set; }
    }
}