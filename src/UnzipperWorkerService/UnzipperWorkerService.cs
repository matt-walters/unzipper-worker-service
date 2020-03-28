using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnzipperWorkerService
{
    class UnzipperWorkerService : BackgroundService
    {
        private readonly IOptions<AppConfig> config;
        private readonly ILogger<UnzipperWorkerService> logger;
        private readonly List<Task> unzipperTasks = new List<Task>();

        private FileSystemWatcher fileSystemWatcher;
        private BlockingCollection<string> filesToUnzip;

        public UnzipperWorkerService(IOptions<AppConfig> config, ILogger<UnzipperWorkerService> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogConfigValues();

            filesToUnzip = new BlockingCollection<string>();

            fileSystemWatcher = new FileSystemWatcher(config.Value.SourceFolderPath, config.Value.ZipFileFilter);
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.IncludeSubdirectories = false;

            for (int i = 0; i < config.Value.WorkerTaskCount; i++)
            {
                unzipperTasks.Add(
                    Task.Factory.StartNew(
                        () => UnzipperWorker(stoppingToken), stoppingToken));
            }

            await Task.WhenAll(unzipperTasks);
            fileSystemWatcher.Dispose();
        }

        private async void UnzipperWorker(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    var fullPath = filesToUnzip.Take(stoppingToken);
                    await UnzipFile(fullPath, stoppingToken);
                }
                catch(OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            filesToUnzip.Add(e.FullPath);
            logger.LogInformation($"Added file: {e.FullPath}");
            logger.LogInformation($"Files to unzip: {filesToUnzip.Count}.");
        }

        private async Task UnzipFile(string fullPath, CancellationToken stoppingToken)
        {
            var name = Path.GetFileName(fullPath);
            var destinationFilePath = Path.Combine(config.Value.DestinationFolderPath, Path.GetFileNameWithoutExtension(fullPath));

            await WaitUntilFileIsUnlocked(fullPath, stoppingToken);

            try
            {
                ZipFile.ExtractToDirectory(fullPath, destinationFilePath, true);

                if (config.Value.DeleteFileAfterUnzip)
                {
                    File.Delete(fullPath);
                }

                logger.LogInformation($"Unzipped {name} ({filesToUnzip.Count} files remaining).");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to unzip {name}. Reason: {ex.Message}");
            }
        }

        private async Task WaitUntilFileIsUnlocked(string fileName, CancellationToken stoppingToken)
        {
            while(IsFileLocked(fileName))
            {
                stoppingToken.ThrowIfCancellationRequested();
                await Task.Delay(500, stoppingToken);
            }
        }

        private bool IsFileLocked(string fileName)
        {
            var file = new FileInfo(fileName);

            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        private void LogConfigValues()
        {
            var message = 
                $"ZipFileFilter:         {config.Value.ZipFileFilter}" + Environment.NewLine + 
                $"WorkerTaskCount:       {config.Value.WorkerTaskCount}" + Environment.NewLine + 
                $"SourceFolderPath:      {config.Value.SourceFolderPath}" + Environment.NewLine + 
                $"DestinationFolderPath: {config.Value.DestinationFolderPath}" + Environment.NewLine + 
                $"DeleteFileAfterUnzip:  {config.Value.DeleteFileAfterUnzip}";

            logger.LogInformation(message);
        }
    }
}
