using System.Text.Json.Serialization;

namespace data_layer
{
    public class Customer
    {
        [JsonPropertyName("customer-nr")]
        public int Id { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public ( int x, int y) Coords => (X, Y);
        public int Demand { get; set; }
        public int Earliest { get; set; }
        public int Latest { get; set; }
        public int Cost { get; set; }
    }
}
