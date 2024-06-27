using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderBackupApplication
{
    public class FileRecord : IEquatable<FileRecord>
    {
        public string FileName { get; set; }
        public DateTime LastWriteTime { get; set; }

        public FileRecord(string filename, DateTime lastwritetime)
        {
            FileName = filename;
            LastWriteTime = lastwritetime;
        }

        public bool Equals(FileRecord other)
        {
            if (other is null)
                return false;

            return this.FileName == other.FileName && this.LastWriteTime == other.LastWriteTime;
        }

        public override bool Equals(object obj) => Equals(obj as FileRecord);
        public override int GetHashCode() => (FileName, LastWriteTime).GetHashCode();
    }

    public class BackupService()
    {
        public async Task Backup(string folderlocation, string backuplocation, string logfilelocation)
        {
            Console.WriteLine("{0} - synchronizing backup folder", DateTime.Now.ToLongTimeString());

            DirectoryInfo folderdir = new DirectoryInfo(folderlocation);

            if (!folderdir.Exists)
            {
                Console.WriteLine("The folder directory does not exist - {0}", folderdir.FullName);
                Console.WriteLine("{0} - Synchronization Completed unsucessfully \n", DateTime.Now.ToLongTimeString());
                Log.Error("Backup sync failed - The folder directory does not exist - {0}", folderdir.FullName);
                return;
            }

            FileInfo[] Files = folderdir.GetFiles("*", SearchOption.AllDirectories);

            DirectoryInfo backupdir = new DirectoryInfo(backuplocation);

            if (!backupdir.Exists)
            {
                Console.WriteLine("The backup directory does not exist - {0}", backupdir.FullName);
                Console.WriteLine("{0} - Synchronization Completed unsucessfully \n", DateTime.Now.ToLongTimeString());
                Log.Error("Backup sync failed - The backup directory does not exist - {0}", folderdir.FullName);
                return;
            }

            FileInfo[] BackupFiles = backupdir.GetFiles("*", SearchOption.AllDirectories);

            var modifiedFiles = Files
                .Select(file => new FileRecord(file.FullName.Substring(folderlocation.Length + 1), file.LastWriteTime))
                .Except(BackupFiles.Select(file => new FileRecord(file.FullName.Substring(backuplocation.Length + 1), file.LastWriteTime)))
                .Select(e => e.FileName)
                .Distinct();

            foreach (var filename in modifiedFiles)
            {
                try
                {
                    var newPath = Path.Combine(backuplocation, filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    File.Copy(Path.Combine(folderlocation, filename), newPath, true);
                    Console.WriteLine("File copied to backup folder - {0}", filename);
                    Log.Information("File copied to backup folder - {0}", filename);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to copy to backup folder - {0}", filename);
                    Console.WriteLine("Check logfile for more details");
                    Log.Error(e, "Failed to copy to backup folder - " + filename);
                }
            }

            var deletedFiles = BackupFiles
                .Select(file => file.FullName.Substring(folderlocation.Length + 1))
                .Except(Files.Select(file => file.FullName.Substring(folderlocation.Length + 1)))
                .Distinct();

            foreach (var filename in deletedFiles)
            {
                try
                {
                    File.Delete(Path.Combine(backuplocation, filename));
                    Console.WriteLine("File deleted from backup folder - {0}", filename);
                    Log.Information("File deleted from backup folder - {0}", filename);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to delete from backup folder - {0}", filename);
                    Console.WriteLine("Check logfile for more details");

                    Log.Error(e, "Failed to delete from backup folder - " + filename);
                }
            }

            Console.WriteLine("modified {0} files and deleted {1} files", modifiedFiles.Count(), deletedFiles.Count());
            Console.WriteLine("{0} - Synchronization Completed sucessfully \n", DateTime.Now.ToLongTimeString());
        }
    }

}
