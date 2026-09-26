using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using algorithm.constraint;

namespace algorithm.solver
{
    public enum ReductionFunction
    {
        linear,
        geometric,
        slowDecrease
    }

    public class SimulatedAnnealing
    {
        private readonly double _alpha;
        private readonly double _beta;

        private readonly Action _decrementRule;
        private readonly Func<IEnumerable<int>, double> _evaluate;
        private readonly double _finalTemp;
        private readonly int _iterationPerTemp;

        private readonly Func<
            IEnumerable<int>,
            IList<int?>
        > _neighborhoodSelector;

        private readonly Random _random;
        private int[] _bestSuccessors;
        private double _bestCost;
        private double _currTemp;

        private int[] _successors;
        private double _cost;
        private int[] _predecessors;

        /// <param name="initialSolution"></param>
        /// <param name="solutionEvaluator">The cost of a solution given as successors</param>
        /// <param name="neighborhoodSelector">The successors that may change, null for fixed ones</param>
        /// <param name="initialTemp"></param>
        /// <param name="finalTemp">Must be positive</param>
        /// <param name="tempReduction"></param>
        /// <param name="iterationPerTemp"></param>
        /// <param name="alpha">linear: the step (&gt; 0), geometric: the factor (between 0 and 1)</param>
        /// <param name="beta">slowDecrease: T = T / (1 + beta * T) (&gt; 0)</param>
        /// <param name="seed">Seed for the random number generator</param>
        public SimulatedAnnealing(
            Circuit initialSolution,
            Func<IEnumerable<int>, double> solutionEvaluator,
            Func<IEnumerable<int>, IList<int?>> neighborhoodSelector,
            double initialTemp = 10,
            double finalTemp = 1,
            ReductionFunction tempReduction = ReductionFunction.geometric,
            int iterationPerTemp = 100,
            double alpha = 0.9,
            double beta = 0.01,
            int seed = 1
        )
        {
            if (finalTemp <= 0)
                throw new ArgumentOutOfRangeException(nameof(finalTemp));
            if (iterationPerTemp < 1)
                throw new ArgumentOutOfRangeException(nameof(iterationPerTemp));

            _successors = initialSolution.Successors.ToArray();
            _predecessors = _successors.Predecessors();
            _evaluate = solutionEvaluator;
            _cost = _evaluate(_successors);
            _bestSuccessors = _successors;
            _bestCost = _cost;
            _currTemp = initialTemp;
            _finalTemp = finalTemp;
            _iterationPerTemp = iterationPerTemp;
            _alpha = alpha;
            _beta = beta;
            _neighborhoodSelector = neighborhoodSelector;
            _random = new Random(seed);

            _decrementRule = tempReduction switch
            {
                ReductionFunction.linear when alpha > 0 => LinearTempReduction,
                ReductionFunction.geometric when alpha is > 0 and < 1 => GeometricTempReduction,
                ReductionFunction.slowDecrease when beta > 0 => SlowDecreaseTempReduction,
                ReductionFunction.linear or ReductionFunction.geometric =>
                    throw new ArgumentOutOfRangeException(nameof(alpha)),
                ReductionFunction.slowDecrease => throw new ArgumentOutOfRangeException(nameof(beta)),
                _ => throw new ArgumentOutOfRangeException(nameof(tempReduction))
            };
        }

        private void LinearTempReduction() => _currTemp -= _alpha;

        private void GeometricTempReduction() => _currTemp *= _alpha;

        private void SlowDecreaseTempReduction() => _currTemp /= 1 + _beta * _currTemp;

        public double BestCost => _bestCost;

        public Circuit Run()
        {
            while (_currTemp > _finalTemp)
            {
                for (int i = 0; i < _iterationPerTemp; i++)
                    if (!Step())
                        return new Circuit(_bestSuccessors);
                _decrementRule();
            }

            return new Circuit(_bestSuccessors);
        }

        /// <summary>
        /// Try one move from the neighborhood of the current solution
        /// </summary>
        /// <returns>false if there are no moves left</returns>
        private bool Step()
        {
            var moves = Explorer.Mover(
                _neighborhoodSelector(_successors),
                _predecessors,
                _random);
            if (moves.Count == 0)
                return false;

            int[] candidateSolution = _successors.ToArray();
            foreach ((int i, int s) in moves)
                candidateSolution[i] = s;
            Debug.Assert(Circuit.Valid(candidateSolution));

            double candidateCost = _evaluate(candidateSolution);
            double delta = candidateCost - _cost;
            // Metropolis criterion: always accept an improvement,
            // accept a worse solution with a probability of e^(-delta/temp)
            if (delta > 0 && _random.NextDouble() >= Math.Exp(-delta / _currTemp))
                return true;

            _successors = candidateSolution;
            _cost = candidateCost;
            _predecessors = _successors.Predecessors();
            if (_cost < _bestCost)
            {
                _bestSuccessors = _successors;
                _bestCost = _cost;
            }

            return true;
        }
    }
}
