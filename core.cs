using System;
using System.IO;
using System.Linq;

namespace Motus
{
    class Core
    {
        public readonly string word;
        public readonly bool isTimed;
        public readonly int triesNb;
        public readonly int letterNb;
        public readonly int difficultyLevel;
        private string[] _history;
        public string[] History { get => _history; }
        private int _triesDone;
        public int triesDone { get => _triesDone; }
        public bool isWon {
            get => this.History.Last()
                        .Split("|")[0]
                        .Equals(this.word);
        }

        public Core(int letterNb, int triesNb, int difficultyLevel)
        {
            this.letterNb = letterNb;
            this.triesNb = triesNb;
            this.difficultyLevel = difficultyLevel;
            this.isTimed = (difficultyLevel > 1) ? true : false;

            this.word = SelectWord(letterNb);

            string tmp = string.Empty;
            
            this.CheckPosition(tmp, "0");
        }

        private string SelectWord(int letterNb)
        {
            string filepath = "./Resources/dico_{0}.txt";
            var file = new StreamReader(string.Format(filepath, letterNb)).ReadToEnd();
            string[] lines = file.Split(new char[] { '\n' });
            int count = lines.Length;
            int index = new Random().Next(0, count - 1);



            return lines[index];
        }

        public string CheckPosition(string input, string time)
        {
            /*
             * 0 : well positioned character
             * 1 : correct char but not mis positioned
             * 2 : nothing right here
             */

            string feedback = string.Empty;
            
            for (int i = 0; i < this.word.Length; i++)
            {
                feedback += (this.word[i].Equals(input[i])) ? 0 : this.word.Contains(input[i]) ? 1 : 2;
                
                /* -- OR --
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
                */
            }

            this._history.Append(string.Format("{0}|{1}|{2}", input, feedback, time));

            return feedback;
        }
    }
}
