using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public abstract class LoopMenu : IMenu
    {
        public IReadOnlyList<KeyValuePair<string, Func<Task>>> MenuPoints { get; }
        public bool ExitInNextLoop { get; private set; }

        public event Action OnExit;

        protected LoopMenu(IEnumerable<KeyValuePair<string, Func<Task>>> menuPoints, int? exitPosition = null)
        {
            List<KeyValuePair<string, Func<Task>>> newPointList = menuPoints?.ToList() ?? throw new ArgumentNullException(nameof(menuPoints));
            if (exitPosition.HasValue)
            {
                newPointList.Insert(exitPosition.Value, new KeyValuePair<string, Func<Task>>("Exit", this.Exit));
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
                    Console.WriteLine(i + ") " + this.MenuPoints[i].Key);
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
                    await this.MenuPoints[(int)optionInt].Value.Invoke();
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
