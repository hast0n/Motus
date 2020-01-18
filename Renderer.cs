using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Motus
{
    static class RendererHelper
    {
        public static string Encapsulate(this string line, int width, char symb)
        {
            string trimmedLine = line.Trim();
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

            bool colParity = trimmedLine.IsOddLength() != width.IsOdd();

            string paddingString = new string(' ', padding);

            string debug = string.Format("{2}{1}{0}{1}{3}{2}\n", 
                trimmedLine, paddingString, symb, (colParity) ? " " : "");
            return debug;
        }

        public static string Pad(this string line, int padding)
        {
            return $"{new string(' ', padding)}{line}";
        }

        public static bool IsOddLength(this string value)
        {
            return value.Length % 2 != 0;
        }
        
        public static bool IsOdd(this int value)
        {
            return value % 2 != 0;
        }
    }

    class Renderer
    {
        public IDictionary<string, string> VisualResources;
        public IDictionary<string, string[]> ScreenResources;
        public int WindowWidth;
        public int WindowHeight;
        public int GamePadding;
        public int GameWidth;
        public char HorizontalLineChar;
        public char VerticalLineChar;
        public char SplitChar;
        public string PaddingString;
        public string EmptyLine;
        public string RegexTextAttributeDelimiterPattern;
        public string RegexScreenParamDelimiterPattern;
        public string RegexInputDelimiterPattern;
        public string RegexInputParamDelimiterPattern;
        public string HorizontalBar;
        public string DefaultInputValue;

        public Dictionary<string, ConsoleColor> ConsoleColors;

        public void InitDefault()
        {
            #region InitVarRegion
            GameWidth = WindowWidth - GamePadding * 2 - 4;
            PaddingString = new string(' ', GamePadding);
            HorizontalBar = new string(HorizontalLineChar, GameWidth - 2);
            ConsoleColors = new Dictionary<string, ConsoleColor>()
            {
                {"red", ConsoleColor.Red},
                {"cyan", ConsoleColor.Cyan},
                {"blue", ConsoleColor.Blue},
                {"gray", ConsoleColor.Gray},
                {"green", ConsoleColor.Green},
                {"black", ConsoleColor.Black},
                {"white", ConsoleColor.White},
                {"yellow", ConsoleColor.Yellow},
                {"magenta", ConsoleColor.Magenta},
                {"darkred", ConsoleColor.DarkRed},
                {"darkblue", ConsoleColor.DarkBlue},
                {"darkcyan", ConsoleColor.DarkCyan},
                {"darkgray", ConsoleColor.DarkGray},
                {"darkgreen", ConsoleColor.DarkGreen},
                {"darkyellow", ConsoleColor.DarkYellow},
                {"darkmagenta", ConsoleColor. DarkMagenta},
            };
            EmptyLine ??= InnerSetLine("");
            DefaultInputValue ??= " ";
            #endregion

            Console.SetWindowSize(WindowWidth, WindowHeight);
        }

        public void SetColor(string color)
        {
            Console.BackgroundColor = ConsoleColors[color];
        }

        public string InnerSetLine(string line)
        {
            return line.Encapsulate(GameWidth, VerticalLineChar).Pad(GamePadding);
        }

        public string SetLine(string linesString)
        {
            try
            {
                string[] linesArray = linesString.Split(SplitChar)
                    .Where(l => !string.IsNullOrEmpty(l)).ToArray();

                if (linesArray.Length < 1)
                {
                    throw new StackOverflowException();
                }

                StringBuilder titleString = new StringBuilder();

                foreach (string line in linesArray.Where(l => !string.IsNullOrEmpty(l)))
                {
                    titleString.Append(InnerSetLine(line));
                }

                return titleString.ToString();
            }
            catch (StackOverflowException)
            {
                // no multiline delimiter
                return InnerSetLine(linesString);
            }
        }

        public Dictionary<int, string[]> RenderScreen(string screen)
        {
            #region Render Period Initialization
            Dictionary<int, string[]> modifierDictionary = new Dictionary<int, string[]>();
            string input = string.Empty;
            string[] strings = new [] {"\b", "\r"};

            /*int LastUnsetInput()
            {
                return modifierDictionary.OrderBy(x => x.Key)
                .LastOrDefault(x => string.IsNullOrEmpty(x.Value[0])).Key;
            }*/
            bool HasSetModifiers()
            {
                return modifierDictionary.Count(x => !x.Value[0].Equals(DefaultInputValue)) > 0;
            }
            int FirstUnsetInput()
            {
                return modifierDictionary.OrderBy(x => x.Key)
                .FirstOrDefault(x => x.Value[0].Equals(DefaultInputValue)).Key;
            }
            int LastSetInput()
            {
                return modifierDictionary.OrderBy(x => x.Key)
                    .LastOrDefault(x => !x.Value[0].Equals(DefaultInputValue)).Key;
            }
            void WipeLastInput()
            {
                try
                {
                    modifierDictionary[LastSetInput()][0] = DefaultInputValue;
                }
                catch (KeyNotFoundException) { /* DO NOTHING */ }
            }
            #endregion

            SetFrame(screen, ref modifierDictionary);

            while (true)
            {
                int modIndex = FirstUnsetInput();

                if (modIndex == 0)
                {
                    while (!strings.Contains(input))
                    {
                        input = $"{Console.ReadKey().KeyChar}";
                        Console.Write("\b");
                    }

                    if (input.Equals("\r"))
                    {
                        break;
                    }

                    WipeLastInput();
                    SetFrame(screen, ref modifierDictionary);
                    continue;
                }

                string currentRegexPattern = modifierDictionary[modIndex][1];
                input = $"{Console.ReadKey().KeyChar}";

                while (!Regex.IsMatch(input, currentRegexPattern))
                {
                    Console.Write("\b");

                    if (input.Equals("\b") && HasSetModifiers())
                    {
                        WipeLastInput();
                        break;
                    }

                    input = $"{Console.ReadKey().KeyChar}";
                }

                if (!input.Equals("\b"))
                {
                    modifierDictionary[modIndex][0] = input;
                }
                
                SetFrame(screen, ref modifierDictionary);
            }

            return modifierDictionary;
        }

        private void SetFrame(string screen, ref Dictionary<int, string[]> modifierDictionary)
        {
            Console.Clear();

            StringBuilder output = new StringBuilder();
            bool useModifiers = modifierDictionary.Count > 0;

            foreach (string lineIdentifier in ScreenResources[screen])
            {
                var rawStrings = Regex.Match(lineIdentifier, RegexScreenParamDelimiterPattern).Groups;
                string key = rawStrings.Count > 1 ? rawStrings[2].Value : lineIdentifier;
                string line = VisualResources[key];


                Regex r = new Regex(RegexInputDelimiterPattern);
                var match = r.Match(line);

                while (match.Success)
                {
                    var group = Regex.Match(match.Value, RegexInputParamDelimiterPattern).Groups;
                    int modIndex = output.Length + match.Index;
                    string replacement;
                        
                    if (!useModifiers)
                    {
                        modifierDictionary[modIndex] = new [] { DefaultInputValue, group[2].Value };
                    }
                    
                    if (useModifiers && group[1].Value == "input")
                    {
                        replacement = modifierDictionary[modIndex][0];
                    }
                    else if (useModifiers && group[1].Value == "color")
                    {
                        replacement = modifierDictionary[modIndex][0];
                    }
                    else
                    {
                        replacement = DefaultInputValue;
                    }

                    // seems to need instance of Regex to use occurence replacement quantifier...
                    line = r.Replace(line, replacement, 1);
                    // search again for ny new match (new input)
                    match = r.Match(line);
                }

                if (rawStrings.Count > 1)
                {
                    string repString = rawStrings[1].Value;
                    int repN = string.IsNullOrEmpty(repString)
                        ? 1
                        : int.Parse(repString);

                    for (int i = 0; i < repN; i++)
                    {
                        output.Append(SetLine(line));
                    }
                }
                else if (VisualResources.ContainsKey(lineIdentifier))
                {
                    output.Append(VisualResources[lineIdentifier]);
                }
            }

            if (useModifiers)
            {
                
            }
            else
            {
            }

            Console.WriteLine(output.ToString());
        }
    }
}
