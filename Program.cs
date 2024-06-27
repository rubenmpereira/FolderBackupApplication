using FolderBackupApplication;
using Coravel;
using Microsoft.Extensions.Hosting;
using Serilog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScheduler();

var app = builder.Build();

string folderlocation = UserSettings.Default.FolderLocation;
string backuplocation = UserSettings.Default.BackupLocation;
string logfilelocation = UserSettings.Default.LogFile;
int syncInterval = UserSettings.Default.Interval;
int hoursPassed = 0;

BackupService service = new();

app.Services.UseScheduler(x =>
{
    x.ScheduleAsync(async () =>
    {
        hoursPassed++;
        if (hoursPassed % syncInterval == 0)
        {
            if (!string.IsNullOrEmpty(folderlocation) && !string.IsNullOrEmpty(backuplocation))
                await service.Backup(folderlocation, backuplocation, logfilelocation);
            else
            {
                Console.WriteLine("Backup sync failed");
                Console.WriteLine("Folder location or backup location is undefined \n");
            }
        }
    })
    .Hourly();
});

app.Start();

Console.WriteLine("\nBackup software started \n");

if (string.IsNullOrEmpty(folderlocation))
{
    Console.WriteLine("The folder to backup is undefined");
    Console.WriteLine("Type 'set folder [value]' to define the location \n");
}
else
{
    Console.WriteLine("Folder: {0}", folderlocation);
}

if (string.IsNullOrEmpty(backuplocation))
{
    Console.WriteLine("The backup folder is undefined");
    Console.WriteLine("Type 'set backup [value]' to define the location \n");
}
else
{
    Console.WriteLine("Backup folder: {0}", backuplocation);
}

if (string.IsNullOrEmpty(logfilelocation))
{
    Console.WriteLine("The log file is undefined");
    Console.WriteLine("Type 'set logfile [value]' to define the location \n");
}
else
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.File(logfilelocation, rollingInterval: RollingInterval.Infinite, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("Log file created");

    Console.WriteLine("Logs will be save on: {0}", logfilelocation);
}

Console.WriteLine("Will sync backup folder every {0} hours starting now", syncInterval);

Console.WriteLine("type 'help' to get a list of commands \n");

bool exit = false;

do
{
    string? input = Console.ReadLine();

    if (string.IsNullOrEmpty(input))
        continue;

    var command = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    switch (command[0].ToLower())
    {
        case "help":
            Console.WriteLine("Commands:\n");
            Console.WriteLine("run");
            Console.WriteLine("Will trigger the backup sync manually \n");

            Console.WriteLine("set folder [value]");
            Console.WriteLine("Define the location of the folder you wish to backup \n");

            Console.WriteLine("set backup [value]");
            Console.WriteLine("Define the location of the backup folder \n");

            Console.WriteLine("set interval [value]");
            Console.WriteLine("Define the interval in hours between sync \n");

            Console.WriteLine("set logfile [value]");
            Console.WriteLine("Define the location of the log file \n");

            Console.WriteLine("exit");
            Console.WriteLine("Will close the application \n");

            break;
        case "set":

            if (command.Length < 2)
            {
                Console.WriteLine("You must define the [variable] argument");
                Console.WriteLine("set [variable] [value]");
                Console.WriteLine("Variables: folder,backup,interval,logfile \n");
                break;
            }

            if (command.Length < 3)
            {
                Console.WriteLine("You must define the [value] argument");
                Console.WriteLine("set [variable] [value] \n");
                break;
            }

            string variable = command[1].ToLower();

            string value = command[2].ToLower();

            switch (variable)
            {
                case "folder":
                    if (!value.StartsWith(@"c:\"))
                        value = @"c:\" + value;
                    folderlocation = value.Replace('/', '\\');
                    Directory.CreateDirectory(folderlocation);
                    UserSettings.Default.FolderLocation = folderlocation;
                    UserSettings.Default.Save();
                    Console.WriteLine("Folder location changed to {0} sucessefully \n", value);
                    break;
                case "backup":
                    if (!value.StartsWith(@"c:\"))
                        value = @"c:\" + value;
                    backuplocation = value.Replace('/', '\\');
                    Directory.CreateDirectory(backuplocation);
                    UserSettings.Default.BackupLocation = backuplocation;
                    UserSettings.Default.Save();
                    Console.WriteLine("Backup location changed to {0} sucessefully \n", value);
                    break;
                case "interval":

                    if (int.TryParse(value, out int result) && result > 0)
                    {
                        hoursPassed = 0;
                        syncInterval = result;
                        UserSettings.Default.Interval = syncInterval;
                        UserSettings.Default.Save();
                        Console.WriteLine("Backup sync interval changed to {0} hours sucessfully \n", value);
                    }
                    else
                    {
                        Console.WriteLine("{0} is not a valid number \n", value);
                    }

                    break;
                case "logfile":
                    if (!value.StartsWith(@"c:\"))
                        value = @"c:\" + value;
                    logfilelocation = value.Replace('/', '\\');

                    if (!Path.HasExtension(logfilelocation))
                    {
                        logfilelocation = Path.Combine(logfilelocation, "logfile.txt");
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(logfilelocation));

                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.File(logfilelocation,
                            rollingInterval: RollingInterval.Infinite,
                            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
                        .CreateLogger();

                    Log.Information("Log file changed");

                    UserSettings.Default.LogFile = logfilelocation;
                    UserSettings.Default.Save();

                    Console.WriteLine("Log file location changed to {0} sucessefully \n", logfilelocation);
                    break;
                default:
                    Console.WriteLine("Could not recognize this variable: {0}", command[1].ToLower());
                    Console.WriteLine("Variables: folder,backup,interval,logfile \n");
                    break;
            }

            break;
        case "run":

            if (string.IsNullOrEmpty(folderlocation))
            {
                Console.WriteLine("Folder location is not defined");
                break;
            }

            if (string.IsNullOrEmpty(backuplocation))
            {
                Console.WriteLine("Backup folder location is not defined");
                break;
            }

            Console.WriteLine("Running backup sync manually \n");

            await service.Backup(folderlocation, backuplocation, logfilelocation);

            break;
        case "exit":
            exit = true;
            break;
        default:
            Console.WriteLine("Could not recognize this command: {0}", command[0].ToLower());
            Console.WriteLine("type 'help' to get a list of commands \n");
            break;
    }

} while (exit == false);