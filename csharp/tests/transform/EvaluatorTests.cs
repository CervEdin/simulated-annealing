using System;
using System.Linq;
using algorithm.constraint;
using cli;
using data_layer;
using transform;
using Xunit;

namespace tests.transform
{
    public class EvaluatorTests
    {
        // A depot at the origin and customers on the x axis, the distance is the difference in x
        private static Customer[] Customers(params (int x, int demand, int earliest, int latest)[] customers)
            => new[] {new Customer {Id = 0, Earliest = 0, Latest = 100}}
                .Concat(customers.Select((c, i) => new Customer
                {
                    Id = i + 1,
                    X = c.x,
                    Demand = c.demand,
                    Earliest = c.earliest,
                    Latest = c.latest,
                    Cost = 1
                }))
                .ToArray();

        private static Evaluation Evaluate(Customer[] customers, int capacity, int nVehicles, params int[][] routes)
        {
            Reindexer reindexer = new(nVehicles, customers.Length - 1);
            VrptwEvaluator evaluator = new(customers, capacity, reindexer);
            Circuit circuit = new Route(routes.ToCircuit(reindexer)).ToCircuit();
            return evaluator.Evaluate(circuit.Successors);
        }

        [Fact]
        public void BestKnownC101IsFeasible()
        {
            Loader loader = new("c", "1", "01");
            Instance instance = loader.Instance();
            Reindexer reindexer = new(instance.nVehicles, instance.Customers.Count - 1);
            VrptwEvaluator evaluator = new(instance.Customers, instance.Capacity, reindexer);
            Circuit circuit = new Route(loader.Result().Solution.ToCircuit(reindexer)).ToCircuit();

            Evaluation evaluation = evaluator.Evaluate(circuit.Successors);
            Assert.Equal(828.94, evaluation.Distance, 2);
            Assert.Equal(0, evaluation.Overload);
            Assert.Equal(0, evaluation.Lateness);
            Assert.True(evaluation.Feasible);
            Assert.Equal(evaluation.Distance, evaluation.Penalized(10));
            Assert.Equal(10, evaluator.VehiclesUsed(circuit.Successors));
        }

        [Fact]
        public void MovingACustomerIntoAFullRouteOverloads()
        {
            Loader loader = new("c", "1", "01");
            Instance instance = loader.Instance();
            Reindexer reindexer = new(instance.nVehicles, instance.Customers.Count - 1);
            VrptwEvaluator evaluator = new(instance.Customers, instance.Capacity, reindexer);
            var routes = loader.Result().Solution.Select(r => r.ToList()).ToList();
            int Load(int r) => routes[r].Sum(id => instance.Customers.Single(c => c.Id == id).Demand);
            int full = Enumerable.Range(0, routes.Count).First(r => Load(r) == instance.Capacity);
            int other = (full + 1) % routes.Count;
            int moved = routes[other][0];
            routes[other].RemoveAt(0);
            routes[full].Add(moved);

            Circuit circuit = new Route(routes.ToCircuit(reindexer)).ToCircuit();
            Evaluation evaluation = evaluator.Evaluate(circuit.Successors);
            Assert.Equal(instance.Customers.Single(c => c.Id == moved).Demand, evaluation.Overload);
            Assert.False(evaluation.Feasible);
            Assert.True(evaluation.Penalized(10) > evaluation.Distance);
        }

        [Fact]
        public void Distance()
        {
            var customers = Customers((3, 0, 0, 100), (5, 0, 0, 100));
            Assert.Equal(10, Evaluate(customers, 10, 1, new[] {1, 2}).Distance);
            Assert.Equal(10, Evaluate(customers, 10, 1, new[] {2, 1}).Distance);
            Assert.Equal(16, Evaluate(customers, 10, 2, new[] {2}, new[] {1}).Distance);
        }

        [Fact]
        public void OverloadIsTheExcess()
        {
            var customers = Customers((1, 6, 0, 100), (2, 7, 0, 100), (3, 5, 0, 100));
            // loads 13 and 5, then 18
            Evaluation evaluation = Evaluate(customers, 10, 2, new[] {1, 2}, new[] {3});
            Assert.Equal(3, evaluation.Overload);
            Assert.Equal(0, evaluation.Lateness);
            Assert.Equal(8, Evaluate(customers, 10, 1, new[] {1, 2, 3}).Overload);
            Assert.True(Evaluate(customers, 13, 2, new[] {1, 2}, new[] {3}).Feasible);
        }

        [Fact]
        public void LatenessIsTheDelay()
        {
            // arrive at 1 at t = 1, leave at 2, arrive at 2 at t = 3
            var customers = Customers((1, 0, 0, 100), (2, 0, 0, 2));
            Evaluation evaluation = Evaluate(customers, 10, 1, new[] {1, 2});
            Assert.Equal(1, evaluation.Lateness);
            Assert.Equal(0, evaluation.Overload);
            Assert.False(evaluation.Feasible);
            Assert.Equal(4 + 10 * 1, evaluation.Penalized(10));
            // visiting 2 first is on time
            Assert.True(Evaluate(customers, 10, 1, new[] {2, 1}).Feasible);
        }

        [Fact]
        public void ArrivingEarlyWaits()
        {
            // arrive at 1 at t = 1, wait until 10, leave at 11, arrive at 2 at t = 12
            var customers = Customers((1, 0, 10, 100), (2, 0, 0, 12));
            Assert.True(Evaluate(customers, 10, 1, new[] {1, 2}).Feasible);
            customers[2].Latest = 11;
            Assert.Equal(1, Evaluate(customers, 10, 1, new[] {1, 2}).Lateness);
        }

        [Fact]
        public void ReturningLateToTheDepot()
        {
            // leave 1 at t = 51, return at t = 101
            var customers = Customers((50, 0, 0, 100));
            Assert.Equal(1, Evaluate(customers, 10, 1, new[] {1}).Lateness);
            customers[0].Latest = 101;
            Assert.True(Evaluate(customers, 10, 1, new[] {1}).Feasible);
        }

        [Fact]
        public void EachVehicleStartsAtTheDepot()
        {
            // the second vehicle starts fresh at t = 0 and without load
            var customers = Customers((40, 6, 0, 100), (40, 6, 0, 45));
            Evaluation evaluation = Evaluate(customers, 10, 2, new[] {1}, new[] {2});
            Assert.True(evaluation.Feasible);
            Assert.Equal(160, evaluation.Distance);
        }

        [Fact]
        public void EmptyRoutes()
        {
            var customers = Customers((1, 0, 0, 100));
            Reindexer reindexer = new(3, 1);
            VrptwEvaluator evaluator = new(customers, 10, reindexer);
            Circuit empty = new Route(Array.Empty<int[]>().ToCircuit(reindexer)
                .Concat(reindexer.CustomerIndexes(1))).ToCircuit();
            Assert.Equal(0, evaluator.VehiclesUsed(empty.Successors));

            Circuit circuit = new Route(new[] {new int[0], new[] {1}}.ToCircuit(reindexer)).ToCircuit();
            Evaluation evaluation = evaluator.Evaluate(circuit.Successors);
            Assert.Equal(new Evaluation(2, 0, 0), evaluation);
            Assert.Equal(1, evaluator.VehiclesUsed(circuit.Successors));
        }

        [Fact]
        public void RejectsWhatIsNotACircuit()
        {
            VrptwEvaluator evaluator = new(Customers((1, 0, 0, 100)), 10, new Reindexer(1, 1));
            Assert.Throws<ArgumentException>(() => evaluator.Evaluate(new[] {1, 0}));
            Assert.Throws<ArgumentException>(() => evaluator.Evaluate(new[] {1, 2, 1}));
        }
    }
}
