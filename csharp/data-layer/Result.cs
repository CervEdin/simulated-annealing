using System.Collections.Generic;

namespace data_layer
{
    public class Result
    {
        public string Instance { get; set; }
        public string Authors { get; set; }
        public string Date { get; set; }
        public string Reference { get; set; }
        public ICollection<ICollection<int>> Solution { get; set; }
    }
}
