using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using algorithm.constraint;

namespace algorithm.solver
{
    public static class Explorer
    {
        /// <summary>
        /// Pick one edge to change
        /// </summary>
        /// <param name="successors"></param>
        /// <returns></returns>
        internal static (int c, int s) EdgePicker(IList<int?> successors, Random _random)
        {
            Debug.Assert(successors.Any(x => x != null)); // Neighborhood can't be empty
            // pick a random to swap
            var arr = successors
                .Select((c, s) => (c, s))
                .Where(tp => tp.c != null);
            // TODO: >= or >
            var a = arr.FirstOrDefault(_ => _random.NextDouble() >= 0.5);
            a = a.c != null ? a : arr.First();
            return ((int c, int s)) a;
        }

        private static Stack<T> Permutate<T>(this Stack<T> stack) => new Stack<T>(stack.Skip(1).Append(stack.Pop()));

        // TODO tidy
        internal static IEnumerable<(int i, int s)> Mover(
            IList<int?> successors,
            IList<int> predecessors,
            Random _random
        )
        {
            // S:1
            (int current, int succ) = EdgePicker(successors, _random);
            int succ_succ = successors[succ].Value;
            // can't swap with successor (creates cycle)
            // TODO: verify
            var candidates = successors
                .Where(b => b != succ)
                .Where(b => b.HasValue)
                .Select(b => b.Value)
                .ToList(); // TODO: Optimize
            // Pick a random candidate  S:1 -> S:2
            int new_succ = candidates[_random.Next(0, candidates.Count)]; // 2
            int pred = predecessors[current];

            var tuples = new ( int i, int s, ISet<int> canidates)[]
            {
                (current, succ, new HashSet<int>(1) {new_succ}),
                (succ, succ_succ, successors
                    .Where(x => x.HasValue).Select(x => x.Value)
                    .Where(x => x != succ)
                    .Where(x => x != new_succ)
                    .Where(x => x != succ_succ) // 
                    .ToHashSet()
                ),
                (pred, current, successors
                    .Where(x => x.HasValue).Select(x => x.Value)
                    .Where(x => x != pred)
                    .Where(x => x != new_succ)
                    .Where(x => x != current)
                    .ToHashSet()
                )
            };

            var moves = NewEdgePicker(tuples);
            return moves.Select(tp => (tp.i, tp.canidates.Single()));
        }


        internal static IList<(int i, int s, ISet<int> canidates)> NewEdgePicker(
            IList<(int i, int s, ISet<int> canidates)> list,
            int i = 0
        )
        {
            if (list.All(s => s.canidates.Count == 1))
                return list;
            if (i >= list.Count)
                i = 0;
            var current = list[i];
            if (current.canidates.Count == 1)
                return NewEdgePicker(list, i + 1);
            var toRemove = list
                .Select(tp =>
                    tp.canidates.Count > 1
                        ? new[] {tp.i}
                        : new[] {tp.i, tp.canidates.First()})
                .ToHashSet();
            return NewEdgePicker(list);
        }

        internal static (
            (int i, int s, ISet<int> canidates),
            (int i, int s, ISet<int> canidates),
            (int i, int s, ISet<int> canidates)
            ) NewEdgePicker(
                (int i, int s, ISet<int> canidates) one,
                (int i, int s, ISet<int> canidates) two,
                (int i, int s, ISet<int> canidates) three
            )
        {
            if (one.canidates.Count == 1 && two.canidates.Count == 1 && three.canidates.Count == 1)
                return (three, one, two);
            if (one.canidates.Count == 1) return NewEdgePicker(three, one, two);

            if (two.canidates.Count == 1)
                one.canidates.Remove(two.canidates.First());
            if (three.canidates.Count == 1)
                one.canidates.Remove(three.canidates.First());
            one.canidates.Remove(two.i);
            one.canidates.Remove(three.i);
            return NewEdgePicker(three, one, two);
        }

        public static List<int?> NeighborhoodSelector(
            IEnumerable<int> enumerable,
            IEnumerable<int> depotIndexes
        ) =>
            enumerable
                .Select(x =>
                    // Never change vehicle successors
                    depotIndexes.Contains(x)
                        ? (int?) null
                        : x)
                // We only really never CANT change End depot successors
                // but for now we exclude all
                .ToList();

        public static double CostObjective(
            ICollection<int> route,
            IList<IList<double>> m
        ) =>
            route
                .Zip(route.Skip(1))
                .Select(ss => m[ss.First][ss.Second])
                .Sum();

        public static double CostObjective(
            this Circuit c,
            IList<IList<double>> m
        ) => CostObjective(c.ToRoute().List, m);
    }
}