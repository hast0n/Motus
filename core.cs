using System;
using System.IO;
using System.Linq;

namespace Motus
{
    class core
    {
        private readonly int letterNb;
        private readonly int triesNb;
        private readonly string word;
        private readonly bool isTimed;
        private string[] _history;
        private int difficultyLevel;
        
        public string[] History
        {
            get => _history;
        }

        public core(int letterNb, int triesNb, int difficultyLevel)
        {
            this.letterNb = letterNb;
            this.triesNb = triesNb;
            this.isTimed = isTimed;
            this.difficultyLevel = difficultyLevel;

            this.word = SelectWord(letterNb);

        }

        private string SelectWord(int letterNb)
        {
            var file = new StreamReader(string.Format("./Resources/dico_{0}.txt", letterNb)).ReadToEnd(); // big string
            string[] lines = file.Split(new char[] { '\n' });           // big array
            int count = lines.Length;
            int index = new Random().Next(0, count - 1);

            return lines[index];
        }

        public string CheckPosition(string input, string time)
        {
            /*
             * 0 : well positioned character
             * 1 : correct char but not miss positioned
             * 2 : nope
             */
            string feedback = string.Empty;
            
            for (int i = 0; i < this.word.Length; i++)
            {
                if (this.word[i].Equals(input[i]))
                {
                    feedback += 0;
                } 
                else if (this.word.Contains(input[i]))
                {
                    feedback += 1;
                }
                else
                {
                    feedback += 2;
                }
            }

            this._history.Append(string.Format("{0}|{1}|{2}", input, feedback, time));

            return feedback;
        }
    }
}
