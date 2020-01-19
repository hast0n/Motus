﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;// indispensable pour lire et ecrire un fichier
using System.Linq;
using System.Text;
using System.Timers;
using static System.String;

//using System.Xml.Linq;//csv


namespace Motus
{
    class GameLauncher
    {
        private readonly Renderer MyRenderer;
        private Stopwatch _watch;
        private GameCore _game;
        private Timer _timer;
        private double _lastGuessTime;
        private bool _isLive;

        private int NbTried => this._game.History.Where(c => c != null).ToArray().Length - 1;

        public GameLauncher()
        {
            MyRenderer = new Renderer()
            {
                WindowWidth = 120,
                WindowHeight = 50,
                GamePadding = 5,
                HorizontalLineChar = '─',
                VerticalLineChar = '│',
                SplitChar = '\n',
                // https://regexr.com/4s4lb
                RegexTextAttributeDelimiterPattern = @"(<.*>)",
                //RegexScreenParamDelimiterPattern = @"(?:([1-9]+)\*)?(?:([a-z]+)|(?:\[[a-z]+\]))",
                RegexScreenParamDelimiterPattern = @"([1-9]*)\[([A-za-z]+)\]",
                RegexInputDelimiterPattern = @"<(?:input|color):[^>]+>",
                RegexInputParamDelimiterPattern = @"<(input|color):([^>]+)>"
            };

            MyRenderer.InitDefault();
            SetRendererResources();

            //tests csv
            //string[] tab = { "chaud|00212|6000", "chute|00000|12000" };
            string[] tab = { "chien|00011|6000", "chine|00000|13000" };
            // SaveToCsv(true, tab,1,2);
        }

        private void SetRendererResources()
        {
            MyRenderer.VisualResources = new Dictionary<string, string>
            {
                {
                    "topBar",
                    $"{MyRenderer.PaddingString}┌{MyRenderer.HorizontalBar}┐\n"
                },
                {
                    "botBar",
                    $"{MyRenderer.PaddingString}└{MyRenderer.HorizontalBar}┘\n"
                },
                //{
                //    "gameplaytopbar",
                //    $"┌{new string('─', _game.LetterNb * )}┐\n"
                //},
                {
                    "gameplayBotBar",
                    $"{MyRenderer.PaddingString}└{MyRenderer.HorizontalBar}┘\n"
                },
                {
                    "intro",
                    Join(MyRenderer.SplitChar, 
                        "Bonjour et bienvenue sur Motus, le jeu dans le quel vous devinez des mots !!", 
                        "Wahoooo c'est trop génial ! Allez, vas-y choisis un niveau :")
                },
                {
                    // characters used : │ ─ ├ ┼ ┤ ┌ ┬ ┐ └ ┴ ┘
                    "title",
                    Join(MyRenderer.SplitChar, "" +
                        " ┌─┐    ┌─┐   ┌────┐   ┌────────┐ ┌─┐   ┌─┐   ┌───────┐ ",
                        " │ └─┐┌─┘ │ ┌─┘┌──┐└─┐ └──┐  ┌──┘ │ │   │ │  ┌┘┌──────┘ ", 
                        " │ ┌┐└┘┌┐ │ │  │  │  │    │  │    │ │   │ │  └┐└─────┐  ", 
                        " │ │└──┘│ │ │  │  │  │    │  │    │ │   │ │   └─────┐└┐ ", 
                        " │ │    │ │ └─┐└──┘┌─┘    │  │    └┐└───┘┌┘  ┌──────┘┌┘ ",
                        " └─┘    └─┘   └────┘      └──┘     └─────┘   └───────┘  "
                        )
                },
                {
                    "empty",
                    "\n"
                },
                {
                    "levels",
                    Join(MyRenderer.SplitChar, "" +
                        "┌───┐  ┌─────────────────────────────────────────────────────────────────────┐",
                        "│ 1 │──│ 4 lettres dont la première donnée, pas de limite de temps, 6 essais │",
                        "└───┘  └─────────────────────────────────────────────────────────────────────┘",
                        "┌───┐  ┌─────────────────────────────────────────────────────────────────────┐",
                        "│ 2 │──│       5 lettres dont la première donnée, 1 minute, 10 essais        │",
                        "└───┘  └─────────────────────────────────────────────────────────────────────┘",
                        "┌───┐  ┌─────────────────────────────────────────────────────────────────────┐",
                        "│ 3 │──│   8 lettres dont une donnée aléatoirement, 45 secondes, 8 essais    │",
                        "└───┘  └─────────────────────────────────────────────────────────────────────┘",
                        "┌───┐  ┌─────────────────────────────────────────────────────────────────────┐",
                        "│ 4 │──│   6 lettres dont une donnée aléatoirement, 30 secondes, 8 essais    │",
                        "└───┘  └─────────────────────────────────────────────────────────────────────┘",
                        "┌───┐  ┌─────────────────────────────────────────────────────────────────────┐",
                        "│ 5 │──│   5 lettres dont une donnée aléatoirement, 15 secondes, 8 essais    │",
                        "└───┘  └─────────────────────────────────────────────────────────────────────┘"
                    )
                },
                {
                    "levelInput",
                    Join(MyRenderer.SplitChar,
                        "┌───┐",
                        "---> │ <input:[1-5]{1}> │ <---",
                        "└───┘"
                    )
                },
                {
                    "gameplayHint",
                    Join(MyRenderer.SplitChar, ""+
                        "Saisissez les lettres qui composent selon vous le mot mystère !",
                        "Saisissez-les unes par unes et appuyez sur [Entrée] quand vous avez terminé :"
                        )
                },
                {
                    "badWordError",
                    Join(MyRenderer.SplitChar, ""+
                        "/!\\ Le mot que vous avez sélectionné n'existe pas ou n'est pas valide /!\\",
                        "Vérifiez que le mot contienne les lettres validés et qu'il soit correctement orthographié !"
                    )
                }
            };

            // [(.*)] : group that needs encapsulation
            // ([1-9]*)\[([a-z]+)\] : group that needs encapsulation and can be repeated
            MyRenderer.ScreenResources = new Dictionary<string, List<string>>()
            {
                {
                    "WelcomeScreen", new List<string>
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",
                        
                        "empty",

                        "topBar", "2[empty]",

                        "[intro]", "3[empty]", "[levels]", "[empty]", "[levelInput]",

                        "[empty]", "botBar",
                    }
                },
                {
                    "GameplayScreen", new List<string>
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",

                        "empty",

                        "topBar", "2[empty]",

                        "[gameplayHint]", "3[empty]", "[gameplayInput]", "[empty]", "<1>", "[empty]",

                        "[empty]", "botBar",
                    }
                },
            };


        }

        private void SetTimer()
        {

            int timeLap;

            switch (this._game.DifficultyLevel)
            {
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
                    timeLap = 60000;
                    break;
            }

            _timer = new Timer(timeLap);
            _timer.Elapsed += OnTimeElapsed;
            _timer.AutoReset = false;
            _timer.Enabled = this._game.DifficultyLevel != 1;
            _watch = Stopwatch.StartNew();
        }

        private void OnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("{0} Le temps est écoulé ! {0}", new string('-', 5));
            EndGame();
        }

        private void SetGameParameters(int difficultyLevel)
        {
            int letterNb = 0;
            int triesNb = 8;
            switch (difficultyLevel)
            {
                case 1:
                    letterNb = 4;
                    triesNb = 6;
                    break;
                case 2:
                    letterNb = 5;
                    triesNb = 10;
                    break;
                case 3:
                    letterNb = 8;
                    break;
                case 4:
                    letterNb = 6;
                    break;
                case 5:
                    letterNb = 5;
                    break;
            }
            this._game = new GameCore(letterNb, triesNb, difficultyLevel);
            SetTimer();
        }

        private void BuildWordInputString()
        {
            string topBar = $"┌─{new string('─', _game.LetterNb)}─┐{MyRenderer.SplitChar}";
            string botBar = $"└─{new string('─', _game.LetterNb)}─┘";
            StringBuilder inputStringBuilder = new StringBuilder(topBar);
            string[] colors = new [] {"red", "yellow", "blue"};

            foreach (var userTry in _game.History.Where(x => !IsNullOrEmpty(x)))
            {
                string colorDelimiters = string.Empty;
                string[] userTryStrings = userTry.Split('|');

                for (int i = 0; i < _game.LetterNb; i++)
                {
                    string color = colors[(int)char.GetNumericValue(userTryStrings[1][i])];
                    char letter = userTryStrings[0][i];
                    colorDelimiters += $"<color:{color}>{letter}";
                }

                colorDelimiters += "<color:black>";

                inputStringBuilder.Append(Join(MyRenderer.SplitChar,
                    $"│ {colorDelimiters} │{MyRenderer.SplitChar}"));
            }

            inputStringBuilder.Append(Join(MyRenderer.SplitChar,
                $"│ {Concat(Enumerable.Repeat("<input:[A-Za-z]>", _game.LetterNb))} │",
                botBar
            ));

            MyRenderer.VisualResources["gameplayInput"] = inputStringBuilder.ToString();
        }

        public void Start()
        {
            IDictionary<int, string[]> myScreenParams;

            myScreenParams = MyRenderer.RenderScreen("WelcomeScreen");
            int difficultyLevel = int.Parse(myScreenParams[myScreenParams.Keys.Min()][0]);
            SetGameParameters(difficultyLevel);
            
            _isLive = true;
            _lastGuessTime = this._watch.ElapsedMilliseconds;


            while (this._game.TriesNb - this.NbTried > 0 && this._isLive) // prevent cycling after a correct answer
            {
                BuildWordInputString();
                myScreenParams = MyRenderer.RenderScreen("GameplayScreen");
                string userInput = Concat(myScreenParams.Values.Select(x => x[0]));

                // prevent keeping cycling after timeout
                while (!this._game.Dictionary.Contains(userInput.ToLower()) && this._isLive)
                {
                    // TODO : ne pas changer directement le GameplayScreen utiliser asset
                    int errorIndex = MyRenderer.ScreenResources["GameplayScreen"].IndexOf("<1>");
                    MyRenderer.ScreenResources["GameplayScreen"][errorIndex] = "[badWordError]";
                    myScreenParams = MyRenderer.RenderScreen("GameplayScreen");
                    userInput = Concat(myScreenParams.Values.Select(x => x[0]));
                }

                //if (!this._isLive) { break; } // get out of gameplay if timed out

                double time = this._watch.ElapsedMilliseconds - this._lastGuessTime;
                this._game.CheckPosition(userInput.ToUpper(), (int)time);
                this._lastGuessTime = time;

                //MyRenderer.RenderScreen(MyRenderer.InGameScreen);
            }

            this.EndGame();
        }

        public void EndGame()
        {
            _timer.Enabled = false;
            bool gw = this._game.IsWon;
            Console.WriteLine("C'est fini ! Vous {0}avez {1} réussi à deviner " +
            "le mot mystère qui était \"{2}\" !", (gw) ? "" : "n'", (gw) ? "" : "pas", this._game.Word);
        }
        
        private void DisplayFeedback()
        {
            //string 
            string[] hist = this._game.History;
            
            ConsoleColor[] colors = new ConsoleColor[]
            {
                ConsoleColor.Red, 
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
            };

            foreach (string t in hist)
            {
                if (IsNullOrEmpty(t)) { continue;}

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
        /*public void SaveToCsv() 
       {
           string csvpath = "../../../Resources/data.csv";
           if (this.IsWon)//On sauvegarde les données que si la partie est gagnée
           {
               StringBuilder csvnewline = new StringBuilder();
               string addline = '"'+this._game.DifficultyLevel.ToString()+','+this._game.History.Where(c => c != null).ToArray().Last().Split("|")[2] +',' +  this.NbTried'"';
               csvnewline.AppendLine(addline);
               File.AppendAllText(csvpath,csvnewline.ToString());
           }

       }*/

       /* private void SaveToCsv(bool won, string[] histo, int niv, int nbtenta)
        {
            string csvpath = "../../../Resources/data.csv";

            if (won)//On sauvegarde les données que si la partie est gagnée
            {
                string[] addline = { niv.ToString(), histo.Where(c => c != null).ToArray().Last().Split("|")[2], nbtenta.ToString() };
                File.AppendAllLines(csvpath, addline);       
                //string addline = "\""+niv.ToString()+","+histo.Where(c => c != null).ToArray().Last().Split("|")[2]+","+nbtenta.ToString()+"\"";
                //File.AppendAllText(csvpath, addline};  
            }

        }*/
        private void SaveData(bool won, string[] histo, int niv, int nbtenta)
        {
            string datapath = "../../../Resources/data.txt";
            if (won) // sauvegarde des données si la partie est gagnée
            {
                //niveau, temps total, temps moyen par mot, nb de tendative
            }
        }
        public void Statistics(int level)
        {
            Console.WriteLine("Vous avez terminé la partie en {0} tentative(s) et {1} secondes", (this._game.History.Length-1),int.Parse(this._game.History.Where(c => c != null).ToArray().Last().Split("|")[2])/100 );
            int avgtword = 0;
            int avgttotal = 0;
            //parcours du fichier pour calcul de la moyenne de temps par mot et de la moyenne temps total
            Console.WriteLine("Pour le niveau choisi {0}, le temps moyen par était de {1}",level, avgtword,avgttotal);
            //Diagramme en baton horizontal pour décrire le taux de parties plsu rapides et de parties plus lentes Voir si faut le déplacer
            // Parcours du fichier pour conpter les valeurs inférieures à la moyenne.
            // regarder le renderer pour construire un diagramme à baton horizontal ou juste faire avec ConsoleWrite?
            Console.ReadLine();
        }
    }
}