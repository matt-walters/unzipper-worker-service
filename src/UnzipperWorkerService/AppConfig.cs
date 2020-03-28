namespace UnzipperWorkerService
{
    public class AppConfig
    {
        public string SourceFolderPath { get; set; }
        public string DestinationFolderPath { get; set; }
        public bool DeleteFileAfterUnzip { get; set; }
        public int WorkerTaskCount { get; set; }
    }
}