using System;
using System.Linq;
using algorithm.constraint;
using data_layer;
using transform;
using Xunit;

namespace tests.transform
{
    public class HelperTests
    {
        // 3 vehicles => depots 0..5, customers 1..4 => indexes 6..9
        private readonly Reindexer _reindexer = new(3, 4);

        [Fact]
        public void Reindexer()
        {
            Assert.Equal(Enumerable.Range(0, 6), _reindexer.DepotIndexes);
            Assert.Equal(new[] {1, 3, 5}, _reindexer.EndDepotIndexes);
            Assert.Equal(Enumerable.Range(0, 10), _reindexer.AllIndexes);
            Assert.Equal(
                new[] {0, 0, 0, 0, 0, 0, 1, 2, 3, 4},
                _reindexer.AllIndexes.Select(_reindexer.CustomerId));
            Assert.Equal(new[] {8}, _reindexer.CustomerIndexes(3));
            Assert.Equal(_reindexer.DepotIndexes, _reindexer.CustomerIndexes(0));
        }

        [Fact]
        public void ReindexerNeedsAVehicle()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Reindexer(0, 4));

        [Fact]
        public void ToCircuit()
        {
            int[] order = new[] {new[] {2, 1}, new[] {4, 3}}.ToCircuit(_reindexer).ToArray();
            // vehicle 3 is unused and goes straight from start to end depot
            Assert.Equal(new[] {0, 7, 6, 1, 2, 9, 8, 3, 4, 5}, order);
            Assert.True(Route.Valid(order));
        }

        [Fact]
        public void ToCircuitRejectsUnknownCustomers()
            => Assert.Throws<InvalidOperationException>(
                () => new[] {new[] {5}}.ToCircuit(_reindexer).ToArray());

        [Fact]
        public void ToMatrixLooksCustomersUpById()
        {
            Customer[] customers =
            {
                new() {Id = 2, X = 3, Y = 4},
                new() {Id = 0, X = 0, Y = 0},
                new() {Id = 1, X = 0, Y = 1}
            };
            double[][] matrix = customers.ToMatrix(new Reindexer(1, 2));
            // indexes: depot 0, depot 1, customer 1, customer 2
            Assert.Equal(new double[] {0, 0, 1, 5}, matrix[0]);
            Assert.Equal(new double[] {5, 5, Math.Sqrt(18), 0}, matrix[3]);
        }
    }
}
