using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
//using System.Text;
using System.Timers;

namespace Motus
{
    class CLI
    {
        private Core game;
        private double lastGuessTime;
        private int nbTried => this.game.History.Where(c => c != null).ToArray().Length - 1;
        private Stopwatch watch;
        private static Timer Timer;

        public CLI()
        {
            SetGameParameters();
            watch = Stopwatch.StartNew();
            SetTimer();
        }

        private void SetTimer()
        {
            int timeLap;

            switch (this.game.difficultyLevel)
            {
                case 2:
                    timeLap = 60000;
                    break;
                case 3:
                    timeLap = 45000;
                    break;
                case 4:
                    timeLap = 30000;
                    break;
                case 5:
                    timeLap = 15000;
                    break;
                default:
                    timeLap = 0;
                    break;
            }

            Timer = new Timer(timeLap);
            Timer.Elapsed += OnTimeElapsed;
            Timer.AutoReset = false;
            Timer.Enabled = this.game.isTimed;
        }

        private void OnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            EndGame();
        }

        private void SetGameParameters()
        {
            #region Game Paramter Initialization

            //Console.Write("Sélectionnez le nombre de lettres du mots :\n? ");

            int letterNb = 0;
            int triesNb = 0;
            int difficultyLevel = 0;

            //while (letterNb < 6 || letterNb > 10)
            //{
            //    letterNb = CheckIntInput("Mauvaise entrée ! Réessayez :\n? ");
            //}

            //Console.Write("Sélectionnez le nombre d'essais :\n? ");


            //while (triesNb > letterNb || triesNb < 0)
            //{
            //    triesNb = CheckIntInput("Mauvaise entrée ! Sélectionner un nombre inférieur au nombre de lettres dans le mot :\n? ");
            //}

            Console.Write("Sélectionnez le niveau de difficulté :" +
                          "\n\t1. Mot de 4 lettres, première lettre donnée, pas de limite de temps, 6 essais." +
                          "\n\t2. Mot de 5 lettres, première lettre donnée, limite de temps (1 minute), 10 essais." +
                          "\n\t4. Mot de 6 lettres, une lettre donnée aléatoirement, limite de temps (30 secondes), 8 essais." +
                          "\n\t5. Mot de 7 lettres, une lettre donnée aléatoirement, limite de temps (15 secondes), 8 essais." +
                          "\n\t3. Mot de 8 lettres, une lettre donnée aléatoirement, limite de temps (45 secondes), 8 essais." +
                          "\n=====\n? ");


            while (!new int[] { 1, 2, 3 }.Contains(difficultyLevel))
            {
                difficultyLevel = CheckIntInput("Mauvaise entrée ! Sélectionner un niveau de difficulté valide :\n? ");
            }

            #endregion

            //switch (difficultyLevel)
            //{
            //    case 1:
            //        letterNb = 4;

            //}


            // init game object
            this.game = new Core(letterNb, triesNb, difficultyLevel);
            //this.game = new Core(8, 5, 1);
        }

        public static int CheckIntInput(string err) 
        {
            int bfr = 0;

            try
            {
                bfr = int.Parse(Console.ReadLine());
            }
            catch (FormatException e)
            {
                Console.Write(err);
            }

            return bfr;
        }

        public void Start()
        {
            // present word with one letter according to difficulty level
            this.IntroduceGamePlay();
            this.lastGuessTime = this.watch.ElapsedMilliseconds;
            
            this.DisplayFeedback();

            while (this.game.triesNb - nbTried > 0 && !this.game.isWon)
            {

                string input = Console.ReadLine();
                while (input.Length != this.game.letterNb)
                {
                    Console.Write("Veuillez entrer le bon nombre de lettres :\n");
                    input = Console.ReadLine();
                }

                double time = this.watch.ElapsedMilliseconds - this.lastGuessTime;
                this.game.CheckPosition(input.ToUpper(), (int) time);
                this.lastGuessTime = time;
                
                this.DisplayFeedback();
            }

            this.EndGame();
        }

        public void EndGame()
        {
            Timer.Enabled = false;
            bool gw = this.game.isWon;
            Console.WriteLine("C'est fini ! Vous {0}avez {1} réussi à deviner " +
            "le mot mystère qui était \"{2}\" !", (gw) ? "" : "n'", (gw) ? "" : "pas", this.game.word);
        }

        private void IntroduceGamePlay()
        {
            Console.WriteLine("Bienvenue sur Motus ! " + 
            "Votre but est de deviner le mot suivant " +
            "en moins de {0} essais ", this.game.triesNb);
        }

        private void DisplayFeedback()
        {
            //string 
            string[] hist = this.game.History;
            
            ConsoleColor[] colors = new ConsoleColor[]
            {
                ConsoleColor.Red, 
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
            };

            foreach (string t in hist)
            {
                if (string.IsNullOrEmpty(t)) { continue;}

                string[] values = t.Split('|');
                string input = values[0];
                string feedback = values[1];

                for (int i = 0; i < feedback.Length; i++)
                {
                    int f = (int) char.GetNumericValue(feedback[i]);
                    Console.BackgroundColor = colors[f];
                    Console.Write(input[i]);
                }

                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine();
            }
        }
    }
}