using SpotifyTool.Logger;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class LibraryEditMenu : LoopMenu
    {
        private LibraryEditMenu(LibraryEditMenuActions libraryEditMenuActions) : base(new List<(string Name, Func<Task> Action)>() {
                    ("Like", libraryEditMenuActions.Like),
                    ("Unlike", libraryEditMenuActions.Unlike),
                    ("Search in library", LibraryEditMenuActions.SearchLibrary)
                }, 0)
        {
        }

        public LibraryEditMenu(LogFileManager logFileManager) : this(new LibraryEditMenuActions(logFileManager))
        {
        }
    }
}
