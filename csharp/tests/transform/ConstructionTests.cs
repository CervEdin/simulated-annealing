using System;
using System.Collections.Generic;
using System.Linq;
using algorithm.constraint;
using cli;
using data_layer;
using transform;
using Xunit;

namespace tests.transform
{
    public class ConstructionTests
    {
        // A depot at the origin and customers on the x axis
        private static Customer[] Customers(params (int x, int demand, int latest)[] customers)
            => new[] {new Customer {Id = 0, Latest = 100}}
                .Concat(customers.Select((c, i) => new Customer
                {
                    Id = i + 1,
                    X = c.x,
                    Demand = c.demand,
                    Latest = c.latest,
                    Cost = 1
                }))
                .ToArray();

        [Fact]
        public void C101IsFeasible()
        {
            Instance instance = new Loader("c", "1", "01").Instance();
            Reindexer reindexer = new(instance.nVehicles, instance.Customers.Count - 1);
            var routes = Construction.NearestNeighbor(instance.Customers, instance.Capacity, instance.nVehicles);
            Assert.InRange(routes.Count, 1, instance.nVehicles);
            Assert.All(routes, Assert.NotEmpty);
            Assert.Equal(Enumerable.Range(1, 100), routes.SelectMany(r => r).OrderBy(id => id));

            Route route = new(routes.ToCircuit(reindexer));
            // the vehicles' depots in order, each route between its start and end depot
            Assert.Equal(reindexer.DepotIndexes, route.List.Where(i => reindexer.CustomerId(i) == 0));
            Circuit circuit = route.ToCircuit();
            VrptwEvaluator evaluator = new(instance.Customers, instance.Capacity, reindexer);
            Assert.True(evaluator.Evaluate(circuit.Successors).Feasible);
            Assert.Equal(routes.Count, evaluator.VehiclesUsed(circuit.Successors));
        }

        [Fact]
        public void VisitsTheNearestCustomer()
            => Assert.Equal<IEnumerable<int>>(
                new[] {new[] {2, 3, 1}},
                Construction.NearestNeighbor(Customers((5, 1, 100), (1, 1, 100), (3, 1, 100)), 10, 3));

        [Fact]
        public void CountsTheWaitForEarliest()
        {
            // 2 is nearer, but its service can't start before 1's (at 5) is done
            var customers = Customers((5, 1, 100), (1, 1, 100));
            customers[2].Earliest = 20;
            Assert.Equal<IEnumerable<int>>(
                new[] {new[] {1, 2}},
                Construction.NearestNeighbor(customers, 10, 3));
        }

        [Fact]
        public void ReturnsWhenNoCustomerFits()
        {
            // 2 doesn't fit after 1 and 3 is late after 1 or 2 (arrival 4), but on time from the depot
            Assert.Equal<IEnumerable<int>>(
                new[] {new[] {1}, new[] {2}, new[] {3}},
                Construction.NearestNeighbor(Customers((1, 6, 100), (1, 6, 100), (3, 1, 3)), 10, 3));
            // 1 is on time after 2 (arrival 66), but the vehicle would return to the depot too late (at 112)
            Assert.Equal<IEnumerable<int>>(
                new[] {new[] {2}, new[] {1}},
                Construction.NearestNeighbor(Customers((45, 1, 100), (-10, 1, 100)), 10, 2));
        }

        [Fact]
        public void LeavesNoCustomerOut()
        {
            // one vehicle can't carry them all
            var customers = Customers((1, 6, 100), (2, 6, 100), (3, 6, 100));
            Reindexer reindexer = new(1, 3);
            var routes = Construction.NearestNeighbor(customers, 10, 1);
            Assert.Equal<IEnumerable<int>>(new[] {new[] {1, 2, 3}}, routes);
            Circuit circuit = new Route(routes.ToCircuit(reindexer)).ToCircuit();
            Assert.Equal(8, new VrptwEvaluator(customers, 10, reindexer).Evaluate(circuit.Successors).Overload);

            // nor return from 1 in time (at 121)
            Assert.Equal<IEnumerable<int>>(
                new[] {new[] {1}},
                Construction.NearestNeighbor(Customers((60, 1, 100)), 10, 2));
        }

        [Fact]
        public void NeedsAVehicle()
            => Assert.Throws<ArgumentOutOfRangeException>(
                () => Construction.NearestNeighbor(Customers((1, 1, 100)), 10, 0));
    }
}
