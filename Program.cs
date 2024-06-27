using FolderBackupApplication;
using Serilog;

string folderlocation = UserSettings.Default.FolderLocation;
string backuplocation = UserSettings.Default.BackupLocation;
string logfilelocation = UserSettings.Default.LogFile;
int syncInterval = UserSettings.Default.Interval;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File(logfilelocation, rollingInterval: RollingInterval.Infinite, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("Log file created");