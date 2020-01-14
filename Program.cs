using System;
using System.Text.RegularExpressions;

namespace Motus
{
    class Program
    {
        public static void Main()
        {
            var gameInterface = new GameLauncher();
            gameInterface.Start();
        }
    }
}