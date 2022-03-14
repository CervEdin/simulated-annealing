using System;
using System.Collections.Generic;
using System.Linq;
using data_layer;

// ReSharper disable once CheckNamespace
namespace transform
{
    public static class Helper
    {
        // ReSharper disable once UnusedMember.Local
        private static double Distance(
            ((int, int), (int, int)) tp
        )
            => Distance(tp.Item1, tp.Item2);

        private static double Distance(
            (int x, int y) p1,
            (int x, int y) p2
        )
            => Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));

        public static double[][] ToMatrix(
            this IList<Customer> customers,
            Reindexer reindexer
        )
            => ToMatrix(
                reindexer
                    .AllIndexes
                    .Select(reindexer.CustomerId)
                    .Select(i => customers[i].Coords)
            );

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
            var usedVehicles = solution.Count();
            int lastDepot = usedVehicles * 2 - 1;
            var addedDepots = solution
                .Select((route, i) =>
                    new[] { 2 * i }
                        .Concat(route
                            .Select(id => reindexer
                                .CustomerIndexes(id)
                                .FirstOrDefault()))
                        .Concat(new[] { (2 * i) + 1 })
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
            IEnumerable<IEnumerable<int>> addedMissingRoutes = addedDepots
                .Concat(depotPairs);
            var res = addedMissingRoutes
                .SelectMany(x => x);
            return res;
        }
    }

    public class Reindexer
    {
        private readonly int _nVehicles;
        private readonly int _nCustomers;

        public int CustomerId(int index)
            => DepotIndexes.Contains(index)
                ? 0
                : index - DepotIndexes.Last();

        public IEnumerable<int> CustomerIndexes(int id)
            => id == 0
                ? DepotIndexes
                : VisitIndexes.Where(i => i - DepotIndexes.Last() == id);

        public IEnumerable<int> AllIndexes
            => DepotIndexes.Concat(VisitIndexes);

        public IEnumerable<int> DepotIndexes
            => Enumerable.Range(0, _nVehicles * 2);

        private IEnumerable<int> VisitIndexes
            => Enumerable.Range(DepotIndexes.Last() + 1, _nCustomers);

        public Reindexer(int nVehicles, int nCustomers)
        {
            _nVehicles = nVehicles;
            _nCustomers = nCustomers;
        }
    }
}
