using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Motus
{
    class CLI
    {
        private Core game;
        private int startTime;
        private int lastGuessTime;
        
        public CLI()
        {
            #region Game Paramter Initialization

            Console.Write("Sélectionnez le nombre de lettres du mots :\n? ");

            int letterNb = 0;

            while (letterNb < 6 && letterNb > 10)
            {
                letterNb = CheckIntInput("Mauvaise entrée ! Réessayez :\n? ");
            }

            Console.Write("Sélectionnez le nombre d'essais :\n? ");

            int triesNb = 0;

            while (triesNb <= letterNb && triesNb > 0)
            {
                triesNb = CheckIntInput("Mauvaise entrée ! Sélectionner un nombre inférieur au nombre de lettres dans le mot :\n? ");
            }
 
            Console.Write("Sélectionnez le niveau de difficulté :" +
                          "\n\t1. Première lettre donnée, pas de limite de temps" +
                          "\n\t2. Première lettre donnée, limite de temps (tzempsojdf)" +
                          "\n\t3. Une lettre donnée aléatoirement, limite de temps (kdjfg)" +
                          "\n=====\n? ");

            int difficultyLevel = 0;

            while (!new int[] {1,2,3}.Contains(difficultyLevel))
            {
                difficultyLevel = CheckIntInput("Mauvaise entrée ! Sélectionner un niveau de difficulté valide :\n? ");
            }

            #endregion

            // init game object
            this.game = new Core(letterNb, triesNb, difficultyLevel);
        }

        public static int CheckIntInput(string err) 
        {
            int bfr = 0;

            try
            {
                bfr = int.Parse(Console.ReadLine());
            }
            catch (FormatException)
            {
                Console.Write(err);
            }

            return bfr;
        }

        public void Start()
        {
            // present word with one letter according to difficulty level
            this.IntroduceGamePlay();

            while (this.game.triesNb - this.game.triesDone > 0)
            {
                this.DisplayFeedback();

                // this.game.CheckPosition(Console.ReadLine(),  - this.lastGuessTime);
            }

            this.EndGame();
        }

        public void EndGame() 
        {
            bool gw = this.game.isWon;
            Console.WriteLine("C'est fini ! Vous {0}avez {1} réussi à deviner" +
            "le mot mystère !", (gw) ? "" : "n'", (gw) ? "" : "pas");
        }

        private void IntroduceGamePlay()
        {
            Console.WriteLine("Bienvenue sur Motus ! " + 
            "Votre but est de deviner le mot suivant " +
            "en moins de {0} essais ", this.game.triesNb);
        }

        private void DisplayFeedback()
        {
            string[] hist = this.game.History;
            
            ConsoleColor[] colors = new ConsoleColor[]
            {
                ConsoleColor.Red, 
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
            };

            foreach (string t in hist)
            {
                string[] values = t.Split('|');
                string input = values[0];
                string feedback = values[1];

                for (int i = 0; i < feedback.Length; i++)
                {
                    int f = (int) char.GetNumericValue(feedback[i]);
                    Console.BackgroundColor = colors[f];
                    Console.Write(input[i]);
                }
                Console.WriteLine();
            }
        }
    }
}