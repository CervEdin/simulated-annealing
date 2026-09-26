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

    public enum PenaltyUpdate
    {
        constant,
        cooling
    }

    public class SimulatedAnnealing
    {
        private readonly double _alpha;
        private readonly double _beta;

        private readonly Action _decrementRule;
        private readonly Func<IEnumerable<int>, (double cost, double violation)> _evaluate;
        private readonly double _finalTemp;
        private readonly double _initialTemp;
        private readonly int _iterationPerTemp;

        private readonly Func<
            IEnumerable<int>,
            IList<int?>
        > _neighborhoodSelector;

        private readonly double _initialPenalty;
        private readonly PenaltyUpdate _penaltyUpdate;

        private readonly Random _random;
        private int[] _bestSuccessors;
        private double _bestCost;
        private double _bestViolation;
        private double _currTemp;
        private double _penalty;

        private int[] _successors;
        private double _cost;
        private double _violation;
        private int[] _predecessors;

        /// <param name="initialSolution"></param>
        /// <param name="solutionEvaluator">
        /// The cost of a solution given as successors, and how much it violates the constraints (&gt;= 0), 0 if it is
        /// feasible.
        /// Annealing moves on the cost plus the penalty times the violation.
        /// </param>
        /// <param name="neighborhoodSelector">The successors that may change, null for fixed ones</param>
        /// <param name="initialTemp"></param>
        /// <param name="finalTemp">Must be positive</param>
        /// <param name="tempReduction"></param>
        /// <param name="iterationPerTemp"></param>
        /// <param name="alpha">linear: the step (&gt; 0), geometric: the factor (between 0 and 1)</param>
        /// <param name="beta">slowDecrease: T = T / (1 + beta * T) (&gt; 0)</param>
        /// <param name="seed">Seed for the random number generator</param>
        /// <param name="penalty">The initial penalty per unit of violation (&gt; 0)</param>
        /// <param name="penaltyUpdate">
        /// constant: keep the penalty, cooling: multiply it by initialTemp / temp at each temperature,
        /// so the constraints tighten as the search cools
        /// </param>
        public SimulatedAnnealing(
            Circuit initialSolution,
            Func<IEnumerable<int>, (double cost, double violation)> solutionEvaluator,
            Func<IEnumerable<int>, IList<int?>> neighborhoodSelector,
            double initialTemp = 10,
            double finalTemp = 1,
            ReductionFunction tempReduction = ReductionFunction.geometric,
            int iterationPerTemp = 100,
            double alpha = 0.9,
            double beta = 0.01,
            int seed = 1,
            double penalty = 1,
            PenaltyUpdate penaltyUpdate = PenaltyUpdate.constant
        )
        {
            if (finalTemp <= 0)
                throw new ArgumentOutOfRangeException(nameof(finalTemp));
            if (iterationPerTemp < 1)
                throw new ArgumentOutOfRangeException(nameof(iterationPerTemp));
            if (penalty <= 0)
                throw new ArgumentOutOfRangeException(nameof(penalty));
            if (!Enum.IsDefined(penaltyUpdate))
                throw new ArgumentOutOfRangeException(nameof(penaltyUpdate));

            _successors = initialSolution.Successors.ToArray();
            _predecessors = _successors.Predecessors();
            _evaluate = solutionEvaluator;
            (_cost, _violation) = _evaluate(_successors);
            _bestSuccessors = _successors;
            _bestCost = _cost;
            _bestViolation = _violation;
            _currTemp = initialTemp;
            _initialTemp = initialTemp;
            _finalTemp = finalTemp;
            _iterationPerTemp = iterationPerTemp;
            _alpha = alpha;
            _beta = beta;
            _neighborhoodSelector = neighborhoodSelector;
            _random = new Random(seed);
            _initialPenalty = penalty;
            _penalty = penalty;
            _penaltyUpdate = penaltyUpdate;

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

        /// <summary>
        /// The cost of the best solution, without the penalty
        /// </summary>
        public double BestCost => _bestCost;

        /// <summary>
        /// Whether the best solution is feasible, if not it is the least violating one found
        /// </summary>
        public bool FoundFeasible => _bestViolation == 0;

        /// <summary>
        /// The current penalty per unit of violation
        /// </summary>
        public double Penalty => _penalty;

        public Circuit Run()
        {
            while (_currTemp > _finalTemp)
            {
                for (int i = 0; i < _iterationPerTemp; i++)
                    if (!Step())
                        return new Circuit(_bestSuccessors);
                _decrementRule();
                if (_penaltyUpdate == PenaltyUpdate.cooling)
                    _penalty = _initialPenalty * _initialTemp / _currTemp;
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

            (double candidateCost, double candidateViolation) = _evaluate(candidateSolution);
            double delta = candidateCost - _cost + _penalty * (candidateViolation - _violation);
            // Metropolis criterion: always accept an improvement,
            // accept a worse solution with a probability of e^(-delta/temp)
            if (delta > 0 && _random.NextDouble() >= Math.Exp(-delta / _currTemp))
                return true;

            _successors = candidateSolution;
            _cost = candidateCost;
            _violation = candidateViolation;
            _predecessors = _successors.Predecessors();
            UpdateBest();
            return true;
        }

        /// <summary>
        /// Keep the current solution if it is the least violating, then the cheapest, so far.
        /// Once one is feasible the best is the cheapest feasible solution.
        /// </summary>
        private void UpdateBest()
        {
            if (_violation > _bestViolation || _violation == _bestViolation && _cost >= _bestCost)
                return;
            _bestSuccessors = _successors;
            _bestCost = _cost;
            _bestViolation = _violation;
        }
    }
}
