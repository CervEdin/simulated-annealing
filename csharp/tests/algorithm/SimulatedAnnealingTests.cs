using System;
using System.Collections.Generic;
using System.Linq;
using algorithm.constraint;
using algorithm.solver;
using Xunit;

namespace tests.algorithm
{
    public class SimulatedAnnealingTests
    {
        private const int N = 12;

        // N points on a circle, the optimal circuit visits them in order
        private static readonly IList<IList<double>> Matrix = Enumerable.Range(0, N)
            .Select(i => (IList<double>) Enumerable.Range(0, N)
                .Select(j => 2 * Math.Abs(Math.Sin(Math.PI * (i - j) / N)))
                .ToArray())
            .ToArray();

        private static readonly double OptimalCost = N * 2 * Math.Sin(Math.PI / N);

        private static double Cost(IEnumerable<int> successors) => new Circuit(successors).CostObjective(Matrix);

        private static readonly Circuit Initial = new Route(new[] {0, 6, 1, 7, 2, 8, 3, 9, 4, 10, 5, 11}).ToCircuit();

        private static SimulatedAnnealing Solver(
            ReductionFunction reduction,
            double alpha = 0.9,
            double beta = 0.01,
            IEnumerable<int> fixedIndexes = null
        ) => new(
            Initial,
            Cost,
            s => Explorer.NeighborhoodSelector(s, fixedIndexes ?? Enumerable.Empty<int>()),
            initialTemp: 2,
            finalTemp: 0.01,
            tempReduction: reduction,
            iterationPerTemp: 200,
            alpha: alpha,
            beta: beta);

        [Theory]
        [InlineData(ReductionFunction.linear, 0.05)]
        [InlineData(ReductionFunction.geometric, 0.9)]
        [InlineData(ReductionFunction.slowDecrease, 0.9)]
        public void FindsTheOptimalCircuit(ReductionFunction reduction, double alpha)
        {
            var solver = Solver(reduction, alpha, beta: 0.5);
            Circuit solution = solver.Run();
            Assert.Equal(OptimalCost, Cost(solution.Successors), 6);
            Assert.Equal(OptimalCost, solver.BestCost, 6);
        }

        [Fact]
        public void NeverWorseThanTheInitialSolution()
        {
            // With every successor fixed but a few the optimum can't be reached
            var solver = Solver(ReductionFunction.geometric, fixedIndexes: Enumerable.Range(0, N - 3));
            Circuit solution = solver.Run();
            Assert.True(Cost(solution.Successors) <= Cost(Initial.Successors));
            Assert.All(Enumerable.Range(0, N - 3),
                i => Assert.Equal(Initial.Successors[i], solution.Successors[i]));
        }

        [Fact]
        public void KeepsTheBestSolutionWhileWandering()
        {
            // Start from the optimum at a temperature where nearly every move is accepted,
            // later improvements over the current solution must not replace the best one
            Circuit optimal = new Route(Enumerable.Range(0, N)).ToCircuit();
            SimulatedAnnealing solver = new(
                optimal,
                Cost,
                s => Explorer.NeighborhoodSelector(s, Enumerable.Empty<int>()),
                initialTemp: 1000,
                finalTemp: 999,
                tempReduction: ReductionFunction.linear,
                iterationPerTemp: 500,
                alpha: 1);
            Assert.Equal(optimal.Successors, solver.Run().Successors);
            Assert.Equal(OptimalCost, solver.BestCost, 6);
        }

        [Fact]
        public void StopsWithoutMoves()
        {
            var solver = Solver(ReductionFunction.geometric, fixedIndexes: Enumerable.Range(0, N));
            Assert.Equal(Initial.Successors, solver.Run().Successors);
        }

        [Fact]
        public void SameSeedSameSolution()
            => Assert.Equal(
                Solver(ReductionFunction.geometric, 0.5).Run().Successors,
                Solver(ReductionFunction.geometric, 0.5).Run().Successors);

        [Theory]
        [InlineData(ReductionFunction.linear, 0, 1)]
        [InlineData(ReductionFunction.geometric, 1, 1)]
        [InlineData(ReductionFunction.geometric, 0, 1)]
        [InlineData(ReductionFunction.slowDecrease, 0.5, 0)]
        [InlineData((ReductionFunction) 42, 0.5, 1)]
        public void RejectsSchedulesThatNeverEnd(ReductionFunction reduction, double alpha, double beta)
            => Assert.Throws<ArgumentOutOfRangeException>(() => Solver(reduction, alpha, beta));
    }
}
