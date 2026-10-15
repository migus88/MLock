#nullable enable
using System;
using Migs.MLock.Interfaces;
using Migs.MLock.Storage;

namespace Migs.MLock
{
    /// <summary>
    /// Creates <see cref="LockSwitch{TLockTags}"/> instances for any <see cref="ILockService{TLockTags}"/>
    /// </summary>
    public static class LockSwitchExtensions
    {
        /// <summary>
        /// Creates a switch that, when on, locks every lockable that has at least one of the given tags.
        /// The switch starts off.
        /// </summary>
        /// <param name="service">The lock service the switch locks through</param>
        /// <param name="includeTags">Tags to include for locking</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null</exception>
        public static LockSwitch<TLockTags> CreateSwitch<TLockTags>(this ILockService<TLockTags> service, TLockTags includeTags)
            where TLockTags : Enum
        {
            return new LockSwitch<TLockTags>(service, LockMode.Include, includeTags);
        }

        /// <summary>
        /// Creates a switch that, when on, locks every lockable that has none of the given tags.
        /// The switch starts off.
        /// </summary>
        /// <param name="service">The lock service the switch locks through</param>
        /// <param name="excludeTags">Tags to exclude from locking</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null</exception>
        public static LockSwitch<TLockTags> CreateSwitchAllExcept<TLockTags>(this ILockService<TLockTags> service, TLockTags excludeTags)
            where TLockTags : Enum
        {
            return new LockSwitch<TLockTags>(service, LockMode.Exclude, excludeTags);
        }

        /// <summary>
        /// Creates a switch that, when on, locks every lockable.
        /// The switch starts off.
        /// </summary>
        /// <param name="service">The lock service the switch locks through</param>
        /// <exception cref="ArgumentNullException">Thrown if service is null</exception>
        public static LockSwitch<TLockTags> CreateSwitchAll<TLockTags>(this ILockService<TLockTags> service)
            where TLockTags : Enum
        {
            return new LockSwitch<TLockTags>(service, LockMode.All, default!);
        }
    }
}
