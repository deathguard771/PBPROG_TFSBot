using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Dto
{
    public class ItemUpdatedRequest : Request
    {
        public ItemUpdatedResource Resource { get; set; }
    }
}
