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
            IList<int?>
        > _neighborhoodSelector;

        private readonly Random _random = new(1);
        private int[] _bestSuccessors;
        private int _currTemp;

        private int[] _successors;

        public SimulatedAnnealing(
            Circuit initialSolution,
            Func<IEnumerable<int>, double> solutionEvaluator,
            Func<IEnumerable<int>, IList<int?>> neighborhoodSelector,
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
        
        private (int c, int s) Picker(IList<int?> successors)
        {
            Debug.Assert(successors.Any(x => x != null));
            // pick a random to swap
            var arr = successors
                .Select((c, s) => (c, s))
                .Where(tp => tp.c != null);
            // TODO: >= or >
            var a = arr.FirstOrDefault(_ => _random.NextDouble() >= 0.5);
            a = a.c != null ? a : arr.First();
            return ((int c, int s))a;
        }

        private (int a, int b) Mover(IList<int?> successors)
        {
            (int c, int s) = Picker(successors);
            // can't swap with successor (creates cycle)
            // TODO: verify
            var candidates = successors
                .Where(b => b != s)
                .Where(b => b != null)
                .ToList(); // TODO: Optimize
            var b = candidates[_random.Next(0, candidates.Count)];
            return (c, b ?? throw new NullReferenceException());
        }

        public Circuit Run()
        {
            while (!IsTerminationCriteriaMet())
                // iterate that number of times
                foreach (var _ in Enumerable.Range(0, _iterationPerTemp))
                {
                    // get neighbors (all successors)
                    IList<int?> neighborhood = _neighborhoodSelector(_successors);

                    //logging.debug(f's-neighbors: {neighbors}')
                    (var a, var b) = Mover(neighborhood);

                    var candidateSolution = _successors
                        .Select((s, i) =>
                            i != a || i != b
                                ? s
                                : i == a
                                    ? neighborhood[b] ?? throw new NullReferenceException()
                                    : neighborhood[a] ?? throw new NullReferenceException())
                        .ToArray();
                    if (!Circuit.Valid(candidateSolution) || !new Circuit(candidateSolution).ToRoute().Valid())
                        throw new ArgumentOutOfRangeException();
                    //logging.debug(f's-new-sol: {newSolution}')
                    // get the cost between the two solutions
                    var cost = _evaluate(_successors) - _evaluate(candidateSolution);
                    // if the new solution is better, accept it
                    if (cost >= 0)
                    {
                        _successors = candidateSolution;
                        _bestSuccessors = candidateSolution;
                    }
                    // if the new solution is not better, accept it with a probability of e^(-cost/temp)
                    else if (_random.NextDouble() < Math.Exp(-cost / _currTemp))
                    {
                        _successors = candidateSolution;
                    }

                    // decrement the temperature
                    _decrementRule();
                }

            return new Circuit(_bestSuccessors);
        }

        //logging.info(f'stopping solver')
    }
}
