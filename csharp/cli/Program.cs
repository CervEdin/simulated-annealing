using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using algorithm.constraint;
using algorithm.solver;
using data_layer;
using transform;

namespace cli
{
    internal static class Program
    {

        public static JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static void Test(Circuit circuit, Route route)
        {
            Debug.Assert(
                circuit.ToRoute().List
                    .Zip(route.List)
                    .All(tp => tp.First == tp.Second));
            Debug.Assert(
                route.ToCircuit().Successors
                    .Zip(circuit.Successors)
                    .All(tp => tp.First == tp.Second));
        }

        private static void Test()
        {
            Circuit circuit = new(new[] {1, 2, 0});
            Route route = new(new[] {0, 1, 2});
            Test(circuit, route);
            circuit = new Circuit(new[] {1, 2, 3, 0});
            route = new Route(new[] {0, 1, 2, 3});
            Test(circuit, route);
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
            Loader loader = new();
            Instance instance = loader.Instance();
            Result result = loader.Result();
            Reindexer reindexer = new(instance.nVehicles, instance.Customers.Count - 1);
            Route optimalRoute = new(result.Solution.ToCircuit(reindexer).ToArray());
            Circuit? optimalCircuit = optimalRoute.ToCircuit();
            Test(optimalCircuit, optimalRoute);
            // ReSharper disable once CoVariantArrayConversion
            IList<IList<double>> matrix = instance.Customers.ToMatrix(reindexer);
            var optimalCost = optimalCircuit.CostObjective(matrix);
            Debug.Assert(Math.Abs(828.94 - optimalCost) > 0.01);
            Circuit initial = optimalCircuit;

            Func<IEnumerable<int>, double> evaluator = x => CostObjective(new Circuit(x), matrix);
            Func<IEnumerable<int>, IList<int>> neighborOperator = NeighborhoodSelector;

            SimulatedAnnealing solver = new(initial, evaluator, neighborOperator);
            Circuit? solution = solver.Run();
            var cost = solution.CostObjective(matrix);
            Console.WriteLine($"found solution cost:\t{cost}");
        }

        private static IList<int> NeighborhoodSelector(
            IEnumerable<int> arg
        )
        {
            return arg.Select(x => x).ToList();
        }

        private static double CostObjective(
            this Circuit c,
            IList<IList<double>> m
        ) =>
            CostObjective(c.ToRoute().List, m);

        private static double CostObjective(
            ICollection<int> route,
            IList<IList<double>> m
        ) =>
            route
                .Zip(route.Skip(1))
                .Select(ss => m[ss.First][ss.Second])
                .Sum();
    }
}