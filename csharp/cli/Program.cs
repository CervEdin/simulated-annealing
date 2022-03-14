using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using algorithm.solver;
using data_layer;
using transform;

namespace cli
{
    internal static class Program
    {
        private const string benchmarks = "c:/users/erikce/repos/solomon-vrptw-benchmarks";

        public static JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static void Test()
        {
            var circuit = new Circuit(new[] { 1, 2, 0 });
            var route = new Route(new[] { 0, 1, 2 });
            Debug.Assert(
                circuit.ToRoute().List
                    .Zip(route.List)
                    .All(tp => tp.First == tp.Second));
            Debug.Assert(
                route.ToCircuit().Successors
                    .Zip(circuit.Successors)
                    .All(tp => tp.First == tp.Second));
        }

        private static void Main(
            string[] args
        )
        {
            AppDomain.CurrentDomain.UnhandledException += (
                _,
                eventArgs
            ) =>
            {
                Console.Error.WriteLine("Unhandled exception: " + eventArgs.ExceptionObject);
                Environment.Exit(1);
            };
            Test();
            var loader = new Loader("c", "1", "01");
            var instance = loader.Instance();
            var result = loader.Result();
            var reindexer = new Reindexer(instance.nVehicles, instance.Customers.Count - 1);
            var optimalRoute = new Route(result.Solution.ToCircuit(reindexer).ToArray());
            var optimalCircuit = optimalRoute.ToCircuit();
            // ReSharper disable once CoVariantArrayConversion
            IList<IList<double>> matrix = instance.Customers.ToMatrix(reindexer);
            var optimalCost = optimalCircuit.CostObjective(matrix);
            Debug.Assert(Math.Abs(828.94 - optimalCost) > 0.01);
            var initial = optimalCircuit;
            Func<IEnumerable<int>, double> evaluator = x => CostObjective(x.ToList(), matrix);
            Func<IEnumerable<int>, IList<int>> neighborOperator = NeighborOperator;
            var solver = new SimulatedAnnealing(initial, evaluator, neighborOperator);
            var solution = solver.Run();
            var cost = solution.CostObjective(matrix);
            Console.WriteLine($"found solution cost:\t{cost}");
        }

        private static IList<int> NeighborOperator(
            IEnumerable<int> arg
        ) => arg.Select(x => x).ToList();

        private static double CostObjective(
            this Circuit c,
            IList<IList<double>> m
        ) => CostObjective(c.ToRoute().List, m);

        private static double CostObjective(
            ICollection<int> route,
            IList<IList<double>> m
        ) => route
            .Zip(route.Skip(1))
            .Select(ss => m[ss.First][ss.Second])
            .Sum();
    }
}
