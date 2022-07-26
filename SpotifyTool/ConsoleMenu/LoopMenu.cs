using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public abstract class LoopMenu : IMenu
    {
        public IReadOnlyList<(string Name, Func<Task> Action)> MenuPoints { get; }
        public bool ExitInNextLoop { get; private set; }

        public event Action OnExit;

        protected LoopMenu(IEnumerable<(string Name, Func<Task> Action)> menuPoints, int? exitPosition = null)
        {
            List<(string name, Func<Task> action)> newPointList = menuPoints?.ToList() ?? throw new ArgumentNullException(nameof(menuPoints));
            if (exitPosition.HasValue)
            {
                newPointList.Insert(exitPosition.Value, ("Exit", this.Exit));
            }

            this.MenuPoints = newPointList;
            this.ExitInNextLoop = false;
        }

        protected Task Exit()
        {
            this.ExitInNextLoop = true;
            if (OnExit != null)
            {
                OnExit.Invoke();
            }
            return Task.CompletedTask;
        }

        public async Task UseMenu()
        {
            while (true)
            {
                Console.WriteLine("\n");
                Console.WriteLine("Please choose a menu point: ");
                for (int i = 0; i < this.MenuPoints.Count; i++)
                {
                    Console.WriteLine(i + ") " + this.MenuPoints[i].Name);
                }
                string option = Console.ReadLine();
                uint optionInt;
                if (!UInt32.TryParse(option, out optionInt))
                {
                    Console.WriteLine("Please write a valid number");
                    continue;
                }

                if (optionInt < this.MenuPoints.Count)
                {
                    await this.MenuPoints[(int)optionInt].Action.Invoke();
                }
                else
                {
                    Console.WriteLine("Number not recognized for menu");
                }

                Console.WriteLine("\n");
                if (this.ExitInNextLoop)
                {
                    this.ExitInNextLoop = false;
                    break;
                }
            }
        }
    }
}
