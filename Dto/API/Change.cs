using Newtonsoft.Json;

namespace BasicBot.Dto.API
{
    public class Change
    {
        public ChangeTypes ChangeType { get; set; }

        [JsonProperty("item")]
        public ChangeInfo Info { get; set; }

        public enum ChangeTypes { Add, Edit, Remove}
    }
}