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
        public readonly int DifficultyLevel;
        public string[] Dictionary { private set; get; }
        public string[] History { get; }

        // Using Linq Enumerable sorting methods (.Where & .Last) to
        // find, extract and compare first section of last non null 
        // string element in a string array
        public bool IsWon => this.History.Where(c => c != null).ToArray()
                    .Last().Split("|")[0].Equals(this.Word);

        public GameCore(int letterNb, int triesNb, int difficultyLevel)
        {
            this.LetterNb = letterNb;
            this.TriesNb = triesNb;
            this.DifficultyLevel = difficultyLevel;

            this.Word = this.SelectWord(letterNb).ToUpper();
            string tmp;

            if (new [] {1, 2}.Contains(difficultyLevel))
            {
                // string interpolation : $"{exp1}{exp2}"
                // instead of string formatting : string.format("{0}{1}", exp1, exp2)
                tmp = $"{this.Word[0]}{new string(' ', letterNb - 1)}";
            }
            else 
            {
                int index = new Random().Next(0, letterNb - 1);
                char[] tmpCharArray = new char[letterNb];
                tmpCharArray[index] = this.Word[index];
                tmp = new string(tmpCharArray);
            }
            
            this.History = new string[triesNb + 1];
            this.CheckPosition(tmp, DateTime.Now.Millisecond);
        }

        private string SelectWord(int letterNb)
        {
            string filepath = "../../../Resources/dico_{0}.txt";
            var file = new StreamReader(string.Format(filepath, letterNb)).ReadToEnd();
            Dictionary = file.Split("\r\n");
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
            //int time = Timer.;
            
            for (int i = 0; i < this.Word.Length; i++)
            {
                // concatenate a string with an expression evaluated with
                // the conditional operator ?: (ternary conditional operator)
                feedback += this.Word[i].Equals(input[i]) ? 0 : 
                            this.Word.Contains(input[i]) ? 1 : 2;

                #region -- OR --

                //if (this.word[i].Equals(input[i]))
                //{
                //    feedback += 0;
                //}
                //else if (this.word.Contains(input[i]))
                //{
                //    feedback += 1;
                //}
                //else
                //{
                //    feedback += 2;
                //}

                #endregion
            }

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

            History[index] = $"{input}|{feedback}|{time}";

            return feedback;
        }

        public void SaveData()
        {
            string datapath = "../../../Resources/data.txt";
            if (File.Exists(datapath) == false)//if data.txt does not exist, create data.txt
            {
                try
                {
                    TextWriter newfile = new StreamWriter(datapath, true);
                    newfile.WriteLine("Enregistrement(s) de vos statistiques de jeu");
                    newfile.WriteLine("Niveau, Temps total de résolution, Temps moyen par mot, Nombre de tentative(s)");
                    newfile.WriteLine();
                    newfile.Close();
                }
                catch (Exception ex)
                {
                    Console.Write("Une erreur est survenue au cours de l'opération de création du fichier data.txt :");
                    Console.WriteLine(ex.Message);
                }
            }



            if (IsWon) //if the game is won, the game data are saved : Difficulty level, overall time, average time by word, number of try
            {
                //Average time by word on this game
                int avgTime = 0;
                int overallTime = 0;
                int i = 1;
                bool noEnd = true;
                while (i <History.Length & noEnd)
                {
                    while (History[i] != null)
                    {
                        avgTime += (int.Parse(History[i].Split("|")[2]) - int.Parse(History[i - 1].Split("|")[2]));
                        overallTime += int.Parse(History[i].Split("|")[2]);
                        i++;
                    }

                    if (History[i] == null)
                    {
                        noEnd = false;
                    }
                }
                avgTime /= (i - 1);
                string entry = String.Format("{0},{1},{2},{3}", DifficultyLevel.ToString(), overallTime.ToString(), avgTime.ToString(), (History.Length - 1).ToString());

                try
                {
                    using StreamWriter writtingOn = File.AppendText(datapath);
                    writtingOn.WriteLine(entry);
                }
                catch (Exception ex)
                {
                    Console.Write("Une erreur est survenue au cours de l'opération de sauvegarde :");
                    Console.WriteLine(ex.Message);
                }
            }
            //Console.ReadLine();
        }
    }
}