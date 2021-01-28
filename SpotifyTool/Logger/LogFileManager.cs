using System;
using System.IO;
using System.Threading.Tasks;

namespace SpotifyTool.Logger
{
    public class LogFileManager
    {
        public string LogFilePath { get; }

        private LogFileManager(string logFilePath)
        {
            this.LogFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        }

        public static async Task<LogFileManager> GetNewManager(string logFilePath)
        {
            LogFileManager manager = new LogFileManager(logFilePath);
            await File.WriteAllTextAsync(logFilePath, "");
            return manager;
        }

        public Task WriteToLog(string toWrite, bool newLine = true)
        {
            if (newLine)
            {
                toWrite = "\n" + toWrite;
            }
            return File.AppendAllTextAsync(this.LogFilePath, toWrite);
        }

        public Task WriteToLogAndConsole(string toWrite, bool newLine = true)
        {
            Task writeTask = WriteToLog(toWrite, newLine);
            if (newLine)
            {
                Console.WriteLine(toWrite);
            }
            else
            {
                Console.Write(toWrite);
            }
            return writeTask;
        }
    }
}
