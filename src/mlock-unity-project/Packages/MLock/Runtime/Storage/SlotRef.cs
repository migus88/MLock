#nullable enable

namespace Migs.MLock.Storage
{
    /// <summary>
    /// A reference to a slot of a <see cref="SlotTable{TEntry}"/> together with the generation it was taken at
    /// </summary>
    internal readonly struct SlotRef
    {
        public readonly int Slot;
        public readonly int Generation;

        public SlotRef(int slot, int generation)
        {
            Slot = slot;
            Generation = generation;
        }
    }
}
