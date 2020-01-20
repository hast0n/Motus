using System;
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
    public class GameHasEndedException : Exception
    {
        public GameHasEndedException()
        {
        }

        public GameHasEndedException(string message)
            : base(message)
        {
        }

        public GameHasEndedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    class GameLauncher
    {
        private Renderer MyRenderer;
        private Stopwatch _watch;
        private GameCore _game;
        private Timer _timer;
        private double _lastGuessTime;
        private bool _isLive;
        public Dictionary<string, List<string>> ScreenResources;

        private int NbTried => this._game.History.Where(c => c != null).ToArray().Length - 1;

        private void SetGameRenderer()
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
                RegexScreenParamDelimiterPattern = @"([1-9]*)\[([A-za-z0-9]+)\]",
                RegexInputDelimiterPattern = @"<(?:input|color):[^>]+>",
                RegexInputParamDelimiterPattern = @"<(input|color):([^>]+)>",
            };

            MyRenderer.InitDefault();
            SetRendererResources();
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
                    "gameplayBotBar",
                    $"{MyRenderer.PaddingString}└{MyRenderer.HorizontalBar}┘\n"
                },
                {
                    "intro",
                    Join(MyRenderer.SplitChar, 
                        "Bonjour et bienvenue dans Motus, le jeu dans lequel vous devinez des mots !!", 
                        "Wahoooo c'est trop génial ! Commencez par choisir parmis ",
                        "un dès niveaux de difficultés suivants :")
                },
                {
                    "level1",
                    Join(MyRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 1 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 4 lettres dont la première est donnée.",
                        "Vous avez 6 essaies et aucune limite de temps pour trouver le mot."
                    )
                },
                {
                    "level2",
                    Join(MyRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 2 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 5 lettres dont une donnée aléatoirement.",
                        "Vous avez 10 essaies et 1 minutes pour trouver le mot."
                    )
                },
                {
                    "level3",
                    Join(MyRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 3 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 8 lettres dont une donnée aléatoirement.",
                        "Vous avez 8 essaies et 45 secondes pour trouver le mot."
                    )
                },
                {
                    "level4",
                    Join(MyRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 4 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 6 lettres dont une donnée aléatoirement.",
                        "Vous avez 8 essaies et 30 secondes pour trouver le mot."
                    )
                },
                {
                    "level5",
                    Join(MyRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 5 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 5 lettres dont une donnée aléatoirement.",
                        "vous avez 8 essaies et 15 secondes pour trouver le mot."
                    )
                },
                {
                    // characters used : │ ─ ├ ┼ ┤ ┌ ┬ ┐ └ ┴ ┘
                    "title",
                    Join(MyRenderer.SplitChar, "" +
                        "<color:blue>┌─┐    ┌─┐<color:black> <color:red>  ┌────┐  <color:black> <color:green>┌────────┐<color:black> <color:magenta>┌─┐   ┌─┐<color:black> <color:yellow> ┌───────┐<color:black>",
                        "<color:blue>│ └─┐┌─┘ │<color:black> <color:red>┌─┘┌──┐└─┐<color:black> <color:green>└──┐  ┌──┘<color:black> <color:magenta>│ │   │ │<color:black> <color:yellow>┌┘┌──────┘<color:black>", 
                        "<color:blue>│ ┌┐└┘┌┐ │<color:black> <color:red>│  │  │  │<color:black> <color:green>   │  │   <color:black> <color:magenta>│ │   │ │<color:black> <color:yellow>└┐└─────┐ <color:black>", 
                        "<color:blue>│ │└──┘│ │<color:black> <color:red>│  │  │  │<color:black> <color:green>   │  │   <color:black> <color:magenta>│ │   │ │<color:black> <color:yellow> └─────┐└┐<color:black>", 
                        "<color:blue>│ │    │ │<color:black> <color:red>└─┐└──┘┌─┘<color:black> <color:green>   │  │   <color:black> <color:magenta>└┐└───┘┌┘<color:black> <color:yellow>┌──────┘┌┘<color:black>",
                        "<color:blue>└─┘    └─┘<color:black> <color:red>  └────┘  <color:black> <color:green>   └──┘   <color:black> <color:magenta> └─────┘ <color:black> <color:yellow>└───────┘ <color:black>"
                        )
                },
                {
                    "empty",
                    "\n"
                },
                {
                    "levels",
                    Join(MyRenderer.SplitChar, "" +
                        "┌─────┬──────────────────────────────────────────────────────────────────────┐",
                        "│  1  │  4 lettres dont la première donnée, pas de limite de temps, 6 essais │",
                        "├     ┼                                                                      │",
                        "│  2  │  5 lettres dont la première donnée, 1 minute, 10 essais              │",
                        "├     ┼                                                                      │",
                        "│  3  │  8 lettres dont une donnée aléatoirement, 45 secondes, 8 essais      │",
                        "├     ┼                                                                      │",
                        "│  4  │  6 lettres dont une donnée aléatoirement, 30 secondes, 8 essais      │",
                        "├     ┼                                                                      │",
                        "│  5  │  5 lettres dont une donnée aléatoirement, 15 secondes, 8 essais      │",
                        "└─────┴──────────────────────────────────────────────────────────────────────┘"
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
                    "levelHint",
                    "Saisissez une option au clavier et validez en appuyant sur Entrée."
                },
                {
                    "gameplayHint",
                    Join(MyRenderer.SplitChar, ""+
                        "Saisissez les lettres qui composent selon vous le mot mystère !",
                        "Saisissez-les unes à unes et appuyez sur Entrée quand vous avez terminé.",
                        "Bonne chance !"
                        )
                },
                {
                    "badWordError",
                    Join(MyRenderer.SplitChar, ""+
                        "/!\\ Le mot que vous avez sélectionné n'existe pas ou n'est pas valide /!\\",
                        "Vérifiez que le mot contienne les lettres validés et qu'il soit correctement orthographié !"
                    )
                },
                {
                    "caption",
                    Join(MyRenderer.SplitChar, ""+
                        "┌── Légende ───────────────────────────┐",
                        "│ <color:red> <color:black> -> lettre correcte et bien placée  │",
                        "│ <color:yellow> <color:black> -> lettre correcte mais mal placée │",
                        "│ <color:blue> <color:black> -> lettre incorrecte               │",
                        "└──────────────────────────────────────┘"
                    )
                },
                {
                    "inputAreaTextIndicator",
                    "Saisissez ci-dessous :"
                },
                {
                    "startHint",
                    "<color:white> Appuyez sur n'importe quelle touche pour commencer <color:black>"
                },
                {
                    "displayStats",
                    Join(MyRenderer.SplitChar, "┌─────┬──────────────────────────────────────────────────────────────────────┐",
                        "│  6  │  Afficher les statistiques de jeu                                    │",
                        "└─────┴──────────────────────────────────────────────────────────────────────┘"
                    )
                },
                {
                    "backToMainMenu",
                    "<color:white> Appuyez sur n'importe quelle touche pour revenir au menu principal... <color:black>"
                }
            };

            // [(.*)] : group that needs encapsulation
            // ([1-9]*)\[([a-z]+)\] : group that needs encapsulation and can be repeated
            ScreenResources = new Dictionary<string, List<string>>()
            {
                {
                    "WelcomeScreen", new List<string>
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",
                        
                        "empty",

                        "topBar", "2[empty]",

                        "[intro]", "[empty]", "[levels]", "2[empty]", "[levelInput]", "[levelHint]",

                        "[empty]", "botBar",
                    }
                },
                {
                    "GameplayScreen", new List<string>
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",

                        "empty",

                        "topBar", "[empty]", "<1>", "[empty]",

                        "[gameplayHint]", "[empty]", "[caption]", "2[empty]",

                        "<2>", "[empty]", "[gameplayInput]", 
                        
                        "[empty]", "<3>", "[empty]", "<4>",

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
            _isLive = false;
            MyRenderer.canInput = false;
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

        private void BuildUserFeedbackString(bool buildWithInput = true)
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

            if (buildWithInput)
            {
                inputStringBuilder.Append(
                    $"--> │ {Concat(Enumerable.Repeat("<input:[A-Za-z]>", _game.LetterNb))} │ <--\n"
                );
            }

            inputStringBuilder.Append(botBar);

            MyRenderer.VisualResources["gameplayInput"] = inputStringBuilder.ToString();
        }

        public void LaunchMainMenu()
        {
            bool play = true;

            while (play)
            {
                // Game Renderer is wiped out when exiting current game loop
                // Even if it is not a clean solution to deal with the screen still waiting for input,
                // it helps to deal with asynchronous bad behaviour due to Timer
                SetGameRenderer(); 

                IDictionary<int, string[]> myScreenParams = MyRenderer.RenderScreen(ScreenResources["WelcomeScreen"]);
                int userChoice = int.Parse(myScreenParams[myScreenParams.Keys.Min()][0]);

                if (new[] { 1, 2, 3, 4, 5 }.Contains(userChoice))
                {
                    try
                    {
                        Start(userChoice);
                    }
                    catch (GameHasEndedException e)
                    {
                        Console.Clear();
                        // Exemple pour la page rejouer
                    }
                }
            }
        }

        public void Start(int difficultyLevel)
        {
            List<string> screenTemplate = ScreenResources["GameplayScreen"].ToList(); // .ToList() To create a copy

            int startHintIndex = ScreenResources["GameplayScreen"].IndexOf("<2>");
            int levelIndicatorIndex = ScreenResources["GameplayScreen"].IndexOf("<1>");
            int infoIndex = ScreenResources["GameplayScreen"].IndexOf("<3>");

            screenTemplate[levelIndicatorIndex] = $"[level{difficultyLevel}]";
            screenTemplate[startHintIndex] = "[startHint]";

            MyRenderer.RenderScreen(screenTemplate);
            screenTemplate[startHintIndex] = "[inputAreaTextIndicator]";

            SetGameParameters(difficultyLevel);
            
            _isLive = true;
            _lastGuessTime = _watch.ElapsedMilliseconds;
            
            while (_game.TriesNb - NbTried > 0 && _isLive)
            {
                BuildUserFeedbackString();
                IDictionary<int, string[]> myScreenParams = MyRenderer.RenderScreen(screenTemplate);
                if (!_isLive) { throw new GameHasEndedException("game has ended."); }
                string userInput = Concat(myScreenParams.Values.Select(x => x[0]));

                // prevent keeping cycling after timeout
                while (!_game.Dictionary.Contains(userInput.ToLower()) && _isLive)
                {
                    screenTemplate[infoIndex] = "[badWordError]";
                    myScreenParams = MyRenderer.RenderScreen(screenTemplate);
                    if (!_isLive) { throw new GameHasEndedException("game has ended."); }
                    userInput = Concat(myScreenParams.Values.Select(x => x[0]));
                }

                screenTemplate[infoIndex] = "[empty]";

                double time = _watch.ElapsedMilliseconds - _lastGuessTime;
                _game.CheckPosition(userInput.ToUpper(), (int)time);
                _lastGuessTime = time;

                // !_isLive to handle time out
                if (_game.IsWon)
                {
                    break;
                }

                if (!_isLive)
                {
                    throw new GameHasEndedException("game has ended.");
                } 
            }

            this.EndGame();
        }

        public void EndGame()
        {
            _timer.Enabled = false;
            bool gw = _game.IsWon;
            string gameDuration = $"{_watch.Elapsed.Minutes}:{_watch.Elapsed.Seconds}";

            MyRenderer.VisualResources["ending"] = Format("C'est terminé ! Vous {0}avez {1} réussi à deviner " +
            "le mot mystère qui était \"{2}\" !{3}", (gw) ? "" : "n'", (gw) ? "" : "pas", this._game.Word,
                (gw) ? $"\nTemps total : {gameDuration}" : "");

            BuildUserFeedbackString(false);
            List<string> screenTemplate = ScreenResources["GameplayScreen"];

            int levelIndicatorIndex = ScreenResources["GameplayScreen"].IndexOf("<1>");
            int startHintIndex = ScreenResources["GameplayScreen"].IndexOf("<2>");
            int infoIndex = ScreenResources["GameplayScreen"].IndexOf("<3>");
            int backIndex = ScreenResources["GameplayScreen"].IndexOf("<4>");

            screenTemplate[levelIndicatorIndex] = $"[level{_game.DifficultyLevel}]";
            screenTemplate[startHintIndex] = "[inputAreaTextIndicator]";
            screenTemplate[infoIndex] = "[ending]";
            screenTemplate[backIndex] = "[backToMainMenu]";

            MyRenderer.RenderScreen(screenTemplate);

            this._game.SaveData();
        }

        public void ShowStatistics()
        {
            string[,] tab = this._game.Statistics();
            IDictionary<int, string[]> myScreenParams;

            //myScreenParams = MyRenderer.RenderScreen("WelcomeScreen");

            //si la premiere ligne de tab est nulle lors première partie

            /*Console.WriteLine("\tLes Statistiques pour le niveau {0}", DifficultyLevel);
            Console.WriteLine("Pour le niveau {0} choisi, le temps moyen par tentative était de {1} secondes, le temps moyen par partie était de {2} secondes \n", DifficultyLevel, Math.Round((avgTimeTry / 1000), 1).ToString("0.0"), Math.Round((avgTimeTot / 1000), 1).ToString("0.0"));

            Console.WriteLine("Nombres de tentatives");

            Console.BackgroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < infAvgTry + 1; i += 2)
            {
                Console.Write(" ");
            }
            Console.ResetColor();
            Console.Write(" {0} % ont réalisé moins de tentatives que la moyenne\n", Math.Round(infAvgTry, 1).ToString("0.0"));

            Console.BackgroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < supAvgTry + 1; i += 2)
            {
                Console.Write(" ");
            }
            Console.ResetColor();
            Console.WriteLine(" {0} % ont réalisé plus de tentatives que la moyenne\n", Math.Round(supAvgTry, 1).ToString("0.0"));


            Console.WriteLine("Temps moyen par tentative ");

            Console.BackgroundColor = ConsoleColor.DarkYellow;
            for (int i = 0; i < infAvgTimeTry + 1; i += 2)
            {
                Console.Write(" ");
            }
            Console.ResetColor();
            Console.Write(" {0} % ont un temps par tentative inférieur au temps moyen\n", Math.Round(infAvgTimeTry, 1).ToString("0.0"));

            Console.BackgroundColor = ConsoleColor.DarkYellow;
            for (int i = 0; i < supAvgTimeTry + 1; i += 2)
            {
                Console.Write(" ");
            }
            Console.ResetColor();
            Console.WriteLine(" {0} % ont un temps par tentative supérieur au temps moyen\n", Math.Round(supAvgTimeTry, 1).ToString("0.0"));


            Console.WriteLine("Temps total moyen");

            Console.BackgroundColor = ConsoleColor.Red;
            for (int i = 0; i < infAvgTimeTot + 1; i += 2)
            {
                Console.Write(" ");
            }
            Console.ResetColor();
            Console.Write(" {0} % ont un temps de résolution inférieur au temps moyen\n", Math.Round(infAvgTimeTot, 1).ToString("0.0"));

            Console.BackgroundColor = ConsoleColor.Red;
            for (int i = 0; i < supAvgTimeTot + 1; i += 2)
            {
                Console.Write(" ");
            }
            Console.ResetColor();
            Console.WriteLine(" {0} % ont un temps de résolution supérieur au temps moyen\n", Math.Round(supAvgTimeTot, 1).ToString("0.0"));
            
            Console.WriteLine("\tVos Statistiques pour le niveau {0}", DifficultyLevel);
            Console.WriteLine("Vous avez terminé la partie en {0} tentative(s) et {1} secondes", nbTry, int.Parse(History.Where(c => c != null).ToArray().Last().Split("|")[2]) / 1000);
            //Console.WriteLine("Vous avez terminé la partie en {0} tentative(s) et {1} secondes", (this._game.History.Length-1),int.Parse(this._game.History.Where(c => c != null).ToArray().Last().Split("|")[2])/1000 );
            */
            #region 
            /*MyRenderer.VisualResources = new Dictionary<string, string>
            {
                {
                "bar",
                "_"
                },
                {
                "vbar",
                "|"
                },
                {
                "angle1",
                "┌"
                },
                {
                "angle2",
                "┐"
                },
                {
                "angle3",
                "└"
                },
                {
                "angle4",
                "┘"
                },
                {
                    "tentatives",
                    string.Join(MyRenderer.SplitChar,
                        "Nombres de tentatives")
                },
                {
                    "tempstent",
                    string.Join(MyRenderer.SplitChar,
                        "Temps moyen par tentative")
                },
                {
                    "tempstot",
                    string.Join(MyRenderer.SplitChar,
                        "Temps total moyen")
                },
                {
                    "empty",
                    "\n"
                },
                {
                    "stattrya",
                    string.Join(MyRenderer.SplitChar, ""+
                        infAvgTryPerso/(lines.Length-4)*100+"% des joueurs ont réalisés moins de tentatives que vous"
                        )
                },
                {
                    "stattryb",
                    string.Join(MyRenderer.SplitChar, ""+
                        supAvgTryPerso/(lines.Length-4)*100+"% des joueurs ont réalisés plus de tentatives que vous"
                        )
                },
                {
                    "statttota",
                    string.Join(MyRenderer.SplitChar, ""+
                        infAvgTimeTotPerso/(lines.Length-4)*100+"% des joueurs ont réalisés moins de tentatives que vous"
                        )
                },
                {
                    "statttotb",
                    string.Join(MyRenderer.SplitChar, ""+
                        supAvgTimeTotPerso/(lines.Length-4)*100+"% des joueurs ont réalisés plus de tentatives que vous"
                        )
                },
                {
                    "stattmoya",
                    string.Join(MyRenderer.SplitChar, ""+
                        infAvgTimeTryPerso/(lines.Length-4)*100+"% des joueurs ont réalisés moins de tentatives que vous"
                        )
                },
                {
                    "stattmoyb",
                    string.Join(MyRenderer.SplitChar, ""+
                        supAvgTimeTryPerso/(lines.Length-4)*100+"% des joueurs ont réalisés plus de tentatives que vous"
                        )
                }
            };
            MyRenderer.ScreenResources = new Dictionary<string, string[]>()
            {
                {

                    "StatScreen", new []
                    {
                        "tentatives",

                        "stattrya",
                        "angle1","4[bar]", "angle2",
                        "vbar","[empty]","vbar",
                        "angle3","3[bar]",  "angle4",
                        "stattryb",

                        "empty",

                        "tempstent",
                        "stattmoya",
                        "angle1","[bar]",  "angle2",
                        "vbar","[empty]","vbar",
                        "angle3","7[bar]",  "angle4",
                        "stattmoyb",

                        "empty",

                        "tempstot",
                        "statttota",
                        "angle1","bar",  "angle2",
                        "vbar","[empty]","vbar",
                        "angle3","bar",  "angle4",
                        "statttotb",
                    }
                }
            };

            //IDictionary<int, string[]> myScreenParams;
            //myScreenParams = MyRenderer.RenderScreen("StatScreen");*/
            #endregion


        }




    }
}