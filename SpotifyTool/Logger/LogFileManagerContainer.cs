using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.Logger
{
    public class LogFileManagerContainer(LogFileManager logFileManager)
    {
        public LogFileManager LogFileManager { get; } = logFileManager ?? throw new ArgumentNullException(nameof(logFileManager));
    }
}
