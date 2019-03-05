using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Dto
{
    public class WorkItem
    {
        public string WebUrl { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public string WorkItemType { get; set; }

        public string State { get; set; }

        public string AssignedTo { get; set; }
    }
}
