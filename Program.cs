using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Motus
{
    class Program
    {
        public static void Main()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            var gameInterface = new GameLauncher();
            gameInterface.Start();
        }
    }
}