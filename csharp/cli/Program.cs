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
        // The penalty per unit of overload and lateness
        private const double Lambda = 10;

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
            VrptwEvaluator evaluator = new(instance.Customers, instance.Capacity, reindexer);
            double optimalCost = evaluator.Evaluate(optimalCircuit.Successors).Distance;
            Debug.Assert(Math.Abs(828.94 - optimalCost) < 0.01);
            Circuit initial = optimalCircuit;

            Func<IEnumerable<int>, double> evaluate = x => evaluator.Evaluate(x.ToArray()).Penalized(Lambda);
            Func<IEnumerable<int>, bool> isFeasible = x => evaluator.Evaluate(x.ToArray()).Feasible;
            // The end depots are always followed by the next vehicle's start depot
            Func<IEnumerable<int>, IList<int?>> neighborOperator =
                e => Explorer.NeighborhoodSelector(e, reindexer.EndDepotIndexes);

            SimulatedAnnealing solver = new(
                initial,
                evaluate,
                neighborOperator,
                initialTemp: 100,
                finalTemp: 0.1,
                tempReduction: ReductionFunction.geometric,
                iterationPerTemp: 1000,
                alpha: 0.95,
                isFeasible: isFeasible);
            var solution = solver.Run();
            Evaluation found = evaluator.Evaluate(solution.Successors);
            Console.WriteLine($"best known solution cost:\t{optimalCost:F2}");
            Console.WriteLine($"found solution cost:\t{found.Distance:F2}");
            Console.WriteLine($"feasible:\t{found.Feasible}"
                              + $" (overload {found.Overload}, lateness {found.Lateness:F2})");
            Console.WriteLine($"vehicles used:\t{evaluator.VehiclesUsed(solution.Successors)}");
            Console.WriteLine($"gap:\t{100 * (found.Distance - optimalCost) / optimalCost:F2}%");
        }
    }
}