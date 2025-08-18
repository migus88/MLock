namespace Migs.MLock.Debugging
{
    /// <summary>
    /// Represents a single frame in the origin stack for a lock.
    /// </summary>
    public class DebugOrigin
    {
        public string Display { get; set; }
        public string File { get; set; }
        public int? Line { get; set; }
    }
}