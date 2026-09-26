using System;
using algorithm.constraint;
using Xunit;

namespace tests.algorithm
{
    public class RepresentationTests
    {
        [Theory]
        [InlineData(new[] {0}, new[] {0})]
        [InlineData(new[] {1, 0}, new[] {0, 1})]
        [InlineData(new[] {1, 2, 0}, new[] {0, 1, 2})]
        [InlineData(new[] {1, 2, 3, 0}, new[] {0, 1, 2, 3})]
        [InlineData(new[] {2, 3, 1, 0}, new[] {0, 2, 1, 3})]
        public void CircuitAndRouteConvert(int[] successors, int[] order)
        {
            Assert.Equal(order, new Circuit(successors).ToRoute().List);
            Assert.Equal(successors, new Route(order).ToCircuit().Successors);
        }

        [Fact]
        public void RouteNotStartingAtZeroIsRotated()
            => Assert.Equal(new[] {0, 1, 2}, new Route(new[] {1, 2, 0}).ToCircuit().ToRoute().List);

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new[] {1, 0, 3, 2})]
        [InlineData(new[] {1, 5, 0})]
        public void InvalidCircuitThrows(int[] successors)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Circuit(successors));

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new[] {0, 0})]
        [InlineData(new[] {0, 2})]
        public void InvalidRouteThrows(int[] order)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Route(order));

        [Fact]
        public void CircuitCopiesItsInput()
        {
            int[] successors = {1, 2, 0};
            Circuit circuit = new(successors);
            successors[0] = 0;
            Assert.Equal(new[] {1, 2, 0}, circuit.Successors);
        }

        [Fact]
        public void Predecessors()
            => Assert.Equal(new[] {3, 2, 0, 1}, new[] {2, 3, 1, 0}.Predecessors());
    }
}
