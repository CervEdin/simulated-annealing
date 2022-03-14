using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace algorithm.solver
{
    public class Circuit
    {
        public readonly int[] Successors;

        public Circuit(IEnumerable<int> successors)
        {
            var ss = successors.ToArray();
            if (!ss.AllDifferent() || !ss.Circuit())
                throw new ArgumentOutOfRangeException(nameof(successors));
            Successors = ss.ToArray();
        }

        public Route ToRoute()
        {
            var rr = new int[Successors.Length];

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
    }

    public class Route
    {
        public int[] List;

        public Route(ICollection<int> ints)
        {
            if (!ints.AllDifferent())
                throw new ArgumentOutOfRangeException(nameof(ints));
            List = ints.ToArray();
        }

        public Circuit ToCircuit()
        {
            var circuit = new int[List.Length];

            int current_i = 0;
            int current = List[current_i];
            int successor_i = current_i + 1;
            int successor = List[successor_i];
            while (current_i < circuit.Length)
            {
                circuit[current] = successor;
                current = successor;
                successor_i = (successor_i + 1) % circuit.Length;
                successor = List[successor_i];
                current_i++;
            }

            return new Circuit(circuit);
        }
    }

    public enum ReductionFunction
    {
        linear,
        geometric,
        slowDecrease
    }

    public class SimulatedAnnealing
    {
        private readonly Random _random = new(1);
        private readonly int _alpha;
        private readonly int _beta;
        private int _currTemp;

        private readonly Action _decrementRule;
        private readonly Func<IEnumerable<int>, double> _evaluate;
        private readonly int _finalTemp;
        private readonly int _iterationPerTemp;

        private readonly Func<
            IEnumerable<int>,
            IList<int>
        > _neighborhoodSelector;

        private int[] _successors;

        public SimulatedAnnealing(
            Circuit initialSolution,
            Func<IEnumerable<int>, double> solutionEvaluator,
            Func<IEnumerable<int>, IList<int>> neighborhoodSelector,
            int initialTemp = 10,
            int finalTemp = 1,
            ReductionFunction tempReduction = ReductionFunction.linear,
            int iterationPerTemp = 10,
            int alpha = 10,
            int beta = 5
        )
        {
            //logging.debug(f'evaluator: {solutionEvaluator}')
            _successors = initialSolution.Successors;
            _evaluate = solutionEvaluator;
            _currTemp = initialTemp;
            _finalTemp = finalTemp;
            _iterationPerTemp = iterationPerTemp;
            _alpha = alpha;
            _beta = beta;
            _neighborhoodSelector = neighborhoodSelector;

            if (tempReduction == ReductionFunction.linear)
                _decrementRule = LinearTempReduction;
            // else if (tempReduction == "geometric")
            // this.decrementRule = this.geometricTempReduction;
            // else if (tempReduction == "slowDecrease")
            // this.decrementRule = this.slowDecreaseTempReduction;
            // else
            // this.decrementRule = tempReduction
        }

        private void LinearTempReduction() => _currTemp -= _alpha;

        // ReSharper disable once UnusedMember.Local
        private void GeometricTempReduction() =>
            _currTemp *= 1 / _alpha;

        // ReSharper disable once UnusedMember.Local
        private void SlowDecreaseTempReduction() =>
            _currTemp = _currTemp / (1 + _beta * _currTemp);

        private bool IsTerminationCriteriaMet()
        {
            // can add more termination criteria
            return _currTemp <= _finalTemp || _neighborhoodSelector(_successors).Any();
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
                    // pick a random to swap
                    var toSwap = _random.Next(0, successors.Count);

                    //logging.debug(f'chosen: {to_change}')
                    var successor = successors[toSwap];
                    var newSolution = successors
                        .Select(x =>
                            x != toSwap && x != successor
                                ? x
                                : x == toSwap
                                    ? successor
                                    : toSwap)
                        .ToArray();

                    //logging.debug(f's-new-sol: {newSolution}')
                    // get the cost between the two solutions
                    var cost = _evaluate(_successors) - _evaluate(newSolution);
                    // if the new solution is better, accept it
                    if (cost >= 0)
                        _successors = newSolution;
                    // if the new solution is not better, accept it with a probability of e^(-cost/temp)
                    else if (_random.NextDouble() < Math.Exp(-cost / _currTemp))
                        _successors = newSolution;
                    // decrement the temperature
                    _decrementRule();
                }

            return new Circuit(_successors);
        }

        //logging.info(f'stopping solver')
    }
}
