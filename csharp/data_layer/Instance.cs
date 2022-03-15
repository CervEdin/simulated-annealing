using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace data_layer
{
    public class Instance
    {
        [JsonPropertyName("instance")]
        public string Name { get; set; }

        public int nVehicles { get; set; }

        public int Capacity { get; set; }
        public IList<Customer> Customers { get; set; }
    }
}