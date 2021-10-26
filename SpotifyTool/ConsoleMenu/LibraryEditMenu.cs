using SpotifyTool.Logger;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class LibraryEditMenu : LoopMenu
    {
        private LibraryEditMenu(LibraryEditMenuActions libraryEditMenuActions) : base(new List<KeyValuePair<string, Func<Task>>>() {
                    new KeyValuePair<string, Func<Task>>("Like", libraryEditMenuActions.Like),
                    new KeyValuePair<string, Func<Task>>("Unlike", libraryEditMenuActions.Unlike),
                    new KeyValuePair<string, Func<Task>>("Search in library", libraryEditMenuActions.SearchLibrary)
                }, 0)
        {
        }

        public LibraryEditMenu(LogFileManager logFileManager) : this(new LibraryEditMenuActions(logFileManager))
        {
        }
    }
}
