using System;
using System.Collections.Generic;
using System.Linq;

namespace algorithm.constraint
{
    /// <summary>
    /// A hamiltonian circuit represented by successors,
    /// Successors[i] is the node visited after node i.
    /// </summary>
    public class Circuit
    {
        public IReadOnlyList<int> Successors { get; }

        public Circuit(IEnumerable<int> successors)
        {
            int[] ss = successors.ToArray();
            if (!Valid(ss))
                throw new ArgumentOutOfRangeException(nameof(successors));
            Successors = Array.AsReadOnly(ss);
        }

        public static bool Valid(IReadOnlyList<int> circuit) => circuit.Circuit();

        /// <summary>
        /// The order the circuit visits the nodes in, starting from node 0
        /// </summary>
        public Route ToRoute()
        {
            int[] rr = new int[Successors.Count];
            for (int i = 0, current = 0; i < rr.Length; i++, current = Successors[current])
                rr[i] = current;
            return new Route(rr);
        }
    }

    public static class CircuitHelper
    {
        internal static int[] Predecessors(this IReadOnlyList<int> successors)
        {
            int[] predecessors = new int[successors.Count];
            for (int current = 0; current < successors.Count; current++)
                predecessors[successors[current]] = current;
            return predecessors;
        }
    }
}
