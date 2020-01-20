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
        private Renderer _myRenderer;
        private Stopwatch _watch;
        private GameCore _game;
        private Timer _timer;
        private double _lastGuessTime;
        private bool _isLive;
        public int DifficultyLevel;
        public Dictionary<string, List<string>> ScreenResources;

        // Get length of history to know how many inputs has the user done
        // (-1 to avoid taking history initializer in account)
        private int NbTried => this._game.History.Where(c => c != null).ToArray().Length - 1;

        private void SetGameRenderer()
        {
            // Setup renderer parameters
            _myRenderer = new Renderer()
            {
                WindowWidth = 120,
                WindowHeight = 50,
                GamePadding = 5,
                HorizontalLineChar = '─',
                VerticalLineChar = '│',
                SplitChar = '\n',
                // https://regexr.com/4s4lb
                RegexTextAttributeDelimiterPattern = @"(<.*>)",
                RegexScreenParamDelimiterPattern = @"([1-9]*)\[([A-za-z0-9]+)\]",
                RegexInputDelimiterPattern = @"<(?:input|color):[^>]+>",
                RegexInputParamDelimiterPattern = @"<(input|color):([^>]+)>",
            };

            _myRenderer.InitDefault();
            SetRendererResources();
        }

        private void SetRendererResources()
        {
            // Define visual resources
            _myRenderer.VisualResources = new Dictionary<string, string>
            {
                {
                    "topBar",
                    $"{_myRenderer.PaddingString}┌{_myRenderer.HorizontalBar}┐\n"
                },
                {
                    "botBar",
                    $"{_myRenderer.PaddingString}└{_myRenderer.HorizontalBar}┘\n"
                },
                {
                    "gameplayBotBar",
                    $"{_myRenderer.PaddingString}└{_myRenderer.HorizontalBar}┘\n"
                },
                {
                    "intro",
                    Join(_myRenderer.SplitChar, 
                        "Bonjour et bienvenue dans Motus, le jeu dans lequel vous devinez des mots !!", 
                        "Wahoooo c'est trop génial ! Commencez par choisir parmis ",
                        "un dès niveaux de difficultés suivants :")
                },
                {
                    "level1",
                    Join(_myRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 1 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 4 lettres dont la première est donnée.",
                        "Vous avez 6 essaies et aucune limite de temps pour trouver le mot."
                    )
                },
                {
                    "level2",
                    Join(_myRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 2 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 5 lettres dont une donnée aléatoirement.",
                        "Vous avez 10 essaies et 1 minutes pour trouver le mot."
                    )
                },
                {
                    "level3",
                    Join(_myRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 3 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 8 lettres dont une donnée aléatoirement.",
                        "Vous avez 8 essaies et 45 secondes pour trouver le mot."
                    )
                },
                {
                    "level4",
                    Join(_myRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 4 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 6 lettres dont une donnée aléatoirement.",
                        "Vous avez 8 essaies et 30 secondes pour trouver le mot."
                    )
                },
                {
                    "level5",
                    Join(_myRenderer.SplitChar, "┌──────────┐",
                        "│ Niveau 5 │",
                        "└──────────┘", " ",
                        "Le mot à deviner contient 5 lettres dont une donnée aléatoirement.",
                        "vous avez 8 essaies et 15 secondes pour trouver le mot."
                    )
                },
                {
                    // characters used : │ ─ ├ ┼ ┤ ┌ ┬ ┐ └ ┴ ┘
                    "title",
                    Join(_myRenderer.SplitChar, "" +
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
                    Join(_myRenderer.SplitChar, "" +
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
                    Join(_myRenderer.SplitChar,
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
                    Join(_myRenderer.SplitChar, ""+
                        "Saisissez les lettres qui composent selon vous le mot mystère !",
                        "Saisissez-les unes à unes et appuyez sur Entrée quand vous avez terminé.",
                        "Bonne chance !"
                        )
                },
                {
                    "badWordError",
                    Join(_myRenderer.SplitChar, ""+
                        "/!\\ Le mot que vous avez sélectionné n'existe pas ou n'est pas valide /!\\",
                        "Vérifiez que le mot contienne les lettres validés et qu'il soit correctement orthographié !"
                    )
                },
                {
                    "caption",
                    Join(_myRenderer.SplitChar, ""+
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
                    Join(_myRenderer.SplitChar, "┌─────┬──────────────────────────────────────────────────────────────────────┐",
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
                    Join(_myRenderer.SplitChar, ""+
                        "Statistiques de jeu",
                        "Vous trouverez ci dessous les statistiques pour la dernière partie jouée.")
                }
            };

            // Define screen resources
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
                    "StatisticsScreen", new List<string>
                    {
                        "empty",

                        "topBar", "[empty]", "[title]", "[empty]", "botBar",

                        "empty",

                        "topBar", "[empty]", 

                        "[empty]", "[descStat]", "[empty]",

                        "[empty]","<1>", "[empty]",

                        "2[empty]", "[backToMainMenu]",

                        "[empty]", "botBar",
                    }
                },
            };
        }

        private void SetTimer()
        {

            int timeLap;

            // Set timer according to difficulty level
            switch (DifficultyLevel)
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
            // Call OnTimeElapsed when the timer reaches timeLap
            _timer.Elapsed += OnTimeElapsed;
            _timer.AutoReset = false;
            // Only use timer when difficulty level is above 1
            _timer.Enabled = DifficultyLevel != 1;
            // Start a stopwatch for statistic purpose
            _watch = Stopwatch.StartNew();
        }

        private void OnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            // Prevent any user input by disabling 
            _isLive = false;
            _myRenderer.CanInput = false;
            EndGame();
        }

        private void SetGameParameters()
        {
            int letterNb = 0;
            int triesNb = 8;

            // Get basic game parameters with DifficultyLevel
            switch (DifficultyLevel)
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

            // Initialize game object with previously defined parameters
            this._game = new GameCore(letterNb, triesNb, DifficultyLevel > 2);
            // Call timer initialization
            SetTimer();
        }

        private void BuildUserFeedbackString(bool buildWithInput = true)
        {
            // This method displays user attempt stored in history
            // and adds an input for the next attempt

            string topBar = $"┌─{new string('─', _game.LetterNb)}─┐{_myRenderer.SplitChar}";
            string botBar = $"└─{new string('─', _game.LetterNb)}─┘";
            StringBuilder inputStringBuilder = new StringBuilder(topBar);
            string[] colors = new [] {"red", "yellow", "blue"};

            // For every user input, create a string with modifiers (colors) to feed the Renderer
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
                inputStringBuilder.Append(Join(_myRenderer.SplitChar,
                    $"│ {colorDelimiters} │{_myRenderer.SplitChar}"));
            }

            // If user input is needed, add the corresponding formatted string to feed the Renderer
            // The string should contain a number of inputs equal to LetterNb
            if (buildWithInput)
            {
                inputStringBuilder.Append(
                    $"--> │ {Concat(Enumerable.Repeat("<input:[A-Za-z]>", _game.LetterNb))} │ <--\n"
                );
            }

            inputStringBuilder.Append(botBar);

            _myRenderer.VisualResources["gameplayInput"] = inputStringBuilder.ToString();
        }

        public void LaunchMainMenu()
        {
            // Main Game Loop
            while (true)
            {
                // ---
                SetGameRenderer(); 

                /*
                When a game has already been played in this instance of Gamelauncher:
                Game Renderer is wiped out when exiting current game iteration
                Why ? Because of Timer's asynchronous behaviour, the Renderer is 
                still waiting for an input even if the game has ended. So by losing
                its reference, the garbage collector is supposed to take care of him 
                and we can now create another one.

                When no game has been played before :
                A new Renderer is created and there is no unexpected behaviour.
                */

                // ---

                // Display welcome screen and get user input
                IDictionary<int, string[]> myScreenParams = _myRenderer.RenderScreen(ScreenResources["WelcomeScreen"]);
                // Parse user input to int
                int userChoice = int.Parse(myScreenParams[myScreenParams.Keys.Min()][0]);

                if (new[] { 1, 2, 3, 4, 5 }.Contains(userChoice))
                {
                    DifficultyLevel = userChoice;
                    try
                    {
                        Start();
                    }
                    // GameHasEndedException also exists to block weird behaviour
                    // when the timer ends the game and the loop is still asking
                    // for input
                    catch (GameHasEndedException) { /* */ }
                }
                else
                {
                    ShowStatistic();
                }
            }
        }

        public void Start()
        {
            // Get copy of stored template
            List<string> screenTemplate = ScreenResources["GameplayScreen"].ToList();

            // Register indexes of placeholders
            int startHintIndex = ScreenResources["GameplayScreen"].IndexOf("<2>");
            int levelIndicatorIndex = ScreenResources["GameplayScreen"].IndexOf("<1>");
            int infoIndex = ScreenResources["GameplayScreen"].IndexOf("<3>");

            // Apply modifications to display template
            screenTemplate[levelIndicatorIndex] = $"[level{DifficultyLevel}]";
            screenTemplate[startHintIndex] = "[startHint]";

            // Use Renderer to display screen
            _myRenderer.RenderScreen(screenTemplate);
            screenTemplate[startHintIndex] = "[inputAreaTextIndicator]";

            // Launch GameCore and Timer initialization
            SetGameParameters();
            
            _isLive = true;
            _lastGuessTime = _watch.ElapsedMilliseconds;
            
            while (_game.TriesNb - NbTried > 0 && !_game.IsWon)
            {
                // Create feedback & input string
                BuildUserFeedbackString();

                // Wait for input
                IDictionary<int, string[]> myScreenParams = _myRenderer.RenderScreen(screenTemplate);

                // If game has ended while waiting for input, throw exception
                if (!_isLive) { throw new GameHasEndedException("game has ended."); }

                // Cast user input to a single string
                string userInput = Concat(myScreenParams.Values.Select(x => x[0]));

                // If input is not correct
                while (!_game.Dictionary.Contains(userInput.ToLower()) && _isLive)
                {
                    // Set bad word error on screen template
                    screenTemplate[infoIndex] = "[badWordError]";

                    // Display screen template and wait for another input
                    myScreenParams = _myRenderer.RenderScreen(screenTemplate);

                    // If game has ended while waiting for input, throw exception
                    if (!_isLive) { throw new GameHasEndedException("game has ended."); }

                    // Cast user input to a single string
                    userInput = Concat(myScreenParams.Values.Select(x => x[0]));
                }

                // User input is valid -> wipe bad word error message
                screenTemplate[infoIndex] = "[empty]";

                // Register time for current input
                double time = _watch.ElapsedMilliseconds - _lastGuessTime;

                // Check input against word to guess
                _game.CheckPosition(userInput.ToUpper(), (int) time);
                _lastGuessTime = time;
            }

            this.EndGame();
        }

        public void EndGame()
        {
            // Reset game over parameters
            _timer.Enabled = false;
            bool gw = _game.IsWon;

            // Extract game duration
            string gameDuration = $"{_watch.Elapsed.Minutes}:{_watch.Elapsed.Seconds}";

            // Create an ending message
            _myRenderer.VisualResources["ending"] = Format("C'est terminé ! Vous {0}avez {1} réussi à deviner " +
            "le mot mystère qui était \"{2}\" !{3}", (gw) ? "" : "n'", (gw) ? "" : "pas", this._game.Word,
                (gw) ? $"\nTemps total : {gameDuration}" : "");

            // Build user feedback string whithout input
            BuildUserFeedbackString(false);

            // Get indexes to set ending screen
            List<string> screenTemplate = ScreenResources["GameplayScreen"];

            int levelIndicatorIndex = ScreenResources["GameplayScreen"].IndexOf("<1>");
            int startHintIndex = ScreenResources["GameplayScreen"].IndexOf("<2>");
            int infoIndex = ScreenResources["GameplayScreen"].IndexOf("<3>");
            int backIndex = ScreenResources["GameplayScreen"].IndexOf("<4>");

            screenTemplate[levelIndicatorIndex] = $"[level{DifficultyLevel}]";
            screenTemplate[startHintIndex] = "[inputAreaTextIndicator]";
            screenTemplate[infoIndex] = "[ending]";
            screenTemplate[backIndex] = "[backToMainMenu]";

            // Display screen
            _myRenderer.RenderScreen(screenTemplate);

            // Call method that saves data on disk
            SaveData();
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



            if (this._game.IsWon) //if the game is won, the game data are saved : Difficulty level, overall time, average time by word, number of try
            {
                //Average time by word on this game
                int avgTime = 0;
                int overallTime = 0;
                int i = 1;
                while (i < _game.History.Length)
                {
                    while (this._game.History[i] != null)
                    {
                        avgTime += (int.Parse(this._game.History[i].Split("|")[2]) - int.Parse(this._game.History[i - 1].Split("|")[2]));
                        overallTime += int.Parse(this._game.History[i].Split("|")[2]);
                    }

                    i++;
                }
                avgTime /= (i - 1);
                string entry = String.Format("{0},{1},{2},{3}", DifficultyLevel.ToString(), overallTime.ToString(), avgTime.ToString(), (this._game.History.Length - 1).ToString());

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
        }

        public static string[,] Statistics()
        {
            string datapath = "../../../Resources/data.txt";

            string[,] dataStat = new string[2, 3];

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
                string[] lines = File.ReadAllLines(datapath);  // array of all the file's lines

                // Statistics for the last level saved
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

                // let's build percentage for the last level saved
                infAvgTry = (infAvgTry / cpt) * 100;
                supAvgTry = (supAvgTry / cpt) * 100;
                infAvgTimeTry = (infAvgTimeTry / cpt) * 100;
                supAvgTimeTry = (supAvgTimeTry / cpt) * 100;
                infAvgTimeTot = (infAvgTimeTot / cpt) * 100;
                supAvgTimeTot = (supAvgTimeTot / cpt) * 100;


                // performance comparison criteria for the last performance
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

                //let's build percentage for how the last player perform
                infAvgTryPerso = (infAvgTryPerso / cptPerso) * 100;
                idemAvgTryPerso = (idemAvgTryPerso / cptPerso) * 100;
                supAvgTryPerso = (supAvgTryPerso / cptPerso) * 100;
                infAvgTimeTryPerso = (infAvgTimeTryPerso / cptPerso) * 100;
                idemAvgTimeTryPerso = (idemAvgTimeTryPerso / cptPerso) * 100;
                supAvgTimeTryPerso = (supAvgTimeTryPerso / cptPerso) * 100;
                infAvgTimeTotPerso = (infAvgTimeTotPerso / cptPerso) * 100;
                idemAvgTimeTotPerso = (idemAvgTimeTotPerso / cptPerso) * 100;
                supAvgTimeTotPerso = (supAvgTimeTotPerso / cptPerso) * 100;

                //Save all of those percentage in dataStat
                dataStat[0, 0] = String.Format("{0}|{1}|{2}", Math.Round(infAvgTry, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(infAvgTry, 1).ToString("0.0"), Math.Round(supAvgTry, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(supAvgTry, 1).ToString("0.0"), avgTry.ToString() == "NaN" ? "0" : avgTry.ToString()); ;
                dataStat[0, 1] = String.Format("{0}|{1}|{2} s", Math.Round(infAvgTimeTry, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(infAvgTimeTry, 1).ToString("0.0"), Math.Round(supAvgTimeTry, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(supAvgTimeTry, 1).ToString("0.0"), Math.Round(avgTimeTry, 1).ToString("0.0") == "NaN" ? "00.0" : Math.Round(avgTimeTry / 1000, 1).ToString("0.0"));
                dataStat[0, 2] = String.Format("{0}|{1}|{2} s", Math.Round(infAvgTimeTot, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(infAvgTimeTot, 1).ToString("0.0"), Math.Round(infAvgTimeTot, 1).ToString("0.0") == "NaN" ? "0.0" : Math.Round(supAvgTimeTot, 1).ToString("0.0"), Math.Round(avgTimeTot, 1).ToString("0.0") == "NaN" ? "00.0" : Math.Round(avgTimeTot / 1000, 1).ToString("0.0"));

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

        public void ShowStatistic()
        {
            string[,] tab = Statistics();

            List<string> screenTemplate = ScreenResources["StatisticsScreen"].ToList();

            int statIndex = ScreenResources["StatisticsScreen"].IndexOf("<1>");

            _myRenderer.VisualResources["TabStat"] = Join(_myRenderer.SplitChar, "" +
                "┌──────────────────────────────────────────────────────────┬────────┬─────────┐",
                "│ Statistiques sur le dernier niveau joué                  │ < en % │ >= en % │",
                "└──────────────────────────────────────────────────────────┼────────┼─────────┘",
                "  Nombre de tentatives moyenne : "+ tab[0, 0].Split("|")[2] + "                           " + tab[0, 0].Split("|")[0] + "     " + tab[0, 0].Split("|")[1],
                " ",
                "  Temps par tentative moyen : "+ tab[0, 1].Split("|")[2] + "                         " + tab[0, 1].Split("|")[0] + "     " + tab[0, 1].Split("|")[1],
                " ",
                "  Temps total moyen : "+ tab[0, 2].Split("|")[2] + "                                 " + tab[0, 2].Split("|")[0] + "     " + tab[0, 2].Split("|")[1],
                " ", " ",
                "┌──────────────────────────────────────────────────────────┬─────────┬─────────┬─────────┐",
                "│ Distribution des données enregistrées                    │         │         │         │",
                "│ par rapport à la dernière partie jouée                   │ < en %  │ > en %  │ = en %  │",
                "└──────────────────────────────────────────────────────────┼─────────┼─────────┼─────────┘",
                "  Nombre de tentatives moyenne                               " + tab[1, 0].Split("|")[0] + "      " + tab[1, 0].Split("|")[1] + "      " + tab[1, 0].Split("|")[2],
                " ",
                "  Temps moyen par tentative                                  " + tab[1, 1].Split("|")[0] + "      " + tab[1, 1].Split("|")[1] + "      " + tab[1, 1].Split("|")[2],
                " ",
                "  Temps total moyen                                          " + tab[1, 2].Split("|")[0] + "      " + tab[1, 2].Split("|")[1] + "      " + tab[1, 2].Split("|")[2]
            );
            
            screenTemplate[statIndex] = "[TabStat]";
            _myRenderer.RenderScreen(screenTemplate);

        }
    }
}