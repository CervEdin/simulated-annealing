using System.Collections.Generic;
using System.Linq;

namespace algorithm.constraint
{
    public static class Constraints
    {
        /// <summary>
        /// The AllDifferent constraint is true IFF
        /// all values in the list are different.
        /// </summary>
        /// <param name="ints"></param>
        /// <returns></returns>
        public static bool AllDifferent(
            this IReadOnlyCollection<int> ints
        ) => ints.Count == ints.ToHashSet().Count;

        /// <summary>
        /// The Circuit constraint is true IFF
        /// the list represents a hamiltonian circuit,
        /// where ints[i] is the successor of node i.
        /// (implies AllDifferent)
        /// </summary>
        /// <param name="ints"></param>
        /// <returns></returns>
        public static bool Circuit(
            this IReadOnlyList<int> ints
        )
        {
            if (ints.Count == 0 || ints.Any(x => x < 0 || x >= ints.Count))
                return false;
            // Follow the successors from 0, a hamiltonian circuit only
            // returns to 0 after visiting every other node exactly once
            int current = 0;
            for (int visited = 1; visited < ints.Count; visited++)
            {
                current = ints[current];
                if (current == 0)
                    return false;
            }

            return ints[current] == 0;
        }
    }
}
