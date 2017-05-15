using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

public static class ProgressZipFile
{
    public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, IProgress<double> progress)
    {
        sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

        var sourceFiles = new DirectoryInfo(sourceDirectoryName).GetFiles("*", SearchOption.AllDirectories);
        double totalBytes = sourceFiles.Sum(f => f.Length);
        long currentBytes = 0;

        using (var archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
        {
            foreach (var file in sourceFiles)
            {
                var entryName = file.FullName.Substring(sourceDirectoryName.Length + 1);
                var entry = archive.CreateEntry(entryName);

                entry.LastWriteTime = file.LastWriteTime;

                using (var inputStream = File.OpenRead(file.FullName))
                using (var outputStream = entry.Open())
                {
                    var progressStream = new ProgressStream(inputStream,
                        new Progress<int>(i =>
                        {
                            currentBytes += i;
                            progress.Report(currentBytes / totalBytes);
                        }), null);

                    progressStream.CopyTo(outputStream);
                }
            }
        }
    }

    public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, IProgress<double> progress)
    {
        using (var archive = ZipFile.OpenRead(sourceArchiveFileName))
        {
            double totalBytes = archive.Entries.Sum(e => e.Length);
            long currentBytes = 0;

            foreach (var entry in archive.Entries)
            {
                var fileName = Path.Combine(destinationDirectoryName, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                using (var inputStream = entry.Open())
                using (var outputStream = File.OpenWrite(fileName))
                {
                    var progressStream = new ProgressStream(outputStream, null,
                        new Progress<int>(i =>
                        {
                            currentBytes += i;
                            progress.Report(currentBytes / totalBytes);
                        }));

                    inputStream.CopyTo(progressStream);
                }

                File.SetLastWriteTime(fileName, entry.LastWriteTime.LocalDateTime);
            }
        }
    }
}