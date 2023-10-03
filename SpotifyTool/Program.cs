using SpotifyTool.ConsoleMenu;
using SpotifyTool.Logger;
using System;

namespace SpotifyTool
{
    public class Program
    {

        public static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
            new MainMenu(LogFileManager.GetNewManager("logfile.txt").Result).UseMenu().Wait();
#if !DEBUG
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                Console.ReadLine();

                Main(args);
            }
#endif
        }
    }
}
