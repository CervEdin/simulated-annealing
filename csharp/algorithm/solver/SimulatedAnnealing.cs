using System;
using System.Collections.Generic;
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
        private readonly int _alpha;
        private readonly int _beta;

        private readonly Action _decrementRule;
        private readonly Func<IEnumerable<int>, double> _evaluate;
        private readonly int _finalTemp;
        private readonly int _iterationPerTemp;

        private readonly Func<
            IEnumerable<int>,
            IList<int>
        > _neighborhoodSelector;

        private readonly Random _random = new(1);
        private int[] _bestSuccessors;
        private int _currTemp;

        private int[] _successors;

        public SimulatedAnnealing(
            Circuit initialSolution,
            Func<IEnumerable<int>, double> solutionEvaluator,
            Func<IEnumerable<int>, IList<int>> neighborhoodSelector,
            int initialTemp = 10,
            int finalTemp = 1,
            ReductionFunction tempReduction = ReductionFunction.linear,
            int iterationPerTemp = 100,
            int alpha = 10,
            int beta = 5
        )
        {
            _bestSuccessors = initialSolution.Successors;
            _successors = initialSolution.Successors;
            _evaluate = solutionEvaluator;
            _currTemp = initialTemp;
            _finalTemp = finalTemp;
            _iterationPerTemp = iterationPerTemp;
            _alpha = alpha;
            _beta = beta;
            _neighborhoodSelector = neighborhoodSelector;

            _decrementRule = tempReduction switch
            {
                ReductionFunction.linear => LinearTempReduction,
                ReductionFunction.geometric => GeometricTempReduction,
                ReductionFunction.slowDecrease => SlowDecreaseTempReduction,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void LinearTempReduction() => _currTemp -= _alpha;

        private void GeometricTempReduction() => _currTemp *= 1 / _alpha;

        private void SlowDecreaseTempReduction() => _currTemp /= 1 + _beta * _currTemp;

        private bool IsTerminationCriteriaMet() =>
            _currTemp <= _finalTemp
            || !_neighborhoodSelector(_successors).Any();


        private (int a, int b) Swapper(IList<int> successors)
        {
            // pick a random to swap
            var a = _random.Next(0, successors.Count);
            // can't swap with successor (creates cycle)
            // TODO: verify
            var candidates = successors
                .Where(b => b != successors[a])
                .ToList(); // TODO: Optimize
            var b = candidates[_random.Next(0, candidates.Count)];
            return (a, b);
        }

        public Circuit Run()
        {
            while (!IsTerminationCriteriaMet())
                // iterate that number of times
                foreach (var _ in Enumerable.Range(0, _iterationPerTemp))
                {
                    // get neighbors (all successors)
                    var successors = _neighborhoodSelector(_successors);

                    //logging.debug(f's-neighbors: {neighbors}')
                    (var a, var b) = Swapper(successors);

                    var newSolution = successors
                        .Select((s, i) =>
                            i != a || i != b
                                ? s
                                : i == a
                                    ? successors[b]
                                    : successors[a])
                        .ToArray();
                    var x = newSolution[a];
                    var y = newSolution[b];
                    if (!Circuit.Valid(newSolution) || !new Circuit(newSolution).ToRoute().Valid())
                        throw new ArgumentOutOfRangeException();
                    //logging.debug(f's-new-sol: {newSolution}')
                    // get the cost between the two solutions
                    var cost = _evaluate(_successors) - _evaluate(newSolution);
                    // if the new solution is better, accept it
                    if (cost >= 0)
                    {
                        _successors = newSolution;
                        _bestSuccessors = newSolution;
                    }
                    // if the new solution is not better, accept it with a probability of e^(-cost/temp)
                    else if (_random.NextDouble() < Math.Exp(-cost / _currTemp))
                    {
                        _successors = newSolution;
                    }

                    // decrement the temperature
                    _decrementRule();
                }

            return new Circuit(_bestSuccessors);
        }

        //logging.info(f'stopping solver')
    }
}
