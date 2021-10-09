namespace TileDownload.CLI.Utils
{
    internal struct ProgressReporter
    {
        public int Count { get; set; }

        public int Total { get; set; }

        public string Message {  get; set; }

        public override string ToString()
        {
            return $"{Message} : {Count}/{Total}";
        }
    }
}
