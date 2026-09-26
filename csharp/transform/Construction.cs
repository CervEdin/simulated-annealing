using System;
using System.Collections.Generic;
using System.Linq;
using data_layer;

// ReSharper disable once CheckNamespace
namespace transform
{
    public static class Construction
    {
        /// <summary>
        /// Build routes one vehicle at a time, each time visiting the unrouted customer whose service can start first,
        /// the nearest once waiting for Earliest is counted, that keeps the load within the capacity, is reached by
        /// its Latest and still allows returning to the depot by its Latest. A vehicle returns when no customer fits.
        /// Customers left after the last vehicle are appended to its route, which makes the solution infeasible.
        /// </summary>
        /// <returns>The routes as customer ids, at most nVehicles, for Helper.ToCircuit</returns>
        public static List<List<int>> NearestNeighbor(
            IList<Customer> customers,
            int capacity,
            int nVehicles
        )
        {
            if (nVehicles < 1)
                throw new ArgumentOutOfRangeException(nameof(nVehicles));
            Customer depot = customers.Single(c => c.Id == 0);
            var unrouted = customers.Where(c => c.Id != 0).ToList();
            List<List<int>> routes = new();
            while (unrouted.Count > 0 && routes.Count < nVehicles)
            {
                List<int> route = new();
                Customer at = depot;
                double t = depot.Earliest;
                int load = 0;
                while (true)
                {
                    Customer from = at;
                    double start = t;
                    int loaded = load;
                    (Customer customer, double arrival) next = unrouted
                        .Where(c => loaded + c.Demand <= capacity)
                        .Select(c => (c, arrival: start + Helper.Distance(from.Coords, c.Coords)))
                        .Where(tp => tp.arrival <= tp.c.Latest
                                     && Math.Max(tp.arrival, tp.c.Earliest) + tp.c.Cost
                                     + Helper.Distance(tp.c.Coords, depot.Coords) <= depot.Latest)
                        .OrderBy(tp => Math.Max(tp.arrival, tp.c.Earliest))
                        .ThenBy(tp => tp.arrival)
                        .ThenBy(tp => tp.c.Id)
                        .FirstOrDefault();
                    if (next.customer == null)
                        break;
                    route.Add(next.customer.Id);
                    unrouted.Remove(next.customer);
                    at = next.customer;
                    t = Math.Max(next.arrival, at.Earliest) + at.Cost;
                    load += at.Demand;
                }

                // A customer that doesn't fit in an empty vehicle won't fit in the next one either
                if (route.Count == 0)
                    break;
                routes.Add(route);
            }

            if (unrouted.Count > 0)
            {
                if (routes.Count == 0)
                    routes.Add(new List<int>());
                routes[^1].AddRange(unrouted.Select(c => c.Id));
            }

            return routes;
        }
    }
}
