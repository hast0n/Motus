﻿using System;
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

        public static string[,] Statistics()
        {
            string datapath = "../../../Resources/data.txt";

            string[,] dataStat = new string[2, 3];

            //int nbTry = History.Count(s => s != null);

            double avgTry = 0;
            double avgTimeTry = 0;
            double avgTimeTot = 0;
            int cpt = 0;
            double infAvgTry = 0;
            double infAvgTimeTry = 0;
            double infAvgTimeTot = 0;
            double supAvgTry = 0;
            double supAvgTimeTry = 0;
            double supAvgTimeTot = 0;

            double infAvgTryPerso = 0;
            double infAvgTimeTryPerso = 0;
            double infAvgTimeTotPerso = 0;
            double idemAvgTryPerso = 0;
            double idemAvgTimeTryPerso = 0;
            double idemAvgTimeTotPerso = 0;
            double supAvgTryPerso = 0;
            double supAvgTimeTryPerso = 0;
            double supAvgTimeTotPerso = 0;
            int cptPerso = 1;

            try
            {
                string[] lines = File.ReadAllLines(datapath);

                // Statistics for the last chosen level
                for (int i = 3; i < lines.Length; i++)
                {
                    int t = int.Parse(lines[i].Split(",")[0]);
                    if (t == int.Parse(lines.Last().Split(",")[0]))
                    {
                        avgTimeTot += int.Parse(lines[i].Split(",")[1]);
                        avgTimeTry += int.Parse(lines[i].Split(",")[2]);
                        avgTry += int.Parse(lines[i].Split(",")[3]);
                        cpt += 1;
                    }
                }
                avgTimeTot /= cpt;
                avgTimeTry /= cpt;
                avgTry /= cpt;

                for (int i = 3; i < lines.Length; i++)
                {
                    int t = int.Parse(lines[i].Split(",")[0]);
                    if (t == int.Parse(lines.Last().Split(",")[0]))
                    {
                        if (int.Parse(lines[i].Split(",")[3]) < avgTry)
                        {
                            infAvgTry += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[1]) < avgTimeTot)
                        {
                            infAvgTimeTot += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[2]) < avgTimeTry)
                        {
                            infAvgTimeTry += 1;
                        }
                    }
                }

                supAvgTry = cpt - infAvgTry;
                supAvgTimeTry = cpt - infAvgTimeTry;
                supAvgTimeTot = cpt - infAvgTimeTot;

                infAvgTry = (infAvgTry/cpt) * 100;
                supAvgTry = (supAvgTry / cpt) * 100;
                infAvgTimeTry =(infAvgTimeTry /cpt) * 100;
                supAvgTimeTry = (supAvgTimeTry / cpt) * 100;
                infAvgTimeTot = (infAvgTimeTot / cpt) * 100;
                supAvgTimeTot = (supAvgTimeTot / cpt) * 100;



                for (int i = 3; i < lines.Length - 1; i++)//doesn't count the last game which is the game studied
                {
                    int t = int.Parse(lines[i].Split(",")[0]);
                    if (t == int.Parse(lines.Last().Split(",")[0]))
                    {
                        if (int.Parse(lines[i].Split(",")[3]) < avgTry)
                        {
                            infAvgTryPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[3]) == avgTry)
                        {
                            idemAvgTryPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[1]) < avgTimeTot)
                        {
                            infAvgTimeTotPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[1]) == avgTimeTot)
                        {
                            idemAvgTimeTotPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[2]) < avgTimeTry)
                        {
                            infAvgTimeTryPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[2]) == avgTimeTry)
                        {
                            idemAvgTimeTryPerso += 1;
                        }
                        cptPerso += 1;
                    }
                }
                supAvgTryPerso = cptPerso - infAvgTryPerso - idemAvgTryPerso;
                supAvgTimeTryPerso = cptPerso - infAvgTimeTryPerso - idemAvgTimeTryPerso;
                supAvgTimeTotPerso = cptPerso - infAvgTimeTotPerso - idemAvgTimeTotPerso;

                infAvgTryPerso = (infAvgTryPerso / cptPerso) * 100;
                idemAvgTryPerso = (idemAvgTryPerso / cptPerso) * 100;
                supAvgTryPerso = (supAvgTryPerso / cptPerso) * 100;
                infAvgTimeTryPerso = (infAvgTimeTryPerso / cptPerso) * 100;
                idemAvgTimeTryPerso = (idemAvgTimeTryPerso / cptPerso) * 100;
                supAvgTimeTryPerso = (supAvgTimeTryPerso / cptPerso) * 100;
                infAvgTimeTotPerso = (infAvgTimeTotPerso / cptPerso) * 100;
                idemAvgTimeTotPerso = (idemAvgTimeTotPerso / cptPerso) * 100;
                supAvgTimeTotPerso = (supAvgTimeTotPerso / cptPerso) * 100;

                dataStat[0, 0] = String.Format("{0}|{1}|{2}", Math.Round(infAvgTry, 1).ToString("0.0")=="NaN"?"0.0": Math.Round(infAvgTry, 1).ToString("0.0"),Math.Round(supAvgTry, 1).ToString("0.0")=="NaN"?"0.0":Math.Round(supAvgTry, 1).ToString("0.0"), avgTry.ToString() == "NaN" ? "0" : avgTry.ToString()); ;
                dataStat[0, 1] = String.Format("{0}|{1}|{2} s", Math.Round(infAvgTimeTry, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(infAvgTimeTry, 1).ToString("0.0"), Math.Round(supAvgTimeTry, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(supAvgTimeTry, 1).ToString("0.0"), Math.Round(avgTimeTry, 1).ToString("0.0") == "NaN" ? "00.0" : Math.Round(avgTimeTry/1000, 1).ToString("0.0"));
                dataStat[0, 2] = String.Format("{0}|{1}|{2} s", Math.Round(infAvgTimeTot, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(infAvgTimeTot, 1).ToString("0.0"), Math.Round(infAvgTimeTot, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(supAvgTimeTot, 1).ToString("0.0"), Math.Round(avgTimeTot, 1).ToString("0.0") == "NaN" ? "00.0" : Math.Round(avgTimeTot/1000, 1).ToString("0.0"));

                dataStat[1, 0] = String.Format("{0}|{1}|{2}", Math.Round(infAvgTryPerso, 1).ToString("0.0"), Math.Round(idemAvgTryPerso, 1).ToString("0.0"), Math.Round(supAvgTryPerso, 1).ToString("0.0"));
                dataStat[1, 1] = String.Format("{0}|{1}|{2}", Math.Round(infAvgTimeTryPerso, 1).ToString("0.0"), Math.Round(idemAvgTimeTryPerso, 1).ToString("0.0"), Math.Round(supAvgTimeTryPerso, 1).ToString("0.0"));
                dataStat[1, 2] = String.Format("{0}|{1}|{2}", Math.Round(infAvgTimeTotPerso, 1).ToString("0.0"), Math.Round(idemAvgTimeTotPerso, 1).ToString("0.0"), Math.Round(supAvgTimeTotPerso, 1).ToString("0.0"));
            }
            catch (Exception ex)
            {
                Console.Write("Une erreur est survenue au cours de l'opération :");
                Console.WriteLine(ex.Message);
            }
            return dataStat;
        }
    }
}