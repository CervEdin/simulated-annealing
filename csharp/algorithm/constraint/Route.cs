using System;
using System.Collections.Generic;
using System.Linq;

namespace algorithm.constraint
{
    /// <summary>
    /// A route represented by the order the nodes are visited in,
    /// the last node is followed by the first.
    /// </summary>
    public class Route
    {
        public IReadOnlyList<int> List { get; }

        public Route(IEnumerable<int> ints)
        {
            int[] rr = ints.ToArray();
            if (!Valid(rr))
                throw new ArgumentOutOfRangeException(nameof(ints));
            List = Array.AsReadOnly(rr);
        }

        /// <summary>
        /// A route is valid IFF it visits every node 0..n-1 exactly once
        /// </summary>
        public static bool Valid(IReadOnlyList<int> ints)
            => ints.Count > 0
               && ints.AllDifferent()
               && ints.All(x => x >= 0 && x < ints.Count);

        public Circuit ToCircuit()
        {
            int[] circuit = new int[List.Count];
            for (int i = 0; i < List.Count; i++)
                circuit[List[i]] = List[(i + 1) % List.Count];
            return new Circuit(circuit);
        }
    }
}
