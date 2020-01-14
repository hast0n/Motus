using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;

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
                RegexTextAttributeDelimiterPattern = @"(<.*>)",
                //RegexScreenParamDelimiterPattern = @"(?:([1-9]+)\*)?(?:([a-z]+)|(?:\[[a-z]+\]))",
                RegexScreenParamDelimiterPattern = @"([1-9]*)\[([a-z]+)\]",
                RegexInputDelimiterPattern = @"<(input|color):(int|string)>",
            };

            MyRenderer.InitDefault();
            SetRendererResources();
            SetGameParameters();
            SetTimer();
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
                {
                    "intro",
                    string.Join(MyRenderer.SplitChar, 
                        "Bonjour et bienvenue sur Motus, le jeu dans le quel vous devinez des mots !!", 
                        "Wahoooo c'est trop génial ! Allez, vas-y choisis un niveau :")
                },
                {
                    // characters used : │ ─ ├ ┼ ┤ ┌ ┬ ┐ └ ┴ ┘
                    "title",
                    string.Join(MyRenderer.SplitChar, "" +
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
                    string.Join(MyRenderer.SplitChar, "" +
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
                    "levelinput",
                    string.Join(MyRenderer.SplitChar, 
                        "┌───┐", 
                        "---> │ <input:int> │ <---",
                        "└───┘"
                    )
                }
            };

            // [(.*)] : group that needs encapsulation
            // ([1-9]*)\[([a-z]+)\] : group that needs encapsulation and can be repeated
            MyRenderer.ScreenResources = new Dictionary<string, string[]>()
            {
                {
                    "WelcomeScreen", new []
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",
                        
                        "empty",

                        "topBar", "2[empty]",

                        "[intro]", "3[empty]", "[levels]", "[empty]", "[levelinput]",

                        "[empty]", "botBar",
                    }
                }
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

        private void SetGameParameters()
        {
            #region Game Paramter Initialization

            //int letterNb = 0;
            //int triesNb = 8;
            //int difficultyLevel = 0;

            //Console.Write("Sélectionnez le niveau de difficulté :" +
            //    "\n\t1. Mot de 4 lettres, première lettre donnée, pas de limite de temps, 6 essais." +
            //    "\n\t2. Mot de 5 lettres, première lettre donnée, limite de temps (1 minute), 10 essais." +
            //    "\n\t3. Mot de 8 lettres, une lettre donnée aléatoirement, limite de temps (45 secondes), 8 essais." +
            //    "\n\t4. Mot de 6 lettres, une lettre donnée aléatoirement, limite de temps (30 secondes), 8 essais." +
            //    "\n\t5. Mot de 5 lettres, une lettre donnée aléatoirement, limite de temps (15 secondes), 8 essais." +
            //    "\n=====\n? ");

            //while (!Enumerable.Range(1, 5).Contains(difficultyLevel)) 
            //{
            //    difficultyLevel = CheckIntInput("Mauvaise entrée ! Sélectionner un niveau de difficulté valide :\n? ");
            //}

            //switch (difficultyLevel)
            //{
            //    case 1:
            //        letterNb = 4;
            //        triesNb = 6;
            //        break;
            //    case 2:
            //        letterNb = 5;
            //        triesNb = 10;
            //        break;
            //    case 3:
            //        letterNb = 8;
            //        break;
            //    case 4:
            //        letterNb = 6;
            //        break;
            //    case 5:
            //        letterNb = 5;
            //        break;
            //}

            #endregion

            // init game object
            //this._game = new GameCore(letterNb, triesNb, difficultyLevel);
            this._game = new GameCore(5, 5, 1);
        }

        public static int CheckIntInput(string err) 
        {
            int bfr = 0;

            try
            {
                // if ReadLine() return type is null, sends back "&" to trigger FormatException
                bfr = int.Parse(Console.ReadLine()??"&");
            }
            catch (FormatException)
            {
                Console.Write(err);
            }

            return bfr;
        }

        public void Start()
        {
            this.IntroduceGamePlay();

            this._isLive = true;
            this._lastGuessTime = this._watch.ElapsedMilliseconds;

            MyRenderer.RenderScreen("WelcomeScreen");


            while (this._game.TriesNb - this.NbTried > 0 && this._isLive) // prevent cycling after a correct answer
            {

                string input = Console.ReadLine();
                while (!this._game.Dictionary.Contains(input?.ToLower()) && !this._isLive) // prevent keeping cycling after timeout
                {
                    Console.Write("Le mot sélectionné n'est pas valide, réessayez :\n");
                    input = Console.ReadLine();
                }

                if (!this._isLive) { break; } // get out of gameplay if

                double time = this._watch.ElapsedMilliseconds - this._lastGuessTime;
                this._game.CheckPosition(input?.ToUpper(), (int) time);
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

        private void IntroduceGamePlay()
        {
            Console.WriteLine("Bienvenue sur Motus ! " + 
            "Votre but est de deviner le mot suivant " +
            "en moins de {0} essais ", this._game.TriesNb);
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