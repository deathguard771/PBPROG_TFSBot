using Newtonsoft.Json;

namespace BasicBot.Dto.API
{
    public class ChangesCollection
    {
        public int Count { get; set; }

        [JsonProperty("value")]
        public Change[] Collection { get; set; }
    }
}