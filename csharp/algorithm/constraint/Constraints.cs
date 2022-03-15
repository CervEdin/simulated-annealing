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
            this ICollection<int> ints
        ) => ints.Count == ints.ToHashSet().Count;

        /// <summary>
        /// The Circuit constraint is true IFF
        /// the list represents a hamiltonian circuit.
        /// (implies AllDifferent)
        /// </summary>
        /// <param name="ints"></param>
        /// <returns></returns>
        public static bool Circuit(
            this ICollection<int> ints
        ) => ints
            .Select((x, i) => (x, i))
            .All(tp => tp.x != tp.i);
    }
}