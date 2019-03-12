using System;
using Newtonsoft.Json;

namespace BasicBot.Dto
{
    public class Message
    {
        public string Text { get; set; }

        public string Html { get; set; }

        public string Markdown { get; set; }

        [JsonIgnore]
        public string TrimmedMarkdown => Markdown.Replace($"{Environment.NewLine}{Environment.NewLine}", Environment.NewLine);
    }
}
