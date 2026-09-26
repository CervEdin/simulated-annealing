using System;
using System.Collections.Generic;
using System.Linq;
using data_layer;

// ReSharper disable once CheckNamespace
namespace transform
{
    /// <summary>
    /// The distance of a solution and how much it breaks the capacity and time window constraints.
    /// </summary>
    public record Evaluation(double Distance, double Overload, double Lateness)
    {
        public bool Feasible => Overload == 0 && Lateness == 0;

        /// <summary>
        /// The distance plus lambda times the constraint violations
        /// </summary>
        public double Penalized(double lambda) => Distance + lambda * (Overload + Lateness);
    }

    /// <summary>
    /// Evaluates a circuit in the layout of the Reindexer: vehicle v starts at depot 2v, visits its customers
    /// and ends at depot 2v + 1. Travel time equals the distance, a vehicle arriving early waits until
    /// Earliest, arriving after Latest (the end depot's included) is lateness and a load above the
    /// capacity is overload.
    /// </summary>
    public class VrptwEvaluator
    {
        // Absorbs the floating-point error of summing the distances
        private const double Epsilon = 1e-9;

        private readonly int _capacity;
        private readonly Reindexer _reindexer;
        private readonly double[][] _matrix;
        private readonly Customer[] _customers;

        public VrptwEvaluator(
            IList<Customer> customers,
            int capacity,
            Reindexer reindexer
        )
        {
            var byId = customers.ToDictionary(c => c.Id);
            _capacity = capacity;
            _reindexer = reindexer;
            _matrix = customers.ToMatrix(reindexer);
            _customers = reindexer.AllIndexes
                .Select(i => byId[reindexer.CustomerId(i)])
                .ToArray();
        }

        private static bool IsStartDepot(int index, int customerId) => customerId == 0 && index % 2 == 0;

        private static bool IsEndDepot(int index, int customerId) => customerId == 0 && index % 2 == 1;

        /// <summary>
        /// Walk the circuit from the first start depot, 0
        /// </summary>
        /// <param name="successors">A circuit, successors[i] is the node visited after i</param>
        public Evaluation Evaluate(IReadOnlyList<int> successors)
        {
            if (successors.Count != _customers.Length)
                throw new ArgumentException("Wrong number of successors", nameof(successors));
            double distance = 0, overload = 0, lateness = 0;
            double t = _customers[0].Earliest;
            int load = 0;
            int steps = 0;
            for (int prev = 0, node = successors[0]; node != 0; prev = node, node = successors[node])
            {
                if (++steps >= successors.Count)
                    throw new ArgumentException("Not a circuit", nameof(successors));
                Customer customer = _customers[node];
                distance += _matrix[prev][node];
                if (IsStartDepot(node, customer.Id))
                {
                    t = customer.Earliest;
                    load = 0;
                    continue;
                }

                double arrival = t + _matrix[prev][node];
                double late = arrival - customer.Latest;
                if (late > Epsilon)
                    lateness += late;
                t = Math.Max(arrival, customer.Earliest) + customer.Cost;
                load += customer.Demand;
                if (IsEndDepot(node, customer.Id))
                    overload += Math.Max(0, load - _capacity);
            }

            return new Evaluation(distance, overload, lateness);
        }

        /// <summary>
        /// The number of vehicles that visit at least one customer
        /// </summary>
        public int VehiclesUsed(IReadOnlyList<int> successors)
            => _reindexer.DepotIndexes
                .Where(i => i % 2 == 0)
                .Count(i => _reindexer.CustomerId(successors[i]) != 0);
    }
}
