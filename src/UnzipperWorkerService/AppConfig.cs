namespace UnzipperWorkerService
{
    public class AppConfig
    {
        public string ZipFileFilter { get; set; }
        public string InputDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public bool DeleteFileAfterUnzip { get; set; }
        public int WorkerTaskCount { get; set; }
    }
}