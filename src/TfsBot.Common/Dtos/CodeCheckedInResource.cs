using System;

namespace TfsBot.Common.Dtos
{
    public class CodeCheckedInResource
    {
        public string ChangesetId { get; set; }

        public string Url { get; set; }

        public Author Author { get; set; }

        public Author CheckedInBy { get; set; }

        public string Comment { get; set; }
        
        public DateTime CreatedDate { get; set; }
    }

    public class Author
    {
        public string DisplayName { get; set; }
        public string UniqueName { get; set; }
    }
}