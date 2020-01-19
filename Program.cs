using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Motus
{
    class Program
    {
        public static void Main()
        {
            Console.ResetColor();
            var gameInterface = new GameLauncher();
            gameInterface.Start();
        }
    }
}