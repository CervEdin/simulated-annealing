using System.Collections.Generic;
using System.Linq;

namespace algorithm.constraint
{
    public static class AllDifferent
    {
        public static bool IsValid(
            this ICollection<int> ints
        )
        {
            return ints.Count == ints.ToHashSet().Count;
        }
    }

    public static class Circuit
    {
        public static bool IsValid(
            this ICollection<int> ints
        )
        {
            return ints
                .Select((x, i) => (x, i))
                .All(tp => tp.x != tp.i);
        }
    }
}

namespace algorithm
{
    public static class Constraint
    {
        public static bool AllDifferent(
            this ICollection<int> ints
        )
        {
            return ints.Count == ints.ToHashSet().Count;
        }

        public static bool Circuit(
            this ICollection<int> ints
        )
        {
            return ints
                .Select((x, i) => (x, i))
                .All(tp => tp.x != tp.i);
        }
    }
}
