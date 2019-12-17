using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Motus
{
    class cli
    {
        private core game;

        public cli()
        {
            Console.Write("Sélectionnez le nombre de lettres du mots :\n? ");
            int letterNb = 0;
            while (letterNb < 6 && letterNb > 10)
            {
                try
                {
                    letterNb = int.Parse(Console.ReadLine());
                }
                catch (FormatException)
                {
                    Console.Write("Mauvaise entrée ! Réessayez :\n? ");
                }
            }
            
            
            Console.Write("Sélectionnez le nombre d'essais :\n? ");
            int triesNb = 0;
            while (triesNb <= letterNb && triesNb > 0)
            {
                try
                {
                    triesNb = int.Parse(Console.ReadLine());
                }
                catch (FormatException)
                {
                    Console.Write("Mauvaise entrée ! Sélectionner un nombre inférieur au nombre de lettres dans le mot :\n? ");
                }
            }
            
            
            Console.Write("Sélectionnez le niveau de difficulté :" +
                          "\n\t1. Première lettre donnée, pas de limite de temps" +
                          "\n\t2. Première lettre donnée, limite de temps (tzempsojdf)" +
                          "\n\t3. Une lettre donnée aléatoirement, limite de temps (kdjfg)" +
                          "\n=====\n? ");
            int difficultyLevel = 0;
            while (!new int[] {1,2,3}.Contains(difficultyLevel))
            {
                try
                {
                    difficultyLevel = int.Parse(Console.ReadLine());
                }
                catch (FormatException)
                {
                    Console.Write("Mauvaise entrée ! Sélectionner un niveau de difficulté valide :\n? ");
                }
            }
            
            LaunchGame(letterNb, triesNb, difficultyLevel);
        }

        private void LaunchGame(int letterNb, int triesNb, int difficultyLevel)
        {
            this.game = new core(letterNb, triesNb, difficultyLevel);

            // boucler sur l'input

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