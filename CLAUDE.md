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
5. **Computes costs** using Euclidean distance matrices
6. **Handles multi-vehicle routing** via depot replication and reindexing

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
    │   ├── data_layer.csproj     # Depends: Newtonsoft.Json 13.0.1
    │   ├── Instance.cs           # VRPTW problem instance model
    │   ├── Customer.cs           # Customer with coordinates, demand, time windows
    │   └── Result.cs             # Solution result model
    │
    ├── algorithm/                # Core optimization algorithms
    │   ├── algorithm.csproj
    │   ├── solver/
    │   │   ├── SimulatedAnnealing.cs  # Main SA algorithm (125 lines)
    │   │   └── Explorer.cs            # Neighborhood exploration (147 lines)
    │   └── constraint/
    │       ├── Constraints.cs    # AllDifferent & Circuit constraints (30 lines)
    │       ├── Circuit.cs        # Circuit representation (52 lines)
    │       └── Route.cs          # Route representation (42 lines)
    │
    └── transform/                # Data transformation utilities
        ├── transform.csproj
        └── Helper.cs             # Distance matrix, reindexing (107 lines)
```

**Total:** ~675 lines of C# code across 11 source files

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
 └─→ Newtonsoft.Json 13.0.1

transform (library)
 └─→ data_layer
```

## Technology Stack

- **Language**: C# (.NET 5.0)
- **Framework**: .NET 5.0 (`net5.0` target)
- **Dependencies**:
  - Newtonsoft.Json 13.0.1 (JSON serialization)
  - System.Text.Json (CLI JSON operations)
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

# Run the CLI
dotnet run --project cli
```

### Solution Configuration

- **Configurations**: Debug|Any CPU, Release|Any CPU
- **Output**: CLI project produces an executable, others are class libraries
- **Asset Copying**: `solomon-vrptw-benchmarks/**/*.json` files copied to output directory

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
  algo/constraint:  Turn validation method into function
  algo/solver:      Move functionality into Explorer helper class
  cli:              Include the solomon benchmarks/results *.json
  data_layer:       Rename data-layer => data_layer
  doc:              Add documentation to Constraints
  conf:             Update .editorconfig settings
  wip:              Work in progress commits
```

**Components**: `bench`, `algo/constraint`, `algo/solver`, `cli`, `data_layer`, `conf`, `doc`, `wip`

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
    ReductionFunction.linear => LinearTempReduction,
    ReductionFunction.geometric => GeometricTempReduction,
    ReductionFunction.slowDecrease => SlowDecreaseTempReduction,
    _ => throw new ArgumentOutOfRangeException(nameof(tempReduction))
};
```

#### 5. Tuple Deconstruction

```csharp
(int current, int succ) = EdgePicker(successors, _random);
var (nextSuccessors, delta) = Mover(successors, current, succ, _random);
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

**Location**: `csharp/algorithm/solver/SimulatedAnnealing.cs:125`

```csharp
public class SimulatedAnnealing
{
    public SimulatedAnnealing(
        Circuit initialSolution,
        Func<IEnumerable<int>, double> solutionEvaluator,
        Func<IEnumerable<int>, IList<int?>> neighborhoodSelector,
        int initialTemp = 10,
        int finalTemp = 1,
        ReductionFunction tempReduction = ReductionFunction.geometric,
        int iterationPerTemp = 100)

    public Circuit Run()  // Main optimization loop
}

public enum ReductionFunction
{
    linear,         // _currTemp -= _alpha
    geometric,      // _currTemp *= 1 / _alpha
    slowDecrease    // _currTemp /= 1 + _beta * _currTemp
}
```

**Algorithm Flow**:
1. Start with initial solution and temperature
2. For each temperature level:
   - Generate neighbors via neighborhood selector
   - Evaluate cost delta
   - Accept if improvement OR probabilistically if worse
   - Track best solution found
3. Reduce temperature according to strategy
4. Continue until final temperature reached

### 2. Neighborhood Exploration

**Location**: `csharp/algorithm/solver/Explorer.cs:147`

Key functions:
- `EdgePicker()`: Randomly selects an edge (current→successor) to modify
- `Mover()`: Generates neighborhood via 3-edge exchange
- `NeighborhoodSelector()`: Filters depot nodes from modification
- `CostObjective()`: Calculates total route cost from distance matrix

### 3. Solution Representations

Two equivalent representations with bidirectional conversion:

#### Circuit Representation

**Location**: `csharp/algorithm/constraint/Circuit.cs:52`

```csharp
public class Circuit
{
    public IList<int?> Successors { get; }  // successors[i] = next node after i

    // Validates Hamiltonian circuit constraint
    // Converts to Route via ToRoute()
}
```

#### Route Representation

**Location**: `csharp/algorithm/constraint/Route.cs:42`

```csharp
public class Route
{
    public IList<int> List { get; }  // Ordered sequence of visits

    // Validates AllDifferent constraint
    // Converts to Circuit via ToCircuit()
}
```

### 4. Constraint Programming

**Location**: `csharp/algorithm/constraint/Constraints.cs:30`

```csharp
public static class Constraints
{
    // AllDifferent: All values must be unique
    public static bool AllDifferent(this ICollection<int> ints)
        => ints.Count == ints.Distinct().Count();

    // Circuit: Represents Hamiltonian circuit (no self-loops, all different)
    public static bool Circuit(this ICollection<int> ints)
        => ints.AllDifferent() && !ints.Select((x, i) => (x, i)).Any(tp => tp.x == tp.i);
}
```

### 5. Data Transformation & Reindexing

**Location**: `csharp/transform/Helper.cs:107`

**Reindexer class**:
- Handles multi-vehicle routing by replicating the depot
- Maps customer IDs to internal indexes
- **DepotIndexes**: `[0..2*nVehicles)` - Each vehicle gets a start/end depot pair
- **VisitIndexes**: `[2*nVehicles..nCustomers]` - Customer visit nodes

**Helper methods**:
- `ToMatrix()`: Converts customer coordinates to Euclidean distance matrix
- `ToCircuit()`: Converts multi-route solution to single circuit
- `Distance()`: Euclidean distance calculation

### 6. Data Models

#### Instance

**Location**: `csharp/data_layer/Instance.cs`

```csharp
public class Instance
{
    public string Name { get; set; }
    public int nVehicles { get; set; }
    public int Capacity { get; set; }
    public IList<Customer> Customers { get; set; }
}
```

#### Customer

**Location**: `csharp/data_layer/Customer.cs`

```csharp
public class Customer
{
    public int Id { get; set; }              // customer-nr
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

    // Loads from solomon-vrptw-benchmarks/{type}/{version}/{name}.json
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

### Current Status

**No formal testing framework** is currently configured. The project uses inline test assertions.

### Inline Testing

**Location**: `csharp/cli/Program.cs`

```csharp
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
    // ... more test cases
}
```

Called in `Main()` via `Test();`

### Validation

Uses `Debug.Assert()` throughout codebase (6 occurrences) for runtime validation:
- Circuit/Route conversion correctness
- Constraint validation
- Neighborhood generation validity

### Recommendations for Testing

If adding formal tests, consider:

1. **Add xUnit/NUnit project**:
   ```bash
   dotnet new xunit -n tests
   dotnet sln add tests/tests.csproj
   ```

2. **Test Coverage Areas**:
   - Constraint validation (AllDifferent, Circuit)
   - Circuit ↔ Route conversion
   - Reindexer mapping logic
   - Distance matrix generation
   - Neighborhood generation
   - Temperature reduction strategies
   - Solution acceptance logic

## Common Development Tasks

### Adding a New Temperature Reduction Strategy

1. Add enum value to `ReductionFunction` in `csharp/algorithm/solver/SimulatedAnnealing.cs:10`
2. Create reduction method following pattern:
   ```csharp
   private void MyNewReduction() => _currTemp = /* formula */;
   ```
3. Add case to switch expression in constructor:
   ```csharp
   _decrementRule = tempReduction switch
   {
       // ...
       ReductionFunction.myNew => MyNewReduction,
       _ => throw new ArgumentOutOfRangeException(nameof(tempReduction))
   };
   ```

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

- `EdgePicker()`: Change edge selection strategy
- `Mover()`: Modify neighborhood generation (currently 3-edge exchange)
- Consider impact on solution space connectivity

### Loading Different Benchmark Instances

In `csharp/cli/Program.cs`, modify `Loader` initialization:

```csharp
// Current: C101
Loader ld = new("c", "1", "01");

// For R205:
Loader ld = new("r", "2", "05");

// For RC108:
Loader ld = new("rc", "1", "08");
```

### Adjusting SA Parameters

In `csharp/cli/Program.cs`, modify `SimulatedAnnealing` constructor:

```csharp
SimulatedAnnealing sa = new(
    initialSolution: /* Circuit */,
    solutionEvaluator: /* Func */,
    neighborhoodSelector: /* Func */,
    initialTemp: 10,              // Increase for more exploration
    finalTemp: 1,                 // Decrease for more exploitation
    tempReduction: ReductionFunction.geometric,  // Try linear, slowDecrease
    iterationPerTemp: 100         // Increase for more thorough search
);
```

## Known TODOs & Areas for Improvement

### Active TODOs in Code

1. `csharp/algorithm/solver/Explorer.cs:23` - "TODO: >= or >"
2. `csharp/algorithm/solver/Explorer.cs:31` - "TODO tidy"
3. `csharp/algorithm/solver/Explorer.cs:42` - "TODO: verify"
4. `csharp/algorithm/solver/Explorer.cs:47` - "TODO: Optimize"
5. `csharp/algorithm/constraint/Circuit.cs:21` - "TODO: Needed? Circuit implies AllDifferent"
6. `csharp/algorithm/constraint/Circuit.cs:24` - "TODO: test"

### Areas Needing Enhancement

1. **Documentation**
   - No README.md in root directory
   - Minimal XML documentation comments (only 3 instances)
   - Consider adding algorithm explanations

2. **Testing**
   - No formal testing framework (xUnit/NUnit/MSTest)
   - Add unit tests for core algorithms
   - Add integration tests for end-to-end solving

3. **CI/CD**
   - No GitHub Actions or CI configuration
   - Consider adding automated build/test workflows

4. **Benchmark Submodule**
   - May need initialization: `git submodule update --init`
   - Verify JSON files are accessible

5. **Code Coverage**
   - No coverage tooling configured
   - Consider adding coverlet for .NET code coverage

6. **Time Window Constraints**
   - Customer model has `Earliest`/`Latest` time windows
   - Not currently enforced in solver
   - Consider adding time window validation

7. **Capacity Constraints**
   - Instance has `Capacity` and customers have `Demand`
   - Not currently enforced in solver
   - Consider adding capacity validation

## Working with This Codebase

### Key Principles

1. **Separation of Concerns**: Keep data models, algorithms, transformations, and CLI separate
2. **Functional Style**: Prefer LINQ, expression-bodied members, extension methods
3. **Explicit Constraints**: Use constraint programming concepts explicitly (AllDifferent, Circuit)
4. **Clean Abstractions**: Circuit and Route provide clean dual representations
5. **Configurability**: SA algorithm is highly configurable via constructor parameters

### When Making Changes

1. **Read existing code first**: Understand patterns before modifying
2. **Follow .editorconfig**: Respect formatting conventions (4 spaces, UTF-8 BOM, CRLF)
3. **Use expression-bodied members**: For concise methods/properties
4. **Add XML docs**: For public APIs, especially in algorithm/constraint namespaces
5. **Update TODOs**: Address or remove TODO comments when working in those areas
6. **Test conversions**: When modifying Circuit/Route, verify bidirectional conversion
7. **Validate constraints**: Ensure AllDifferent and Circuit constraints remain satisfied
8. **Commit with convention**: Use `<component>: <description>` format

### Performance Considerations

1. **LINQ overhead**: Extensive LINQ may impact performance in tight loops
2. **Random number generation**: `Explorer` uses `Random` - consider seed for reproducibility
3. **Distance matrix**: Pre-computed, not recalculated (good)
4. **Neighborhood size**: Currently generates all neighbors - may be memory-intensive for large instances

### Debugging Tips

1. **Enable Debug.Assert**: Run in Debug mode to catch assertion failures
2. **Solution validation**: Use `Circuit.Successors.Circuit()` to validate solutions
3. **Cost tracking**: Monitor cost improvements in SA loop
4. **Temperature schedule**: Log temperature values to verify reduction strategy
5. **Acceptance rate**: Track accepted/rejected moves to tune parameters

## File Locations Quick Reference

### Entry Points
- Main program: `csharp/cli/Program.cs`
- Solution file: `csharp/sim-an.sln`

### Core Algorithm
- Simulated Annealing: `csharp/algorithm/solver/SimulatedAnnealing.cs:125`
- Neighborhood Explorer: `csharp/algorithm/solver/Explorer.cs:147`

### Constraints & Representations
- Constraints: `csharp/algorithm/constraint/Constraints.cs:30`
- Circuit: `csharp/algorithm/constraint/Circuit.cs:52`
- Route: `csharp/algorithm/constraint/Route.cs:42`

### Data Models
- Instance: `csharp/data_layer/Instance.cs`
- Customer: `csharp/data_layer/Customer.cs`
- Result: `csharp/data_layer/Result.cs`

### Utilities
- Transformations: `csharp/transform/Helper.cs:107`
- Benchmark Loader: `csharp/cli/Loader.cs`

### Configuration
- Code style: `csharp/.editorconfig`
- ReSharper: `csharp/sim-an.sln.DotSettings`
- Git ignores: `.gitignore`, `csharp/.gitignore`

## Resources & References

### Solomon VRPTW Benchmarks
- Submodule: `solomon-vrptw-benchmarks/`
- GitHub: `git@github.com:CervEdin/solomon-vrptw-benchmarks.git`
- Format: JSON files with instances and optimal/best-known solutions

### Algorithm Reference
- **Simulated Annealing**: Metropolis acceptance criterion with temperature schedule
- **VRPTW**: Vehicle Routing Problem with Time Windows (Solomon benchmarks)
- **Constraint Programming**: AllDifferent, Circuit (Hamiltonian cycle)

---

**Last Updated**: 2025-11-23
**Total Lines of C# Code**: ~675 lines across 11 files
**Target Framework**: .NET 5.0
**Primary IDE**: JetBrains Rider
