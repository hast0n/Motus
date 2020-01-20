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
                        "---> │ <input:[1-6]{1}> │ <---",
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
                },
                {
                    "descStat",
                    Join(MyRenderer.SplitChar, ""+
                        "Statistiques de jeu",
                        "Vous trouverez ci dessous les statistiques pour la dernière partie jouée.")
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

                        "[intro]", "[empty]", "[levels]","[displayStats]", "2[empty]", "[levelInput]", "[levelHint]",

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
                {
                    "StatisticScreen", new List<string>
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",

                        "empty",

                        "topBar", "[empty]", 

                        "[empty]", "[descStat]", "[empty]",

                        "[empty]","<1>", "[empty]",

                        "[empty]",

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
            IDictionary<int, string[]> myScreenParams;
            while (play)
            {
                // Game Renderer is wiped out when exiting current game loop
                // Even if it is not a clean solution to deal with the screen still waiting for input,
                // it helps to deal with asynchronous bad behaviour due to Timer
                SetGameRenderer(); 

                myScreenParams = MyRenderer.RenderScreen(ScreenResources["WelcomeScreen"]);
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
                else
                {
                    ShowStatistic();
                    myScreenParams = MyRenderer.RenderScreen(ScreenResources["StatisticScreen"]);
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

        public void ShowStatistic()
        {
            string[,] tab = GameCore.Statistics();

            IDictionary<int, string[]> myScreenParams;

            List<string> screenTemplate = ScreenResources["StatisticScreen"].ToList();

            int statIndex = ScreenResources["GameplayScreen"].IndexOf("<1>");

            MyRenderer.VisualResources["TabStat"] = Join(MyRenderer.SplitChar, "" +
                "┌──────────────────────────────────────────────────────────┬────────┬────────┐",
                "│ Statistiques sur le dernier niveau joué                  │ < en % │ > en % |",
                "├──────────────────────────────────────────────────────────┼────────┼────────┤",
                "│  Nombre de tentatives moyenne : "+ tab[0, 0].Split("|")[2] + "                        |   " + tab[0, 0].Split("|")[0] + "  │   " + tab[0, 1].Split("|")[1] + "  │",
                "│                                                          ┼        ┼        ┤",
                "│  Temps par tentative moyen : "+ tab[0, 1].Split("|")[2] + "                      |   " + tab[0, 1].Split("|")[0] + "  │   " + tab[0, 1].Split("|")[1] + "  │",
                "│                                                          ┼        ┼        ┤",
                "│  Temps total moyen : "+ tab[0, 2].Split("|")[2] + "                              |   " + tab[0, 2].Split("|")[0] + "  │   " + tab[0, 2].Split("|")[1] + "  │",
                "└──────────────────────────────────────────────────────────┴────────┴────────┘",
                "┌──────────────────────────────────────────────────────────┬─────────┬─────────┬─────────┐",
                "│ Distribution des données enregistrées                    │         │         |         │",
                "| par rapport à la dernière partie jouée                   │ < en %  │ > en %  | = en %  │",
                "├──────────────────────────────────────────────────────────┼─────────┼─────────┼─────────┤",
                "│  Nombre de tentatives moyenne                            |   " + tab[1, 0].Split("|")[0] + "   │   " + tab[1, 0].Split("|")[1] + "   │   " + tab[1, 0].Split("|")[2] + "   │",
                "│                                                          ┼         ┼         ┼         ┤",
                "│  Temps moyen par tentative                               |   " + tab[1, 1].Split("|")[0] + "   │   " + tab[1, 1].Split("|")[1] + "   │   " + tab[1, 1].Split("|")[2] + "   │",
                "│                                                          ┼         ┼         ┼         ┤",
                "│  Temps total moyen                                       |   " + tab[1, 2].Split("|")[0] + "   │   " + tab[1, 2].Split("|")[1] + "   │   " + tab[1, 2].Split("|")[2] + "   │",
                "└──────────────────────────────────────────────────────────┴─────────┴─────────┴─────────┘"
            );
            
            screenTemplate[statIndex] = "TabStat";
            MyRenderer.RenderScreen(screenTemplate);

        }
    }
}