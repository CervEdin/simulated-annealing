using System;
using System.Collections.Generic;
using System.Linq;
using algorithm.constraint;

namespace algorithm.solver
{
    public class Route
    {
        public readonly int[] List;

        public Route(ICollection<int> ints)
        {
            if (!ints.AllDifferent())
                throw new ArgumentOutOfRangeException(nameof(ints));
            List = ints.ToArray();
        }

        public Circuit ToCircuit()
        {
            int[] circuit = new int[List.Length];

            int current_i = 0;
            int current = List[current_i];
            int successor_i = current_i + 1;
            int successor = List[successor_i];
            while (current_i < circuit.Length)
            {
                circuit[current] = successor;
                current = successor;
                successor_i = (successor_i + 1) % circuit.Length;
                successor = List[successor_i];
                current_i++;
            }

            return new Circuit(circuit);
        }

        public bool Valid() => List.AllDifferent();
    }
}