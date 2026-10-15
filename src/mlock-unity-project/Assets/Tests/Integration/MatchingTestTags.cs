using System;

namespace Migs.MLock.Tests.Integration
{
    public enum SignedTestTags
    {
        None = 0,
        Tag1 = 1,
        Negative = -8,
        All = ~0
    }

    [Flags]
    public enum WideTestTags : ulong
    {
        None = 0,
        Bit8 = 1UL << 8,
        Bit40 = 1UL << 40
    }

    [Flags]
    public enum ByteTestTags : byte
    {
        None = 0,
        Tag1 = 1 << 0,
        Tag2 = 1 << 1
    }
}
