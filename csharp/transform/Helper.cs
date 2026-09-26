using System;
using System.Collections.Generic;
using System.Linq;
using data_layer;

// ReSharper disable once CheckNamespace
namespace transform
{
    public static class Helper
    {
        internal static double Distance(
            (int x, int y) p1,
            (int x, int y) p2
        ) => Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));

        public static double[][] ToMatrix(
            this IList<Customer> customers,
            Reindexer reindexer
        )
        {
            var byId = customers.ToDictionary(c => c.Id);
            return ToMatrix(
                reindexer
                    .AllIndexes
                    .Select(reindexer.CustomerId)
                    .Select(id => byId[id].Coords)
            );
        }

        private static double[][] ToMatrix(
            this IEnumerable<(int, int)> self
        )
        {
            var points = self as (int, int)[] ?? self.ToArray();
            return points
                .Select(p => points.Select(t => Distance(p, t)).ToArray())
                .ToArray();
        }

        public static IEnumerable<int> ToCircuit(
            this IEnumerable<IEnumerable<int>> solution,
            Reindexer reindexer
        )
        {
            int usedVehicles = solution.Count();
            int lastDepot = usedVehicles * 2 - 1;
            var addedDepots = solution
                .Select((route, i) =>
                    new[] { 2 * i }
                        .Concat(route
                            .Select(id => reindexer
                                .CustomerIndexes(id)
                                .Single()))
                        .Concat(new[] { 2 * i + 1 })
                );
            var missingDepots = reindexer.DepotIndexes
                .Where(i => i > lastDepot)
                .ToList();
            var depotPairs = missingDepots
                .Select((x, i) => (x, i))
                .Zip(missingDepots.Skip(1))
                .Select(tp => (tp.First.x, tp.Second, tp.First.i))
                .Where(tp => tp.i % 2 == 0)
                .Select(tp => new[] { tp.x, tp.Second });
            var addedMissingRoutes = addedDepots
                .Concat(depotPairs);
            var res = addedMissingRoutes
                .SelectMany(x => x);
            return res;
        }
    }

    public class Reindexer
    {
        private readonly int _nCustomers;
        private readonly int _nVehicles;

        public Reindexer(int nVehicles, int nCustomers)
        {
            if (nVehicles < 1)
                throw new ArgumentOutOfRangeException(nameof(nVehicles));
            if (nCustomers < 0)
                throw new ArgumentOutOfRangeException(nameof(nCustomers));
            _nVehicles = nVehicles;
            _nCustomers = nCustomers;
        }

        public IEnumerable<int> AllIndexes
            => DepotIndexes.Concat(VisitIndexes);

        private int LastDepot => _nVehicles * 2 - 1;

        /// <summary>
        /// Each vehicle has a start depot 2i and an end depot 2i + 1
        /// </summary>
        public IEnumerable<int> DepotIndexes
            => Enumerable.Range(0, _nVehicles * 2);

        public IEnumerable<int> EndDepotIndexes
            => DepotIndexes.Where(i => i % 2 == 1);

        private IEnumerable<int> VisitIndexes
            => Enumerable.Range(LastDepot + 1, _nCustomers);

        public int CustomerId(int index) => index <= LastDepot
            ? 0
            : index - LastDepot;

        public IEnumerable<int> CustomerIndexes(int id)
        {
            return id == 0
                ? DepotIndexes
                : VisitIndexes.Where(i => i - LastDepot == id);
        }
    }
}