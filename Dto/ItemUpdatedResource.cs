using System.Collections.Generic;

namespace BasicBot.Dto
{
    public class ItemUpdatedResource
    {
        public string Id { get; set; }

        public string WorkItemId { get; set; }

        public string Rev { get; set; }

        public IDictionary<string, ItemField> Fields { get; set; }
    }
}