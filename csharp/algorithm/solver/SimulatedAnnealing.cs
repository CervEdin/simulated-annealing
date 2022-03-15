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
        private readonly int[] _predecessors;

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
            _predecessors = initialSolution.Successors.Predecessors().ToArray();
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

        public Circuit Run()
        {
            while (!IsTerminationCriteriaMet())
                // iterate that number of times
                foreach (int _ in Enumerable.Range(0, _iterationPerTemp))
                {
                    // get neighbors (all successors)
                    var neighborhood = _neighborhoodSelector(_successors);

                    //logging.debug(f's-neighbors: {neighbors}')
                    var moves = Explorer.Mover(
                        neighborhood,
                        _predecessors,
                        _random).ToArray();

                    int[] candidateSolution = _successors
                        .Select((s, i) =>
                            (i, s, new_s: moves.SingleOrDefault(tp => tp.i == i).s)
                        )
                        .Select(tp => tp.i != tp.new_s ? tp.new_s : tp.s)
                        .ToArray();
                    if (!Circuit.Valid(candidateSolution) || !new Circuit(candidateSolution).ToRoute().Valid())
                        throw new ArgumentOutOfRangeException();
                    //logging.debug(f's-new-sol: {newSolution}')
                    // get the cost between the two solutions
                    double cost = _evaluate(_successors) - _evaluate(candidateSolution);
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