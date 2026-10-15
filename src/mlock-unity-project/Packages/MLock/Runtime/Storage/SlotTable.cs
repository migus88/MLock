#nullable enable
using System;

namespace Migs.MLock.Storage
{
    /// <summary>
    /// A table of entries addressed by stable slot indices with generation counters.
    /// Removed slots are reused, and their generation is bumped so that stale references never match.
    /// Active slots are also kept in a dense list for allocation-free iteration.
    /// Growing the arrays is the only allocation.
    /// </summary>
    internal sealed class SlotTable<TEntry> where TEntry : struct
    {
        private const int FirstGeneration = 1;

        private TEntry[] _entries;
        private int[] _generations;
        private int[] _freeSlots;
        private int[] _dense;
        private int[] _denseIndexBySlot;
        private int _freeCount;
        private int _usedSlots;
        private int _count;

        /// <summary>
        /// Number of active entries
        /// </summary>
        public int Count => _count;

        public SlotTable(int capacity)
        {
            if (capacity < 1)
            {
                capacity = 1;
            }

            _entries = new TEntry[capacity];
            _generations = new int[capacity];
            _freeSlots = new int[capacity];
            _dense = new int[capacity];
            _denseIndexBySlot = new int[capacity];

            InitializeSlots(0, capacity);
        }

        /// <summary>
        /// Adds an entry and returns its slot together with the slot's current generation
        /// </summary>
        public int Add(in TEntry entry, out int generation)
        {
            int slot;

            if (_freeCount > 0)
            {
                slot = _freeSlots[--_freeCount];
            }
            else
            {
                if (_usedSlots == _entries.Length)
                {
                    Grow();
                }

                slot = _usedSlots++;
            }

            _entries[slot] = entry;
            _dense[_count] = slot;
            _denseIndexBySlot[slot] = _count;
            _count++;

            generation = _generations[slot];
            return slot;
        }

        /// <summary>
        /// Whether the slot holds an active entry of the given generation
        /// </summary>
        public bool IsAlive(int slot, int generation)
        {
            return slot >= 0
                   && slot < _usedSlots
                   && _denseIndexBySlot[slot] >= 0
                   && _generations[slot] == generation;
        }

        /// <summary>
        /// Removes the entry if the slot is alive with the given generation.
        /// The slot's generation is bumped, so every outstanding reference to it becomes stale.
        /// </summary>
        public bool TryRemove(int slot, int generation, out TEntry removed)
        {
            if (!IsAlive(slot, generation))
            {
                removed = default;
                return false;
            }

            removed = _entries[slot];
            _entries[slot] = default;

            var denseIndex = _denseIndexBySlot[slot];
            var lastIndex = _count - 1;
            var lastSlot = _dense[lastIndex];

            _dense[denseIndex] = lastSlot;
            _denseIndexBySlot[lastSlot] = denseIndex;
            _denseIndexBySlot[slot] = -1;
            _count--;

            var nextGeneration = unchecked(_generations[slot] + 1);
            _generations[slot] = nextGeneration == 0 ? FirstGeneration : nextGeneration;

            _freeSlots[_freeCount++] = slot;
            return true;
        }

        /// <summary>
        /// Returns a reference to the entry stored in the slot
        /// </summary>
        public ref TEntry Get(int slot) => ref _entries[slot];

        /// <summary>
        /// Returns the current generation of the slot
        /// </summary>
        public int GenerationOf(int slot) => _generations[slot];

        /// <summary>
        /// Returns the slot stored at the given index of the dense list of active entries
        /// </summary>
        public int SlotAt(int denseIndex) => _dense[denseIndex];

        private void Grow()
        {
            var oldCapacity = _entries.Length;
            var newCapacity = oldCapacity * 2;

            Array.Resize(ref _entries, newCapacity);
            Array.Resize(ref _generations, newCapacity);
            Array.Resize(ref _freeSlots, newCapacity);
            Array.Resize(ref _dense, newCapacity);
            Array.Resize(ref _denseIndexBySlot, newCapacity);

            InitializeSlots(oldCapacity, newCapacity);
        }

        private void InitializeSlots(int from, int to)
        {
            for (var i = from; i < to; i++)
            {
                _generations[i] = FirstGeneration;
                _denseIndexBySlot[i] = -1;
            }
        }
    }
}
