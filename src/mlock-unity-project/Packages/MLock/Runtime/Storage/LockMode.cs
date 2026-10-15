#nullable enable
namespace Migs.MLock.Storage
{
    /// <summary>
    /// How a lock selects the lockables it applies to
    /// </summary>
    internal enum LockMode : byte
    {
        None = 0,
        Include = 1,
        Exclude = 2,
        All = 3
    }
}
