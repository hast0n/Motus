using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Motus
{
    class Program
    {
        public static void Main()
        {
            // Reset console colors in case of any previous colorization
            Console.BackgroundColor = ConsoleColor.Black;
            // Instantiate GameLauncher
            var gameInterface = new GameLauncher();
            // Call method that launches the main menu
            gameInterface.LaunchMainMenu();
        }
    }
}