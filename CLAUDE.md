# CLAUDE.md - AI Assistant Guide for Simulated Annealing VRPTW Solver

## Project Overview

This repository contains a **C# implementation of a Simulated Annealing metaheuristic solver** for the **Vehicle Routing Problem with Time Windows (VRPTW)**. The solver uses simulated annealing to find near-optimal solutions for the classic Solomon VRPTW benchmark instances.

### What This Project Does

1. **Loads VRPTW benchmark instances** from the Solomon benchmark set (JSON format)
2. **Represents solutions** using two equivalent formulations:
   - **Circuit**: Successor array representation (Hamiltonian circuit)
   - **Route**: Ordered visit sequence representation
3. **Optimizes routes** using simulated annealing with configurable parameters:
   - Temperature reduction strategies (linear, geometric, slow decrease)
   - Neighborhood exploration via 3-edge exchange moves
   - Initial/final temperature and iteration counts
4. **Validates solutions** using constraint programming concepts (AllDifferent, Circuit)
5. **Computes costs** using Euclidean distance matrices, penalizing overload and lateness,
   and reports the best feasible solution (capacity and time windows respected)
6. **Handles multi-vehicle routing** via depot replication and reindexing
7. **Constructs an initial solution** with a nearest feasible neighbor heuristic

## Repository Structure

```
/home/user/simulated-annealing/
├── .gitignore                    # Ignores *.swp, main.out, main.err
├── .gitmodules                   # solomon-vrptw-benchmarks submodule
├── solomon-vrptw-benchmarks/     # Git submodule with benchmark data (may need init)
└── csharp/                       # Main C# solution
    ├── sim-an.sln                # Visual Studio solution file
    ├── .editorconfig             # Code formatting conventions (528 lines)
    ├── .gitignore                # C# ignores (bin/, obj/, .idea/)
    │
    ├── cli/                      # Command-line interface (executable)
    │   ├── cli.csproj
    │   ├── Program.cs            # Entry point, runs solver on C101 instance
    │   └── Loader.cs             # Loads benchmark instances and results from JSON
    │
    ├── data_layer/               # Domain models
    │   ├── data_layer.csproj
    │   ├── Instance.cs           # VRPTW problem instance model
    │   ├── Customer.cs           # Customer with coordinates, demand, time windows
    │   └── Result.cs             # Solution result model
    │
    ├── algorithm/                # Core optimization algorithms
    │   ├── algorithm.csproj
    │   ├── solver/
    │   │   ├── SimulatedAnnealing.cs  # Main SA algorithm
    │   │   └── Explorer.cs            # Relocate move, neighborhood, cost
    │   └── constraint/
    │       ├── Constraints.cs    # AllDifferent & Circuit constraints
    │       ├── Circuit.cs        # Circuit representation
    │       └── Route.cs          # Route representation
    │
    ├── transform/                # Data transformation utilities
    │   ├── transform.csproj
    │   ├── Helper.cs             # Distance matrix, reindexing
    │   ├── Evaluator.cs          # VRPTW distance, overload and lateness of a circuit
    │   └── Construction.cs       # Nearest feasible neighbor initial routes
    │
    └── tests/                    # xUnit tests
        ├── tests.csproj
        ├── algorithm/            # Constraints, representations, Explorer, SA
        ├── transform/            # Reindexer, helpers, evaluator and construction
        └── cli/                  # Loading C101 and its best known cost
```

### Project Dependency Graph

```
cli (executable)
 ├─→ algorithm
 ├─→ data_layer
 └─→ transform
      └─→ data_layer

algorithm (library)
 └─→ (no external dependencies)

data_layer (library)
 └─→ (no external dependencies, uses System.Text.Json)

transform (library)
 └─→ data_layer

tests (xUnit)
 └─→ algorithm, cli, data_layer, transform
```

## Technology Stack

- **Language**: C# (.NET 8.0)
- **Framework**: .NET 8.0 (`net8.0` target)
- **Dependencies**:
  - System.Text.Json (JSON serialization)
  - xUnit (tests)
- **IDE**: JetBrains Rider (config in `.idea/`)
- **Nullable Reference Types**: Enabled in CLI project

## Build & Development Workflow

### Building the Project

```bash
cd /home/user/simulated-annealing/csharp

# Restore NuGet packages
dotnet restore

# Build all projects
dotnet build

# Build in Release mode
dotnet build -c Release

# Run the tests
dotnet test

# Run the CLI
dotnet run --project cli

# Run the CLI on another instance, e.g. R101
dotnet run --project cli -- r 1 01
```

### Solution Configuration

- **Configurations**: Debug|Any CPU, Release|Any CPU
- **Output**: CLI project produces an executable, others are class libraries
- **Asset Copying**: `solomon-vrptw-benchmarks/**/*.json` files copied to output directory,
  `Loader` resolves them relative to `AppContext.BaseDirectory`

### Git Submodule Setup

The benchmark data is stored in a git submodule. If not initialized:

```bash
cd /home/user/simulated-annealing
git submodule update --init --recursive
```

This will clone the Solomon VRPTW benchmark instances into `solomon-vrptw-benchmarks/`.

### Commit Message Convention

Follow the established pattern:

```
<component>: <description>

Examples:
  bench:            Update solomon-vrptw-benchmarks
  transform:        Validate the Reindexer and look customers up by id
  tests:            Add an xUnit test project
  algo/constraint:  Turn validation method into function
  algo/solver:      Move functionality into Explorer helper class
  cli:              Include the solomon benchmarks/results *.json
  data_layer:       Rename data-layer => data_layer
  doc:              Add documentation to Constraints
  conf:             Update .editorconfig settings
  wip:              Work in progress commits
```

**Components**: `bench`, `algo/constraint`, `algo/solver`, `cli`, `data_layer`, `transform`, `tests`, `conf`, `doc`, `wip`

Existing commits separate the component and description with a tab.

## Code Conventions & Style

### EditorConfig Settings

The project uses a comprehensive `.editorconfig` (528 lines) with:

```ini
[*]
charset = utf-8-bom
end_of_line = crlf              # Windows line endings
indent_size = 4
indent_style = space
max_line_length = 120
tab_width = 4
```

Note that the existing source files are actually LF without a BOM; match the file you are editing
rather than converting whole files.

### C# Coding Patterns

The codebase follows modern C# conventions with heavy use of:

#### 1. Expression-Bodied Members

Preferred for concise methods, properties, and constructors:

```csharp
// Good - matches codebase style
private static double Distance((int x, int y) p1, (int x, int y) p2)
    => Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));

public (int x, int y) Coords => (X, Y);
```

#### 2. Extension Methods

Used extensively for fluent APIs:

```csharp
public static bool AllDifferent(this ICollection<int> ints)
    => ints.Count == ints.Distinct().Count();

public static double CostObjective(this Circuit c, IList<IList<double>> m)
    => c.Successors.Select((x, i) => (x, i))
        .Sum(tp => m[tp.i][tp.x]);
```

#### 3. LINQ-Heavy Style

LINQ is used extensively (30+ occurrences):

```csharp
return circuit.Successors
    .Select((c, i) => (c, i))
    .Where(tp => tp.c != null)
    .Select(tp => (tp.c.Value, tp.i))
    .ToArray();
```

#### 4. Pattern Matching & Switch Expressions

```csharp
_decrementRule = tempReduction switch
{
    ReductionFunction.linear when alpha > 0 => LinearTempReduction,
    ReductionFunction.geometric when alpha is > 0 and < 1 => GeometricTempReduction,
    ReductionFunction.slowDecrease when beta > 0 => SlowDecreaseTempReduction,
    // ... parameter validation
    _ => throw new ArgumentOutOfRangeException(nameof(tempReduction))
};
```

#### 5. Tuple Deconstruction

```csharp
foreach ((int i, int s) in moves)
    candidateSolution[i] = s;
```

#### 6. Type Declarations

- Prefer **explicit types** for built-in types (not `var`)
- Use `var` for complex LINQ expressions where type is obvious

### ReSharper Conventions

The codebase uses ReSharper/Rider with specific settings:

- Expression-bodied members preferred for methods, constructors, local functions
- Wrap parameters if long (`chop_if_long`)
- Custom dictionary words: "ints", "Reindexer"
- Some intentional suppressions with `// ReSharper disable once` comments

## Core Architecture & Components

### 1. Simulated Annealing Algorithm

**Location**: `csharp/algorithm/solver/SimulatedAnnealing.cs`

```csharp
public class SimulatedAnnealing
{
    public SimulatedAnnealing(
        Circuit initialSolution,
        Func<IEnumerable<int>, (double cost, double violation)> solutionEvaluator,  // violation 0: feasible
        Func<IEnumerable<int>, IList<int?>> neighborhoodSelector,
        double initialTemp = 10,
        double finalTemp = 1,           // must be > 0
        ReductionFunction tempReduction = ReductionFunction.geometric,
        int iterationPerTemp = 100,
        double alpha = 0.9,
        double beta = 0.01,
        int seed = 1,
        double penalty = 1,             // per unit of violation, must be > 0
        PenaltyUpdate penaltyUpdate = PenaltyUpdate.constant)

    public Circuit Run()        // Main optimization loop, returns the best solution
    public double BestCost { get; }     // its cost, without the penalty
    public bool FoundFeasible { get; }  // whether it is feasible, if not it is the least violating
    public double Penalty { get; }      // the current penalty
}

public enum PenaltyUpdate
{
    constant,       // keep the penalty
    cooling         // penalty = initial penalty * initialTemp / T, tightens as the search cools
}

public enum ReductionFunction
{
    linear,         // T -= alpha           (alpha > 0)
    geometric,      // T *= alpha           (0 < alpha < 1)
    slowDecrease    // T /= 1 + beta * T    (beta > 0)
}
```

Invalid parameters throw `ArgumentOutOfRangeException`, so the schedule always terminates.

Constraints are hard: a solution that violates them may be visited on the way, but it is only
returned if no feasible solution was found, and then `FoundFeasible` is false. That is a result,
not an error.

**Algorithm Flow**:
1. Start with the initial solution, its cost and the initial temperature
2. While the temperature is above the final temperature, `iterationPerTemp` times:
   - Generate a candidate with `Explorer.Mover` from the neighborhood selector's view
   - Accept it if `delta <= 0`, else with probability `e^(-delta/T)`, where `delta` is the change
     of the cost plus the penalty times the violation
   - Track the best solution: the least violating, then the cheapest, so once one is feasible it is
     the cheapest feasible solution
3. Reduce the temperature once per level, and with `PenaltyUpdate.cooling` raise the penalty
4. Stop early if there are no possible moves

### 2. Neighborhood Exploration

**Location**: `csharp/algorithm/solver/Explorer.cs`

Key functions:
- `Mover()`: Relocates a uniformly random node `b` from `a -> b -> c` to between `x -> y`,
  the 3-edge exchange `a->b, b->c, x->y` => `a->c, x->b, b->y`. Returns the changed successors,
  always a valid circuit, and never changes a successor that is `null` in the neighborhood.
- `NeighborhoodSelector()`: Sets the successors of the given (fixed) indexes to `null`.
  The CLI fixes `Reindexer.EndDepotIndexes`, since an end depot is always followed by the next
  vehicle's start depot.
- `CostObjective()`: Sum of `m[i][successor[i]]` for a circuit, or of consecutive pairs for a route

### 3. Solution Representations

Two equivalent representations with bidirectional conversion. Both validate in the constructor,
copy their input and expose it read-only.

#### Circuit Representation

**Location**: `csharp/algorithm/constraint/Circuit.cs`

```csharp
public class Circuit
{
    public IReadOnlyList<int> Successors { get; }  // Successors[i] = next node after i

    public static bool Valid(IReadOnlyList<int> circuit)  // a single hamiltonian circuit
    public Route ToRoute()                                // visit order starting from node 0
}
```

#### Route Representation

**Location**: `csharp/algorithm/constraint/Route.cs`

```csharp
public class Route
{
    public IReadOnlyList<int> List { get; }  // Ordered sequence of visits

    public static bool Valid(IReadOnlyList<int> ints)  // a permutation of 0..n-1
    public Circuit ToCircuit()
}
```

### 4. Constraint Programming

**Location**: `csharp/algorithm/constraint/Constraints.cs`

```csharp
public static class Constraints
{
    // AllDifferent: All values must be unique
    public static bool AllDifferent(this IReadOnlyCollection<int> ints)
        => ints.Count == ints.ToHashSet().Count;

    // Circuit: ints[i] is the successor of i and following them from 0 visits every node once
    // before returning to 0 (no sub-tours, values in range, implies AllDifferent)
    public static bool Circuit(this IReadOnlyList<int> ints)
}
```

### 5. Data Transformation & Reindexing

**Location**: `csharp/transform/Helper.cs`

**Reindexer class**:
- Handles multi-vehicle routing by replicating the depot
- Maps customer IDs to internal indexes
- **DepotIndexes**: `[0..2*nVehicles)` - Vehicle `i` has start depot `2i` and end depot `2i+1`
- **EndDepotIndexes**: the odd depot indexes, whose successors stay fixed
- **VisitIndexes**: `[2*nVehicles..2*nVehicles+nCustomers)` - Customer visit nodes
- Requires at least one vehicle

**Helper methods**:
- `ToMatrix()`: Converts customer coordinates to Euclidean distance matrix (customers looked up by `Id`)
- `ToCircuit()`: Converts multi-route solution to single circuit
- `Distance()`: Euclidean distance calculation

**VrptwEvaluator** (`csharp/transform/Evaluator.cs`), constructed from the customers, capacity and
`Reindexer`:
- `Evaluate(successors)`: walks the circuit from depot 0 and returns an
  `Evaluation(Distance, Overload, Lateness)` with `Feasible`, `Violation` (overload + lateness)
  and `Penalized(lambda)` (distance + lambda * violation)
- Each vehicle starts at depot `2v` at the depot's `Earliest` with no load. Travel time is the
  unrounded Euclidean distance, a vehicle arriving early waits until `Earliest`, the time after
  `Latest` (the end depot's included) is lateness and the load above the capacity at the end depot
  `2v+1` is overload. `Customer.Cost` is the service time
- `VehiclesUsed(successors)`: the number of non-empty routes

**Construction** (`csharp/transform/Construction.cs`):
- `NearestNeighbor(customers, capacity, nVehicles)`: routes of customer ids for `ToCircuit`, one
  vehicle at a time visiting the customer whose service can start first that keeps the load, its
  window and the return to the depot feasible. Customers left after the last vehicle are appended
  to its route (infeasible, for the solver to repair)

### 6. Data Models

#### Instance

**Location**: `csharp/data_layer/Instance.cs`

```csharp
public class Instance
{
    public string Name { get; set; }
    public int nVehicles { get; set; }       // JSON "vehicle-nr"
    public int Capacity { get; set; }
    public IList<Customer> Customers { get; set; }
}
```

#### Customer

**Location**: `csharp/data_layer/Customer.cs`

```csharp
public class Customer
{
    public int Id { get; set; }              // JSON "cust-nr"
    public int X, Y { get; set; }            // Coordinates
    public (int x, int y) Coords => (X, Y);  // Tuple accessor
    public int Demand { get; set; }
    public int Earliest, Latest { get; set; } // Time window
    public int Cost { get; set; }             // Service time
}
```

#### Result

**Location**: `csharp/data_layer/Result.cs`

```csharp
public class Result
{
    public string Instance { get; set; }
    public string Authors { get; set; }
    public string Date { get; set; }
    public string Reference { get; set; }
    public ICollection<ICollection<int>> Solution { get; set; }
}
```

### 7. Benchmark Loading

**Location**: `csharp/cli/Loader.cs`

```csharp
public class Loader
{
    public Loader(string type = "c", string version = "1", string nr = "01")

    // Loads from {AppContext.BaseDirectory}/solomon-vrptw-benchmarks/{type}/{version}/{name}.json
    public Instance Instance()

    // Loads from solomon-vrptw-benchmarks/results/{name}.json
    public Result Result()
}
```

**Solomon Benchmark Structure**:
- Type: `c` (clustered), `r` (random), `rc` (random-clustered)
- Version: `1` (short horizon), `2` (long horizon)
- Number: `01` through `08` (different instance variants)

Example: `C101` = Clustered, version 1, instance 01

## Testing Approach

### Unit Tests

The `csharp/tests` xUnit project covers:

- `algorithm/ConstraintsTests.cs`: AllDifferent, Circuit (sub-tours, out of range, self-loops)
- `algorithm/RepresentationTests.cs`: Circuit <-> Route conversion and validation
- `algorithm/ExplorerTests.cs`: the relocate move always yields a valid circuit, respects fixed
  successors and can pick every node
- `algorithm/SimulatedAnnealingTests.cs`: finds the optimal circuit of points on a circle with
  every reduction function, keeps the best solution, stops without moves, is reproducible by
  seed and rejects schedules that never end
- `algorithm/SimulatedAnnealingTests.cs` also checks that the best feasible solution is kept,
  reached from an infeasible start and beats a cheaper infeasible one, that without feasible
  solutions the least violating is returned with `FoundFeasible` false, that the penalty steers
  the search and that cooling raises it
- `transform/HelperTests.cs`: Reindexer, ToCircuit, ToMatrix
- `transform/EvaluatorTests.cs`: the best known C101 solution is feasible (828.94, 10 vehicles),
  exact overload and lateness, waiting, returning late, empty routes
- `transform/ConstructionTests.cs`: C101 is routed feasibly in the Reindexer layout, the nearest
  choice counts waiting, returning for capacity, windows and the depot's `Latest`
- `cli/LoaderTests.cs`: loads C101 and checks the best known cost of 828.94

`algorithm` exposes its internals to `tests` (`InternalsVisibleTo`) so `Explorer.Mover` can be tested.

```bash
cd /home/user/simulated-annealing/csharp
dotnet test
```

### Inline Validation

`csharp/cli/Program.cs` also runs a few `Debug.Assert` checks of the Circuit/Route conversion and the
C101 best known cost at startup (only for C101), and `SimulatedAnnealing` asserts each candidate is a valid circuit.
Run in Debug mode to enable them.

## Common Development Tasks

### Adding a New Temperature Reduction Strategy

1. Add enum value to `ReductionFunction` in `csharp/algorithm/solver/SimulatedAnnealing.cs`
2. Create reduction method following pattern:
   ```csharp
   private void MyNewReduction() => _currTemp = /* formula */;
   ```
3. Add case to switch expression in constructor:
   ```csharp
   _decrementRule = tempReduction switch
   {
       // ...
       ReductionFunction.myNew when /* parameters valid */ => MyNewReduction,
       _ => throw new ArgumentOutOfRangeException(nameof(tempReduction))
   };
   ```
4. Only accept parameters that strictly decrease the temperature towards `finalTemp`,
   otherwise `Run()` never ends, and add a case to `SimulatedAnnealingTests`

### Adding a New Constraint

1. Add static extension method to `csharp/algorithm/constraint/Constraints.cs`:
   ```csharp
   /// <summary>
   /// The MyConstraint constraint is true IFF ...
   /// </summary>
   public static bool MyConstraint(this ICollection<int> ints)
       => /* validation logic */;
   ```

2. Use in validation contexts (Circuit/Route constructors, assertions)

### Modifying Neighborhood Structure

Edit `csharp/algorithm/solver/Explorer.cs`:

- `Mover()`: Modify neighborhood generation (currently a relocate, a 3-edge exchange)
- Return only valid circuits and leave `null` (fixed) successors unchanged, `ExplorerTests` checks both
- Consider impact on solution space connectivity

### Loading Different Benchmark Instances

Pass the type, version and number to the CLI, C101 by default:

```bash
dotnet run --project cli -- r 2 05    # R205
dotnet run --project cli -- rc 1 08   # RC108
```

The CLI anneals from the best known solution when `solomon-vrptw-benchmarks/results` has one
(7 of the 56 instances don't, e.g. R103), and from the nearest neighbor construction.

### Adjusting SA Parameters

In `csharp/cli/Program.cs`, modify `Lambda` (the initial penalty per unit of overload and lateness)
and the `SimulatedAnnealing` constructor in `Solve`:

```csharp
SimulatedAnnealing solver = new(
    initial,
    evaluate,                    // the distance and the violation
    neighborOperator,
    initialTemp: 10,             // Increase for more exploration, relative to the move deltas
    finalTemp: 0.1,              // Decrease for more exploitation, must be > 0
    tempReduction: ReductionFunction.geometric,  // Try linear, slowDecrease
    iterationPerTemp: 10000,     // Increase for more thorough search
    alpha: 0.95,                 // Geometric factor, closer to 1 cools slower
    penalty: Lambda,             // 10
    penaltyUpdate: PenaltyUpdate.cooling);  // the penalty ends at about 1000
```

From the construction, in about 6.5s per run (Release), over seeds 1-10: C101 reaches the best
known 828.94 in 8 of 10 runs (else 880.48), R101 ends 4.2% and RC101 1.2% above theirs on average.
A higher initial temperature wanders too far to return to a better feasible solution. A constant
penalty of 100 does about as well (better on C101, even on R101, worse on RC101 and 4 of 5 other
instances) but occasionally ends the search infeasible, an adaptive penalty finds slightly better distances on
R101/RC101 but loosens instead of tightening. The best known results minimize the number of
vehicles first, so a distance-only run can also end below them. When no feasible solution is found
the CLI says so and reports the least violating one.

## Known TODOs & Areas for Improvement

### Areas Needing Enhancement

1. **Documentation**
   - No README.md in root directory
2. **CI/CD**
   - No GitHub Actions or CI configuration
3. **Performance**
   - Each iteration copies the successors and evaluates the whole candidate (O(n)); only the two
     routes the relocate move touches need re-walking
4. **Search**
   - The penalty only cools; balancing it adaptively found better distances but loosens the
     constraints, a combination might keep both
   - Only the relocate move, 2-opt* or swap moves could help
   - The vehicle count, which the Solomon rankings minimize first, isn't an objective

## Working with This Codebase

### Key Principles

1. **Separation of Concerns**: Keep data models, algorithms, transformations, and CLI separate
2. **Functional Style**: Prefer LINQ, expression-bodied members, extension methods
3. **Explicit Constraints**: Use constraint programming concepts explicitly (AllDifferent, Circuit)
4. **Clean Abstractions**: Circuit and Route provide clean dual representations
5. **Configurability**: SA algorithm is highly configurable via constructor parameters

### When Making Changes

1. **Read existing code first**: Understand patterns before modifying
2. **Follow the existing files**: 4 spaces; the files are LF without a BOM despite `.editorconfig`
3. **Use expression-bodied members**: For concise methods/properties
4. **Add XML docs**: For public APIs, especially in algorithm/constraint namespaces
5. **Run the tests**: `dotnet test` from `csharp/`, and add tests for new behavior
6. **Test conversions**: When modifying Circuit/Route, verify bidirectional conversion
7. **Validate constraints**: Ensure AllDifferent and Circuit constraints remain satisfied
8. **Commit with convention**: Use `<component>: <description>` format

### Performance Considerations

1. **LINQ overhead**: Extensive LINQ may impact performance in tight loops
2. **Random number generation**: `SimulatedAnnealing` takes a `seed` (default 1) for reproducibility
3. **Distance matrix**: Pre-computed, not recalculated (good)
4. **Per iteration cost**: O(n) for the move, candidate copy, predecessors and evaluation

### Debugging Tips

1. **Enable Debug.Assert**: Run in Debug mode to catch assertion failures
2. **Solution validation**: Use `Circuit.Valid(successors)` to validate solutions
3. **Cost tracking**: Monitor cost improvements in SA loop
4. **Temperature schedule**: Log temperature values to verify reduction strategy
5. **Acceptance rate**: Track accepted/rejected moves to tune parameters

## File Locations Quick Reference

### Entry Points
- Main program: `csharp/cli/Program.cs`
- Solution file: `csharp/sim-an.sln`

### Core Algorithm
- Simulated Annealing: `csharp/algorithm/solver/SimulatedAnnealing.cs`
- Neighborhood Explorer: `csharp/algorithm/solver/Explorer.cs`

### Constraints & Representations
- Constraints: `csharp/algorithm/constraint/Constraints.cs`
- Circuit: `csharp/algorithm/constraint/Circuit.cs`
- Route: `csharp/algorithm/constraint/Route.cs`

### Data Models
- Instance: `csharp/data_layer/Instance.cs`
- Customer: `csharp/data_layer/Customer.cs`
- Result: `csharp/data_layer/Result.cs`

### Utilities
- Transformations: `csharp/transform/Helper.cs`
- VRPTW evaluation: `csharp/transform/Evaluator.cs`
- Initial solution: `csharp/transform/Construction.cs`
- Benchmark Loader: `csharp/cli/Loader.cs`

### Configuration
- Code style: `csharp/.editorconfig`
- ReSharper: `csharp/sim-an.sln.DotSettings`
- Git ignores: `.gitignore`, `csharp/.gitignore`

## Resources & References

### Solomon VRPTW Benchmarks
- Submodule: `solomon-vrptw-benchmarks/`
- GitHub: `https://github.com/CervEdin/solomon-vrptw-benchmarks.git`
- Format: JSON files with instances and optimal/best-known solutions

### Algorithm Reference
- **Simulated Annealing**: Metropolis acceptance criterion with temperature schedule
- **VRPTW**: Vehicle Routing Problem with Time Windows (Solomon benchmarks)
- **Constraint Programming**: AllDifferent, Circuit (Hamiltonian cycle)

---

**Last Updated**: 2026-09-26
**Target Framework**: .NET 8.0
**Primary IDE**: JetBrains Rider
