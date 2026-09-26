using System.Collections.Generic;
using System.Linq;
using algorithm.constraint;
using algorithm.solver;
using cli;
using transform;
using Xunit;

namespace tests.cli
{
    public class LoaderTests
    {
        private readonly Loader _loader = new("c", "1", "01");

        [Fact]
        public void Instance()
        {
            var instance = _loader.Instance();
            Assert.Equal("C101", instance.Name);
            Assert.Equal(25, instance.nVehicles);
            Assert.Equal(200, instance.Capacity);
            Assert.Equal(101, instance.Customers.Count);
            Assert.Equal(Enumerable.Range(0, 101), instance.Customers.Select(c => c.Id));
        }

        [Fact]
        public void BestKnownSolutionCost()
        {
            var instance = _loader.Instance();
            var result = _loader.Result();
            Reindexer reindexer = new(instance.nVehicles, instance.Customers.Count - 1);
            Circuit circuit = new Route(result.Solution.ToCircuit(reindexer)).ToCircuit();
            IList<IList<double>> matrix = instance.Customers.ToMatrix(reindexer);
            Assert.Equal(828.94, circuit.CostObjective(matrix), 2);
        }
    }
}
