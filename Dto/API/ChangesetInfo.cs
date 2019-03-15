using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BasicBot.Dto.API
{
    public class ChangesetInfo
    {
        public string ChangesetID { get; set; }

        public Author Author { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }
}
