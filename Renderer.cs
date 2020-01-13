using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Motus
{
    static class RendererHelper
    {
        public static string Encapsulate(this string line, int width, char symb)
        {
            string trimmedLine = line.Trim();
            int lineLength = trimmedLine.Length;
            int padding;

            if (trimmedLine.Length > width)
            {
                int excess = trimmedLine.Length - width - 2; // -2 corresponds to the 2 border symbols
                trimmedLine = trimmedLine.Substring(excess / 2, width - 2);
                padding = 0;
            }
            else
            {
                padding = (width - trimmedLine.Length) / 2 - 1;
            }

            bool colParity = trimmedLine.isOddLength() != width.isOdd();

            string paddingString = new string(' ', padding);

            string debug = string.Format("{2}{1}{0}{1}{3}{2}\n", 
                trimmedLine, paddingString, symb, (colParity) ? " " : "");
            return debug;
        }

        public static string Pad(this string line, int padding)
        {
            
            return $"{new string(' ', padding)}{line}";
        }

        public static bool isOddLength(this string value)
        {
            return value.Length % 2 != 0;
        }
        
        public static bool isOdd(this int value)
        {
            return value % 2 != 0;
        }
    }

    class Renderer
    {
        private IDictionary<string, string> visualResources;

        public readonly int WindowWidth;
        public readonly int WindowHeight;
        
        public readonly int GamePadding;
        public readonly int GameWidth;

        public readonly char HorizontalLineChar;
        public readonly char VerticalLineChar;
        private readonly char _splitChar;
        private readonly string _paddingString;
        private readonly string _emptyLine;

        private string _title;
        public string Title
        {
            get => _title;
            private set
            {
                string[] lines = value.Split(_splitChar);
                StringBuilder titleString = new StringBuilder();

                foreach (string line in lines.Where(l => !string.IsNullOrEmpty(l)))
                {
                    titleString.Append(SetLine(line));
                }

                _title = titleString.ToString();
            }
        }

        public Renderer()
        {
            #region InitVarRegion
            WindowWidth = 120;
            WindowHeight = 40;
            GamePadding = 5;

            GameWidth = WindowWidth - GamePadding * 2 - 4;

            HorizontalLineChar = '─';
            VerticalLineChar = '│';
            _splitChar = '\n';
            _paddingString = new string(' ', GamePadding);
            _emptyLine = SetLine("");
            #endregion

            _title = string.Empty;
            string horizontalBar = new string(HorizontalLineChar, GameWidth - 2);

            visualResources = new Dictionary<string, string>
            {
                {"topBar", $"{_paddingString}┌{horizontalBar}┐\n"},
                {"bottomBar", $"{_paddingString}└{horizontalBar}┘\n"},
                {"introString", "MO COMME MOTUS, MOTUS !"},
                {
                    // characters used : │ ─ ├ ┼ ┤ ┌ ┬ ┐ └ ┴ ┘
                    "motusArt",
                    string.Join(_splitChar, new []
                    {
                        " ┌─┐    ┌─┐   ┌────┐   ┌────────┐ ┌─┐   ┌─┐   ┌───────┐ ",
                        " │ └─┐┌─┘ │ ┌─┘┌──┐└─┐ └──┐  ┌──┘ │ │   │ │  ┌┘┌──────┘ ",
                        " │ ┌┐└┘┌┐ │ │  │  │  │    │  │    │ │   │ │  └┐└─────┐  ",
                        " │ │└──┘│ │ │  │  │  │    │  │    │ │   │ │   └─────┐└┐ ",
                        " │ │    │ │ └─┐└──┘┌─┘    │  │    └┐└───┘┌┘  ┌──────┘┌┘ ",
                        " └─┘    └─┘   └────┘      └──┘     └─────┘   └───────┘  "
                    })
                },
            };

            Title = visualResources["motusArt"];
            Console.SetWindowSize(WindowWidth, WindowHeight);
        }

        private string SetLine(string line)
        {
            return line.Encapsulate(GameWidth, VerticalLineChar).Pad(GamePadding);
        }
        
        private string SetLine(string[] lines)
        {
            StringBuilder formattedLines = new StringBuilder();

            foreach (string line in lines)
            {
                formattedLines.Append(SetLine(line));
            }

            return formattedLines.ToString();
        }

        public void Render()
        {
            Console.Clear();
            StringBuilder output = new StringBuilder("\n");

            string[] renderLines = new[]
            {
                #region Header
                visualResources["topBar"],
                Title,
                visualResources["bottomBar"],
                #endregion

                #region Body
                visualResources["topBar"],
                _emptyLine, _emptyLine, _emptyLine,



                _emptyLine, _emptyLine, _emptyLine,
                visualResources["bottomBar"],
                #endregion
            };

            foreach (string line in renderLines)
            {
                output.Append(line);
            }

            Console.WriteLine(output.ToString());
        }
    }
}
