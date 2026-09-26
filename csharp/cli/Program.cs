using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using algorithm.constraint;
using algorithm.solver;
using data_layer;
using transform;

namespace cli
{
    internal static class Program
    {
        private static void Test(Circuit circuit, Route route)
        {
            Debug.Assert(circuit.ToRoute().List.SequenceEqual(route.List));
            Debug.Assert(route.ToCircuit().Successors.SequenceEqual(circuit.Successors));
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
            var optimalCircuit = optimalRoute.ToCircuit();
            Test(optimalCircuit, optimalRoute);
            // ReSharper disable once CoVariantArrayConversion
            IList<IList<double>> matrix = instance.Customers.ToMatrix(reindexer);
            double optimalCost = optimalCircuit.CostObjective(matrix);
            Debug.Assert(Math.Abs(828.94 - optimalCost) < 0.01);
            Circuit initial = optimalCircuit;

            Func<IEnumerable<int>, double> evaluator = x => new Circuit(x).CostObjective(matrix);
            // The end depots are always followed by the next vehicle's start depot
            Func<IEnumerable<int>, IList<int?>> neighborOperator =
                e => Explorer.NeighborhoodSelector(e, reindexer.EndDepotIndexes);

            SimulatedAnnealing solver = new(
                initial,
                evaluator,
                neighborOperator,
                initialTemp: 100,
                finalTemp: 0.1,
                tempReduction: ReductionFunction.geometric,
                iterationPerTemp: 1000,
                alpha: 0.95);
            var solution = solver.Run();
            double cost = solution.CostObjective(matrix);
            Console.WriteLine($"best known solution cost:\t{optimalCost:F2}");
            // Capacity and time windows are not enforced yet, so this is not comparable to the best known
            Console.WriteLine($"found solution cost:\t{cost:F2}");
        }
    }
}