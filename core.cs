using System;
using System.IO;
using System.Linq;
using System.Timers;

namespace Motus
{
    class Core
    {
        public readonly string word;
        public readonly bool isTimed;
        public readonly int triesNb;
        public readonly int letterNb;
        public readonly int difficultyLevel;

        public string[] History { get; private set; }

        public bool isWon => this.History.Where(c => c != null).ToArray()
                    .Last().Split("|")[0].Equals(this.word);

        public Core(int letterNb, int triesNb, int difficultyLevel)
        {
            this.letterNb = letterNb;
            this.triesNb = triesNb;
            this.difficultyLevel = difficultyLevel;

            this.word = this.SelectWord(letterNb).ToUpper();
            string tmp;

            switch (difficultyLevel)
            {
                case 3:
                    int index = new Random().Next(0, letterNb - 1);
                    char[] tmpCharArray = new char[letterNb];
                    tmpCharArray[index] = this.word[index];
                    tmp = tmpCharArray.ToString();
                    break;
                default:
                    tmp = string.Format("{0}{1}", this.word[0], new string(' ', letterNb - 1));
                    break;
            }
            
            
            this.History = new string[triesNb + 1];
            string historyInitializer = this.CheckPosition(tmp, DateTime.Now.Millisecond);
        }

        private string SelectWord(int letterNb)
        {
            string filepath = "Resources/dico_{0}.txt";
            var file = new StreamReader(string.Format(filepath, letterNb)).ReadToEnd();
            string[] lines = file.Split("\r\n");
            int count = lines.Length;
            int index = new Random().Next(0, count - 1);

            return lines[index];
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public string CheckPosition(string input, int time)
        {
            /*
             * 0 : well positioned character
             * 1 : correct char but not mis positioned
             * 2 : nothing right here
             */

            string feedback = string.Empty;
            //int time = Timer.;
            
            for (int i = 0; i < this.word.Length; i++)
            {
                //feedback += this.word[i].Equals(input[i]) ? 0 : 
                //            this.word.Contains(input[i]) ? 1 : 2;

                //--OR--
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

            int index = 0;

            for (int i = 0; i < this.History.Length; i++)
            {
                if (string.IsNullOrEmpty(this.History[i]))
                {
                    index = i;
                    break;
                }
            }

            this.History[index] = string.Format("{0}|{1}|{2}", input, feedback, time);
            return feedback;
        }
    }
}