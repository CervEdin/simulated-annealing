using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private const double Lambda = 100;

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
            // e.g. r 1 01 for R101, C101 by default
            Loader loader = args.Length == 3 ? new Loader(args[0], args[1], args[2]) : new Loader();
            Instance instance = loader.Instance();
            Reindexer reindexer = new(instance.nVehicles, instance.Customers.Count - 1);
            VrptwEvaluator evaluator = new(instance.Customers, instance.Capacity, reindexer);
            Console.WriteLine($"instance:\t{instance.Name}");

            double? bestKnownCost = null;
            Result? result = BestKnown(loader);
            if (result != null)
            {
                Route bestKnownRoute = new(result.Solution.ToCircuit(reindexer).ToArray());
                var bestKnownCircuit = bestKnownRoute.ToCircuit();
                Test(bestKnownCircuit, bestKnownRoute);
                bestKnownCost = evaluator.Evaluate(bestKnownCircuit.Successors).Distance;
                Debug.Assert(instance.Name != "C101" || Math.Abs(828.94 - bestKnownCost.Value) < 0.01);
                Console.WriteLine($"best known solution cost:\t{bestKnownCost:F2}");
                Solve("best known", bestKnownCircuit, evaluator, reindexer, bestKnownCost);
            }

            var routes = Construction.NearestNeighbor(instance.Customers, instance.Capacity, instance.nVehicles);
            Circuit constructed = new Route(routes.ToCircuit(reindexer)).ToCircuit();
            Report("construction", constructed, evaluator, bestKnownCost);
            Solve("construction", constructed, evaluator, reindexer, bestKnownCost);
        }

        private static Result? BestKnown(Loader loader)
        {
            try
            {
                return loader.Result();
            }
            catch (FileNotFoundException)
            {
                // Not every instance has a published result
                return null;
            }
        }

        private static void Solve(
            string start,
            Circuit initial,
            VrptwEvaluator evaluator,
            Reindexer reindexer,
            double? bestKnownCost
        )
        {
            Func<IEnumerable<int>, (double, double)> evaluate = x =>
            {
                Evaluation evaluation = evaluator.Evaluate(x as IReadOnlyList<int> ?? x.ToArray());
                return (evaluation.Distance, evaluation.Violation);
            };
            // The end depots are always followed by the next vehicle's start depot
            Func<IEnumerable<int>, IList<int?>> neighborOperator =
                e => Explorer.NeighborhoodSelector(e, reindexer.EndDepotIndexes);

            SimulatedAnnealing solver = new(
                initial,
                evaluate,
                neighborOperator,
                initialTemp: 10,
                finalTemp: 0.1,
                tempReduction: ReductionFunction.geometric,
                iterationPerTemp: 10000,
                alpha: 0.95,
                penalty: Lambda);
            Stopwatch stopwatch = Stopwatch.StartNew();
            var solution = solver.Run();
            Report($"annealed from {start} in {stopwatch.Elapsed.TotalSeconds:F1}s", solution, evaluator,
                bestKnownCost);
        }

        private static void Report(
            string name,
            Circuit solution,
            VrptwEvaluator evaluator,
            double? bestKnownCost
        )
        {
            Evaluation evaluation = evaluator.Evaluate(solution.Successors);
            Console.WriteLine(name);
            Console.WriteLine($"  found solution cost:\t{evaluation.Distance:F2}");
            Console.WriteLine($"  feasible:\t{evaluation.Feasible}"
                              + $" (overload {evaluation.Overload}, lateness {evaluation.Lateness:F2})");
            Console.WriteLine($"  vehicles used:\t{evaluator.VehiclesUsed(solution.Successors)}");
            if (!bestKnownCost.HasValue)
                return;
            double gap = 100 * (evaluation.Distance - bestKnownCost.Value) / bestKnownCost.Value;
            // A rounded 0 prints without a sign
            Console.WriteLine($"  gap:\t{gap:0.00;-0.00;0.00}%");
        }
    }
}