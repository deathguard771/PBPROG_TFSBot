using Newtonsoft.Json;

namespace BasicBot.Dto.API
{
    public class Change
    {
        public string ChangeType { get; set; }

        [JsonProperty("item")]
        public ChangeInfo Info { get; set; }
    }
}