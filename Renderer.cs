using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.String;

namespace Motus
{
    static class RendererHelper
    {
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
        public bool CanInput;

        public Dictionary<string, ConsoleColor> ConsoleBackgroundColors;
        public Dictionary<string, ConsoleColor> ConsoleTextColors;

        public void InitDefault()
        {
            #region InitVarRegion
            GameWidth = WindowWidth - GamePadding * 2 - 4;
            PaddingString = new string(' ', GamePadding);
            HorizontalBar = new string(HorizontalLineChar, GameWidth - 2);
            ConsoleBackgroundColors = new Dictionary<string, ConsoleColor>()
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
            ConsoleTextColors = new Dictionary<string, ConsoleColor>()
            {
                {"red", ConsoleColor.White},
                {"cyan", ConsoleColor.Black},
                {"blue", ConsoleColor.White},
                {"gray", ConsoleColor.Black},
                {"green", ConsoleColor.Black},
                {"black", ConsoleColor.White},
                {"white", ConsoleColor.Black},
                {"yellow", ConsoleColor.Black},
                {"magenta", ConsoleColor.White},
                {"darkred", ConsoleColor.White},
                {"darkblue", ConsoleColor.White},
                {"darkcyan", ConsoleColor.White},
                {"darkgray", ConsoleColor.White},
                {"darkgreen", ConsoleColor.White},
                {"darkyellow", ConsoleColor.White},
                {"darkmagenta", ConsoleColor.White},
            };
            EmptyLine ??= InnerSetLine("");
            DefaultInputValue ??= " ";
            CanInput = true;
            #endregion

            Console.SetWindowSize(WindowWidth, WindowHeight);
        }

        public void SetColor(string color)
        {
            Console.BackgroundColor = ConsoleBackgroundColors[color];
            Console.ForegroundColor = ConsoleTextColors[color];
        }

        public string Encapsulate(string line)
        {
            Regex r = new Regex(RegexInputParamDelimiterPattern);
            string trimmedLine = line.Trim();
            string pseudoLine = trimmedLine;
            var match = r.Match(pseudoLine);
            
            while (match.Success)
            {
                string replacement = match.Groups[1].Value.Equals("input") ? " " : Empty;
                pseudoLine = r.Replace(pseudoLine, replacement, 1);
                match = r.Match(pseudoLine);
            }

            int lineLength = pseudoLine.Length;
            int width = GameWidth;
            char symb = VerticalLineChar;
            int padding;

            // should throw error if line is too long
            // the following behaviour is very foolish and not safe
            if (lineLength > width)
            {
                int excess = lineLength - width - 2; // -2 corresponds to the 2 border symbols
                trimmedLine = trimmedLine.Substring(excess / 2, width - 2);
                padding = 0;
            }
            else
            {
                padding = (width - lineLength) / 2 - 1;
            }

            bool colParity = lineLength.IsOdd() != width.IsOdd();

            string paddingString = new string(' ', padding);

            string debug = Format("{2}{1}{0}{1}{3}{2}\n",
                trimmedLine, paddingString, symb, (colParity) ? " " : "");
            int debug2 = debug.Length;
            return debug;
        }

        public string InnerSetLine(string line)
        {
            return Encapsulate(line).Pad(GamePadding);
        }

        public string SetLine(string linesString)
        {
            try
            {
                string[] linesArray = linesString.Split(SplitChar)
                    .Where(l => !IsNullOrEmpty(l)).ToArray();

                if (linesArray.Length < 1)
                {
                    throw new StackOverflowException();
                }

                StringBuilder titleString = new StringBuilder();

                foreach (string line in linesArray.Where(l => !IsNullOrEmpty(l)))
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

        public Dictionary<int, string[]> RenderScreen(List<string> screen)
        {
            #region Render Period Initialization
            Dictionary<int, string[]> modifierDictionary = new Dictionary<int, string[]>();

            bool HasSetModifiers()
            {
                return modifierDictionary.Where(x => x.Value[0] != "<color>")
                           .Count(x => !x.Value[0].Equals(DefaultInputValue)) > 0;
            }
            int FirstUnsetInput()
            {
                return modifierDictionary.Where(x => x.Value[0] != "<color>").OrderBy(x => x.Key)
                    .FirstOrDefault(x => x.Value[0].Equals(DefaultInputValue)).Key;
            }
            int LastSetInput()
            {
                return modifierDictionary.OrderBy(x => x.Key).Where(x => x.Value[0] != "<color>")
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

            StringBuilder screenString = ScreenReader(screen, ref modifierDictionary);
            FormatScreen(screenString, modifierDictionary);

            int modIndex = (FirstUnsetInput() == 0) ? LastSetInput() : FirstUnsetInput();

            if (modIndex == 0)
            {
                FormatScreen(screenString, modifierDictionary);
                var input = Console.ReadKey();
            }
            else
            {
                while (modifierDictionary.Count > 0 && CanInput)
                {
                    modIndex = (FirstUnsetInput() == 0) ? LastSetInput() : FirstUnsetInput() ;

                    string currentRegexPattern = modifierDictionary[modIndex][1];
                    string input = $"{Console.ReadKey().KeyChar}";

                    while (!Regex.IsMatch(input, currentRegexPattern) && CanInput)
                    {
                        Console.Write("\b");

                        if (input.Equals("\b") && HasSetModifiers())
                        {
                            WipeLastInput();
                            break;
                        }
                        
                        if (input.Equals("\r") && FirstUnsetInput() == 0)
                        {
                            foreach (var item in modifierDictionary.Where(kvp => kvp.Value[0].Equals("<color>")).ToList())
                            {
                                modifierDictionary.Remove(item.Key);
                            }
                            return modifierDictionary;
                        }
                        
                        input = $"{Console.ReadKey().KeyChar}";
                    }

                    if (!input.Equals("\b") && CanInput)
                    {
                        modifierDictionary[modIndex][0] = input;
                    }
                    
                    FormatScreen(screenString, modifierDictionary);
                }
            }

            return modifierDictionary;
        }

        private StringBuilder ScreenReader(List<string> screen, ref Dictionary<int, string[]> modifierDictionary)
        {
            StringBuilder output = new StringBuilder();

            foreach (string lineIdentifier in screen)
            {
                var rawStrings = Regex.Match(lineIdentifier, RegexScreenParamDelimiterPattern).Groups;
                string key = rawStrings.Count > 1 ? rawStrings[2].Value : lineIdentifier;
                string line;

                try { line = VisualResources[key]; }
                catch (KeyNotFoundException) { line = string.Empty; }

                if (rawStrings.Count > 1)
                {
                    string modLine = Empty;
                    string repString = rawStrings[1].Value;
                    int repN = IsNullOrEmpty(repString)
                        ? 1 : int.Parse(repString);
                    
                    for (int i = 0; i < repN; i++)
                    {
                        modLine += SetLine(line);
                    }

                    line = modLine;
                }
                else if (VisualResources.ContainsKey(lineIdentifier))
                {
                    output.Append(VisualResources[lineIdentifier]);
                    continue;
                }

                Regex r = new Regex(RegexInputDelimiterPattern);
                var match = r.Match(line);

                while (match.Success)
                {
                    var group = Regex.Match(match.Value, RegexInputParamDelimiterPattern).Groups;
                    int modIndex = output.Length + match.Index;
                    string replacement;

                    if (group[1].Value.Equals("input"))
                    {
                        replacement = DefaultInputValue;
                        modifierDictionary[modIndex] = new[] { replacement, group[2].Value };
                    }
                    else
                    {
                        replacement = Empty;
                        modifierDictionary[modIndex] = new[] { "<color>", group[2].Value };
                    }

                    // seems to need instance of Regex to use occurence replacement quantifier...
                    line = r.Replace(line, replacement, 1);
                    // search again for any new match (new input)
                    match = r.Match(line);
                }

                output.Append(line);

            }

            return output;
        }

        private void FormatScreen(StringBuilder screenBuilder, Dictionary<int, string[]> modifierDictionary)
        {

            Console.Clear();
            int renderedPartIndex = 0;

            foreach (var kvp in modifierDictionary.OrderBy(x => x.Key))
            {
                if (kvp.Value[0].Equals("<color>"))
                {
                    // color context
                    string leftString = screenBuilder.ToString().Substring(renderedPartIndex, kvp.Key - renderedPartIndex);
                    Console.Write(leftString);
                    SetColor(kvp.Value[1]);
                    renderedPartIndex = kvp.Key;
                }
                else
                {
                    // input context
                    screenBuilder[kvp.Key] = char.Parse(kvp.Value[0]);
                }
            }
            Console.WriteLine(screenBuilder.ToString().Substring(renderedPartIndex, screenBuilder.Length - renderedPartIndex));
        }
    }
}
