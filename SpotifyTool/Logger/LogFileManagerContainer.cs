using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.Logger
{
    public class LogFileManagerContainer
    {
        public LogFileManager LogFileManager { get; }

        public LogFileManagerContainer(LogFileManager logFileManager)
        {
            this.LogFileManager = logFileManager ?? throw new ArgumentNullException(nameof(logFileManager));
        }
    }
}
