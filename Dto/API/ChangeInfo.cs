using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace BasicBot.Dto.API
{
    public class ChangeInfo
    {
        private string _path;

        public int Version { get; set; }

        public int Size { get; set; }

        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
                var match = Regex.Match(value ?? string.Empty, @"\$/([^/]+)/([^/]+)");
                if (match.Success)
                {
                    RootName = match.Groups[1].Value;
                    BranchName = match.Groups[2].Value;
                }
                else
                {
                    RootName = null;
                    BranchName = null;
                }
            }
        }

        public string Url { get; set; }

        [JsonIgnore]
        public string RootName { get; private set; }

        [JsonIgnore]
        public string BranchName { get; private set; }
    }
}