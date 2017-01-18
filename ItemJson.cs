using System.Collections.Generic;

namespace EloBuddy.ItemIdEnumGenerator
{
    internal class ItemJson
    {
        // ReSharper disable InconsistentNaming
        public Dictionary<int, Data> data { get; set; }
        
        public class Data
        {
            public string name { get; set; }
            public List<int> from { get; set; }
        }
        // ReSharper enable InconsistentNaming
    }
}
