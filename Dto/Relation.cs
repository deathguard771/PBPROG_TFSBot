using System.Collections.Generic;

namespace BasicBot.Dto
{
    public class Relation
    {
        public string Rel { get; set; }

        public string Url { get; set; }

        public IDictionary<string, string> Attributes { get; set; }
    }
}