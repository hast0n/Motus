using System;
using System.IO;
using System.Linq;

namespace Motus
{
    class GameCore
    {
        public readonly string Word;
        public readonly int TriesNb;
        public readonly int LetterNb;
        public string[] Dictionary { private set; get; }
        public string[] History { get; }

        // Using Linq Enumerable sorting methods (.Where & .Last) to
        // find, extract and compare first section of last non null 
        // string element in a string array
        public bool IsWon => this.History.Where(c => c != null).ToArray()
                    .Last().Split("|")[0].Equals(this.Word);

        public GameCore(int letterNb, int triesNb, bool randomizeFirstLetter)
        {
            // parameters initialization
            this.LetterNb = letterNb;
            this.TriesNb = triesNb;

            // Intilialize word
            this.Word = this.SelectWord(letterNb).ToUpper();
            string tmp;

            // randomize first letter if difficulty level > 1
            if (randomizeFirstLetter)
            {
                int index = new Random().Next(0, letterNb - 1);
                char[] tmpCharArray = new char[letterNb];
                tmpCharArray[index] = this.Word[index];
                tmp = new string(tmpCharArray);
            }
            else 
            {
                tmp = $"{this.Word[0]}{new string(' ', letterNb - 1)}";
            }
            
            this.History = new string[triesNb + 1];

            // Initialize game history
            this.CheckPosition(tmp, 0);
        }

        private string SelectWord(int letterNb)
        {
            // Fetch dictionnary data
            string filepath = "../../../Resources/dico_{0}.txt";
            var file = new StreamReader(string.Format(filepath, letterNb)).ReadToEnd();
            Dictionary = file.Split("\r\n");
            // Draw random word
            int index = new Random().Next(0, Dictionary.Length - 1);

            return Dictionary[index];
        }

        public string CheckPosition(string input, int time)
        {
            /*
             * 0 : well positioned character
             * 1 : correct char but not mis positioned
             * 2 : nothing right here
             */

            string feedback = string.Empty;
            
            // Check letter validity foreach letter in user input
            for (int i = 0; i < this.Word.Length; i++)
            {
                feedback += this.Word[i].Equals(input[i]) ? 0 : 
                            this.Word.Contains(input[i]) ? 1 : 2;

            }

            // Get index of last non null history element
            int index = History.Where(s => s != null).ToArray().Length;

            #region -- OR --

            //int index = 0;

            //for (int i = 0; i < this.History.Length; i++)
            //{
            //    if (History[i] != null)
            //    {
            //        index = i;
            //        break;
            //    }
            //}

            #endregion

            // Setup history element
            History[index] = $"{input}|{feedback}|{time}";

            return feedback;
        }
    }
}