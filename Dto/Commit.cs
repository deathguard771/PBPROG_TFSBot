namespace BasicBot.Dto
{
    public class Commit
    {
        public string ID { get; set; }

        public string Message { get; set; }

        public string Url { get; set; }

        public string[] Added { get; set; }

        public string[] Modified { get; set; }

        public string[] Removed { get; set; }

        public GitLabAuthor Author { get; set; }
    }
}