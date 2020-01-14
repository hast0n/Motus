using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        public string HorizontalBar;

        //private string _title;
        //public string Title
        //{
        //    get => _title;
        //    private set { _title = SetLine(value); }
        //}
        
        public void InitDefault()
        {
            #region InitVarRegion
            GameWidth = WindowWidth - GamePadding * 2 - 4;
            PaddingString = new string(' ', GamePadding);
            EmptyLine = InnerSetLine("");
            HorizontalBar = new string(HorizontalLineChar, GameWidth - 2);
            #endregion

            Console.SetWindowSize(WindowWidth, WindowHeight);
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

        public void RenderScreen(string screen)
        {
            
            
            int charIndex = 0;
            Dictionary<int, object> modifierDictionary = new Dictionary<int, object>();

            while (modifierDictionary.Keys.Count < 1)
            {
                modifierDictionary = SetFrameString(screen);
                
            }
            SetFrameString(screen, modifierDictionary);
        }

        private Dictionary<int, object> SetFrameString(string screen, Dictionary<int, object> modifierDictionary = null)
        {
            Console.Clear();

            StringBuilder output = new StringBuilder("\n");

            modifierDictionary ??= new Dictionary<int, object>();

            foreach (string lineIdentifier in ScreenResources[screen])
            {
                var rawStrings = Regex.Match(lineIdentifier, RegexScreenParamDelimiterPattern).Groups;
                string key = rawStrings.Count > 1 ? rawStrings[2].Value : lineIdentifier;
                string line = VisualResources[key];

                var paramStrings = Regex.Match(line, RegexInputDelimiterPattern).Groups;
                
                if (paramStrings.Count > 1)
                {
                    int modIndex = output.Length + paramStrings[0].Index;
                    modifierDictionary[modIndex] = paramStrings[2].Value;

                    line = Regex.Replace(line, RegexInputDelimiterPattern, "T");
                    
                    if (paramStrings[1].Value == "input")
                    {
                        
                    }
                    else if (paramStrings[1].Value == "color")
                    {
                        
                    }
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

            if (modifierDictionary.Keys.Count < 1)
            {
            }

            Console.WriteLine(output.ToString());
            return modifierDictionary;
        }
    }
}
