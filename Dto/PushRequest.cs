using System.Collections.Generic;
using Newtonsoft.Json;

namespace BasicBot.Dto
{
    public class PushRequest
    {
        public string Before { get; set; }

        public string After { get; set; }

        public string Ref { get; set; }

        [JsonProperty("user_name")]
        public string UserName { get; set; }

        public int TotalCommitsCount { get; set; }

        public IEnumerable<Commit> Commits { get; set; }
    }
}
