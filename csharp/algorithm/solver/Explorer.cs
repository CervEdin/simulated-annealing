using System;
using System.Collections.Generic;
using System.Linq;
using algorithm.constraint;

namespace algorithm.solver
{
    public static class Explorer
    {
        /// <summary>
        /// Relocate a random node b from a -> b -> c to between x -> y,
        /// exchanging the three edges a->b, b->c, x->y for a->c, x->b, b->y.
        /// Only the successors of nodes that are not null in the neighborhood are changed.
        /// </summary>
        /// <param name="neighborhood">The successors, null for the nodes whose successor can't change</param>
        /// <param name="predecessors">The predecessors of the successors</param>
        /// <param name="random"></param>
        /// <returns>The changed successors (node, new successor), empty if there is no possible move</returns>
        internal static IList<(int i, int s)> Mover(
            IList<int?> neighborhood,
            IList<int> predecessors,
            Random random
        )
        {
            // b and its predecessor a both get new successors
            int[] movable = Enumerable.Range(0, neighborhood.Count)
                .Where(b => neighborhood[b].HasValue && neighborhood[predecessors[b]].HasValue)
                .ToArray();
            if (movable.Length == 0)
                return Array.Empty<(int, int)>();
            int b = movable[random.Next(movable.Length)];
            int a = predecessors[b];
            int c = neighborhood[b].Value;

            // inserting b between a -> c or b -> c would recreate the current circuit
            int[] targets = Enumerable.Range(0, neighborhood.Count)
                .Where(x => neighborhood[x].HasValue && x != a && x != b)
                .ToArray();
            if (targets.Length == 0)
                return Array.Empty<(int, int)>();
            int x = targets[random.Next(targets.Length)];
            int y = neighborhood[x].Value;

            return new[] { (a, c), (x, b), (b, y) };
        }

        /// <summary>
        /// Mark the successors of the fixed nodes as null, i.e. not part of the neighborhood.
        /// </summary>
        public static List<int?> NeighborhoodSelector(
            IEnumerable<int> successors,
            IEnumerable<int> fixedIndexes
        )
        {
            var isFixed = fixedIndexes.ToHashSet();
            return successors
                .Select((s, i) => isFixed.Contains(i) ? (int?) null : s)
                .ToList();
        }

        /// <summary>
        /// The cost of visiting the nodes in order, without returning to the first.
        /// </summary>
        public static double CostObjective(
            IEnumerable<int> route,
            IList<IList<double>> m
        ) =>
            route
                .Zip(route.Skip(1))
                .Select(ss => m[ss.First][ss.Second])
                .Sum();

        /// <summary>
        /// The cost of all edges in the circuit.
        /// </summary>
        public static double CostObjective(
            this Circuit c,
            IList<IList<double>> m
        ) => c.Successors
            .Select((s, i) => m[i][s])
            .Sum();
    }
}
