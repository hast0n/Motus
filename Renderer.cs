using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.String;

namespace Motus
{

    // A scimple class used to help the Renderer to frame content
    static class RendererHelper
    {
        public static string Pad(this string line, int padding)
        {
            return $"{new string(' ', padding)}{line}";
        }
        
        public static bool IsOdd(this int value)
        {
            return value % 2 != 0;
        }
    }

    class Renderer
    {
        // visual elements to be displayed on screen
        public IDictionary<string, string> VisualResources;
        // console width
        public int WindowWidth;
        // console height
        public int WindowHeight;
        // game left padding
        public int GamePadding;
        // game frame width
        public int GameWidth;
        // Default char for horizontal lines
        public char HorizontalLineChar;
        // Default char for vertical lines
        public char VerticalLineChar;
        // Default char to use to split strings for carriage return
        public char SplitChar;
        // Default white space line for left padding
        public string PaddingString;
        // Default empty line
        public string EmptyLine;
        // Regular Expression to detect if there are modifiers in a visual resource
        public string RegexTextAttributeDelimiterPattern;
        // RegEx to detect if a visual resource needs to be framed or repeated
        public string RegexScreenParamDelimiterPattern;
        // RegEx to get modifier index in visual resource
        public string RegexInputDelimiterPattern;
        // RegEx to extract modifier values in visual resource
        public string RegexInputParamDelimiterPattern;
        // Default Horizontal bar
        public string HorizontalBar;
        // Default placeholder for input
        public string DefaultInputValue;
        // Boolean that asserts if inputs are being allowed or not
        public bool CanInput;

        // Console background colors
        public Dictionary<string, ConsoleColor> ConsoleBackgroundColors;
        // Console forground colors
        public Dictionary<string, ConsoleColor> ConsoleTextColors;

        public void InitDefault()
        {
            // Set default values according to instance parameters
            GameWidth = WindowWidth - GamePadding * 2 - 4;
            PaddingString = new string(' ', GamePadding);
            HorizontalBar = new string(HorizontalLineChar, GameWidth - 2);
            // Define basic console colors dictionary to easily access them
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

            // Define foreground color according to background color for better readability
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

            Console.SetWindowSize(WindowWidth, WindowHeight);
        }

        public void SetColor(string color)
        {
            Console.BackgroundColor = ConsoleBackgroundColors[color];
            Console.ForegroundColor = ConsoleTextColors[color];
        }

        public string Encapsulate(string line)
        {
            // Frame a line to match the game outside border

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

            // TODO: throw error if line is too long
            // the following behaviour is very foolish
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
            // Frame one line
            return Encapsulate(line).Pad(GamePadding);
        }

        public string SetLine(string linesString)
        {
            // Frame multiple lines from one visual resource

            try
            {
                // extract lines with multiline delimiters
                string[] linesArray = linesString.Split(SplitChar)
                    .Where(l => !IsNullOrEmpty(l)).ToArray();

                if (linesArray.Length < 1)
                {
                    throw new StackOverflowException();
                }

                StringBuilder stringBuilder = new StringBuilder();
                
                // Append each framed line to StringBuilder and return it
                foreach (string line in linesArray.Where(l => !IsNullOrEmpty(l)))
                {
                    stringBuilder.Append(InnerSetLine(line));
                }

                return stringBuilder.ToString();
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

            // Set useful local methods
            bool HasSetModifiers()
            {
                // Return a bool indicating if user has already input data
                return modifierDictionary.Where(x => x.Value[0] != "<color>")
                           .Count(x => !x.Value[0].Equals(DefaultInputValue)) > 0;
            }
            int FirstUnsetInput()
            {
                // Get index of first unset input modifier
                return modifierDictionary.Where(x => x.Value[0] != "<color>").OrderBy(x => x.Key)
                    .FirstOrDefault(x => x.Value[0].Equals(DefaultInputValue)).Key;
            }
            int LastSetInput()
            {
                // Get index of Last set input modifier
                return modifierDictionary.OrderBy(x => x.Key).Where(x => x.Value[0] != "<color>")
                    .LastOrDefault(x => !x.Value[0].Equals(DefaultInputValue)).Key;
            }
            void WipeLastInput()
            {
                // Remove last user input
                try
                {
                    modifierDictionary[LastSetInput()][0] = DefaultInputValue;
                }
                catch (KeyNotFoundException) { /* DO NOTHING */ }
            }
            #endregion

            StringBuilder screenString = ScreenReader(screen, ref modifierDictionary);
            FormatScreen(screenString, modifierDictionary);

            // Get the index of current modifier to set
            int modIndex = (FirstUnsetInput() == 0) ? LastSetInput() : FirstUnsetInput();

            if (modIndex == 0)
            {
                // no input modifiers in screen
                FormatScreen(screenString, modifierDictionary);
                var input = Console.ReadKey();
            }
            else
            {
                // Iterate over modifiers and set them

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
            // Read screen to display and extract modifier indexes

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
            // Takes care of displaying colors and inputs values
            
            Console.Clear();
            // Set the index to which the frame has been rendered
            int renderedPartIndex = 0;

            foreach (var kvp in modifierDictionary.OrderBy(x => x.Key))
            {
                if (kvp.Value[0].Equals("<color>"))
                {
                    // Entering color context

                    // Extract string before color modifier
                    string leftString = screenBuilder.ToString().Substring(renderedPartIndex, kvp.Key - renderedPartIndex);
                    // Write it
                    Console.Write(leftString);
                    // Change console color
                    SetColor(kvp.Value[1]);
                    // Set rendering index
                    renderedPartIndex = kvp.Key;
                }
                else
                {
                    // Entering input context

                    // Put user input in line to display
                    screenBuilder[kvp.Key] = char.Parse(kvp.Value[0]);
                }
            }

            // Display remaining characters that contains no modifiers
            Console.WriteLine(screenBuilder.ToString().Substring(renderedPartIndex, screenBuilder.Length - renderedPartIndex));
        }
    }
}
