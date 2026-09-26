using System;
using System.Collections.Generic;
using System.Linq;
using algorithm.constraint;
using algorithm.solver;
using Xunit;

namespace tests.algorithm
{
    public class ExplorerTests
    {
        private static int[] RandomCircuit(int n, Random random)
            => new Route(Enumerable.Range(0, n).OrderBy(_ => random.Next()))
                .ToCircuit().Successors.ToArray();

        private static int[] Apply(int[] successors, IEnumerable<(int i, int s)> moves)
        {
            int[] candidate = successors.ToArray();
            foreach ((int i, int s) in moves)
                candidate[i] = s;
            return candidate;
        }

        [Fact]
        public void MoverRelocatesOneNode()
        {
            Random random = new(1);
            foreach (int n in new[] {3, 4, 5, 10, 50})
            foreach (int _ in Enumerable.Range(0, 200))
            {
                int[] successors = RandomCircuit(n, random);
                var moves = Explorer.Mover(
                    successors.Select(s => (int?) s).ToList(),
                    successors.Predecessors(),
                    random);

                Assert.Equal(3, moves.Count);
                int[] candidate = Apply(successors, moves);
                Assert.True(candidate.Circuit());
                // a relocation always changes the circuit
                Assert.NotEqual(successors, candidate);
            }
        }

        [Fact]
        public void MoverKeepsFixedSuccessors()
        {
            Random random = new(2);
            int[] fixedIndexes = {0, 3, 7};
            foreach (int _ in Enumerable.Range(0, 500))
            {
                int[] successors = RandomCircuit(12, random);
                var moves = Explorer.Mover(
                    Explorer.NeighborhoodSelector(successors, fixedIndexes),
                    successors.Predecessors(),
                    random);

                int[] candidate = Apply(successors, moves);
                Assert.True(candidate.Circuit());
                Assert.All(fixedIndexes, i => Assert.Equal(successors[i], candidate[i]));
            }
        }

        [Fact]
        public void MoverPicksEveryNode()
        {
            Random random = new(3);
            int[] successors = RandomCircuit(30, random);
            var neighborhood = successors.Select(s => (int?) s).ToList();
            int[] predecessors = successors.Predecessors();
            var moved = Enumerable.Range(0, 3000)
                .Select(_ => Explorer.Mover(neighborhood, predecessors, random)[2].i)
                .ToHashSet();
            Assert.Equal(30, moved.Count);
        }

        [Fact]
        public void MoverWithoutMovesIsEmpty()
        {
            int[] successors = {1, 2, 0};
            Assert.Empty(Explorer.Mover(
                Explorer.NeighborhoodSelector(successors, new[] {0, 1, 2}),
                successors.Predecessors(),
                new Random(1)));
            // only one node to insert, and it can only go back where it was
            Assert.Empty(Explorer.Mover(
                new List<int?> {1, 0},
                new[] {1, 0},
                new Random(1)));
        }

        [Fact]
        public void NeighborhoodSelectorFixesByIndex()
            => Assert.Equal(
                new int?[] {null, 2, 3, null},
                Explorer.NeighborhoodSelector(new[] {1, 2, 3, 0}, new[] {0, 3}));

        [Fact]
        public void CostObjective()
        {
            IList<IList<double>> m = new IList<double>[]
            {
                new double[] {0, 1, 5},
                new double[] {1, 0, 2},
                new double[] {5, 2, 0}
            };
            Assert.Equal(3, Explorer.CostObjective(new[] {0, 1, 2}, m));
            Assert.Equal(8, new Circuit(new[] {1, 2, 0}).CostObjective(m));
        }
    }
}
