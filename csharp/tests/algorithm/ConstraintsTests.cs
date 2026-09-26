using algorithm.constraint;
using Xunit;

namespace tests.algorithm
{
    public class ConstraintsTests
    {
        [Theory]
        [InlineData(new[] {0, 1, 2}, true)]
        [InlineData(new[] {0, 0, 2}, false)]
        [InlineData(new int[0], true)]
        public void AllDifferent(int[] ints, bool expected)
            => Assert.Equal(expected, ints.AllDifferent());

        [Theory]
        [InlineData(new[] {0}, true)]
        [InlineData(new[] {1, 0}, true)]
        [InlineData(new[] {1, 2, 0}, true)]
        [InlineData(new[] {2, 0, 1}, true)]
        [InlineData(new[] {3, 2, 0, 1}, true)]
        [InlineData(new int[0], false)]
        [InlineData(new[] {1, 0, 3, 2}, false)] // two sub-tours
        [InlineData(new[] {0, 2, 1}, false)] // self-loop and a sub-tour
        [InlineData(new[] {1, 1, 0}, false)] // not all different
        [InlineData(new[] {1, 2, 1}, false)] // 0 is never returned to
        [InlineData(new[] {1, 5, 0}, false)] // out of range
        [InlineData(new[] {1, -1, 0}, false)] // out of range
        public void Circuit(int[] ints, bool expected)
            => Assert.Equal(expected, ints.Circuit());
    }
}
