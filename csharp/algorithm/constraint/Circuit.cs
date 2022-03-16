using System;
using System.Collections.Generic;
using System.Linq;

namespace algorithm.constraint
{
    public class Circuit
    {
        public readonly int[] Successors;

        public Circuit(IEnumerable<int> successors)
        {
            int[] ss = successors.ToArray();
            if (!Valid(ss))
                throw new ArgumentOutOfRangeException(nameof(successors));
            Successors = ss.ToArray();
        }

        public static bool Valid(IList<int> circuit)
            => circuit.Circuit()
               // TODO: Needed? Circuit implies AllDifferent
               && circuit.AllDifferent();

        // TODO: test
        public Route ToRoute()
        {
            int[] rr = new int[Successors.Length];

            int i = 0;
            int current = i;
            int successor = Successors[current];
            while (i < rr.Length)
            {
                rr[i] = current;
                current = successor;
                successor = Successors[current];
                i++;
            }

            return new Route(rr);
        }

        public static bool Valid(int[] circuit) => circuit.AllDifferent() && circuit.Circuit();
    }

    public static class CircuitHelper
    {
        internal static IEnumerable<int> Predecessors(this IList<int> successors)
            => successors
                .Select((succ, current) => successors.IndexOf(current));
    }
}
