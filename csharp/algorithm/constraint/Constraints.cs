using System.Collections.Generic;
using System.Linq;

namespace algorithm.constraint
{
    public static class Constraints
    {
        public static bool AllDifferent(
            this ICollection<int> ints
        ) => ints.Count == ints.ToHashSet().Count;

        public static bool Circuit(
            this ICollection<int> ints
        ) => ints
            .Select((x, i) => (x, i))
            .All(tp => tp.x != tp.i);
    }
}