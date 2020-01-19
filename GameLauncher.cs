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

            SaveData(this._game.IsWon, this._game.History, this._game.DifficultyLevel, (this._game.History.Length - 1));
            Statistics(this._game.IsWon,this._game.DifficultyLevel, this._game.History) ;
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
        
        private void SaveData(bool won, string[] histo, int niv, int nbtenta)
        {
            string datapath = "../../../Resources/data.txt";
            if (File.Exists(datapath)==false)//if data.txt does not exist, create data.txt
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
            

            if (won) //if the game is won, the game data are saved : Difficulty level, overall time, average time by word, number of try
            {
                // j'ai un doute sur l'enregistrement du temps dans history à quoi il correspnd
                
                //Average time by word on this game
                int avgTime = 0;
                for (int i = 1; i < histo.Length; i++)
                {
                    avgTime += (int.Parse(histo[i].Split("|")[2]) - int.Parse(histo[i - 1].Split("|")[2]));
                }
                avgTime /= (histo.Length - 1);
                string entry = String.Format("{0},{1},{2},{3}", niv.ToString(), histo.Where(c => c != null).ToArray().Last().Split("|")[2], avgTime.ToString(), nbtenta.ToString() );

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

        public void Statistics(bool won, int level, string[] tab)
        {
            string datapath = "../../../Resources/data.txt";

            int nbTry = tab.Length - 1;

            double avgTry = 0;
            double avgTimeTry = 0;
            double avgTimeTot = 0;

            try
            {
                string[] lines = File.ReadAllLines(datapath);
                
                for (int i=3;i<lines.Length;i++)
                {
                    int t = int.Parse(lines[i].Split(",")[0]);
                    if (t==level)
                    { 
                        avgTimeTot+=int.Parse(lines[i].Split(",")[1]);
                        avgTimeTry += int.Parse(lines[i].Split(",")[2]);
                        avgTry += int.Parse(lines[i].Split(",")[3]);
                    }
                }
                avgTimeTot /= (lines.Length - 3);
                avgTimeTry /= (lines.Length - 3);
                avgTry /= (lines.Length - 3);

                double infAvgTry = 0;
                double infAvgTimeTry = 0;
                double infAvgTimeTot = 0;
              
                for (int i=3; i<lines.Length;i++)
                {
                    if (int.Parse(lines[i].Split(",")[3])<avgTry)
                    {
                        infAvgTry += 1;
                    }
                    if (int.Parse(lines[i].Split(",")[1])<avgTimeTot)
                    {
                        infAvgTimeTot += 1;
                    }
                    if (int.Parse(lines[i].Split(",")[2])<avgTimeTry)
                    {
                        infAvgTimeTry += 1;
                    }
                }
                
                double supAvgTry = lines.Length-3-infAvgTry;
                double supAvgTimeTry = lines.Length - 3 -infAvgTimeTry;
                double supAvgTimeTot = lines.Length - 3 -infAvgTimeTot;

                infAvgTry /= (lines.Length - 3) * 100;
                supAvgTry/=(lines.Length-3)*100;
                infAvgTimeTry/=(lines.Length-3)*100;
                supAvgTimeTry/=(lines.Length-3)*100;
                infAvgTimeTot/=(lines.Length-3)*100;
                supAvgTimeTot/=(lines.Length-3)*100;

                Console.WriteLine("\tLes Statistiques pour le niveau {0}", level);
                Console.WriteLine("Pour le niveau {0} choisi, le temps moyen par tentative était de {1} secondes, le temps moyen par partie était de {2} secondes \n", level, Math.Round((avgTimeTry / 1000), 1).ToString("0.0"), Math.Round((avgTimeTot / 1000), 1).ToString("0.0"));

                Console.WriteLine("Nombres de tentatives");

                Console.BackgroundColor = ConsoleColor.Yellow;
                for (int i = 0; i < infAvgTry+1; i+=2)
                {
                    Console.Write(" ");
                }
                Console.ResetColor();
                Console.Write(" {0} % ont réalisé moins de tentatives que la moyenne\n", Math.Round(infAvgTry, 1).ToString("0.0"));

                Console.BackgroundColor = ConsoleColor.Yellow;
                for (int i = 0; i < supAvgTry+1; i+=2)
                {
                    Console.Write(" ");
                }
                Console.ResetColor();
                Console.WriteLine(" {0} % ont réalisé plus de tentatives que la moyenne\n",Math.Round(supAvgTry, 1).ToString("0.0") );

    
                Console.WriteLine("Temps moyen par tentative ");

                Console.BackgroundColor = ConsoleColor.DarkYellow;
                for (int i = 0; i < infAvgTimeTry+1; i += 2)
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
                if(won)
                {
                    double infAvgTryPerso = 0;
                    double infAvgTimeTryPerso = 0;
                    double infAvgTimeTotPerso = 0;
                    double idemAvgTryPerso = 0;
                    double idemAvgTimeTryPerso = 0;
                    double idemAvgTimeTotPerso = 0;


                    for (int i=3; i<lines.Length-1;i++)//doesn't count the last game which is the game studied
                    {
                        if (int.Parse(lines[i].Split(",")[3])<avgTry)
                        {
                            infAvgTryPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[3]) == avgTry)
                        {
                            idemAvgTryPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[1])<avgTimeTot)
                        {
                            infAvgTimeTotPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[1]) == avgTimeTot)
                        {
                            idemAvgTimeTotPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[2])<avgTimeTry)
                        {
                            infAvgTimeTryPerso += 1;
                        }
                        if (int.Parse(lines[i].Split(",")[2]) == avgTimeTry)
                        {
                            idemAvgTimeTryPerso += 1;
                        }
                    }
                
                    double supAvgTryPerso = lines.Length-4-infAvgTryPerso- idemAvgTryPerso;
                    double supAvgTimeTryPerso = lines.Length - 4 -infAvgTimeTryPerso - idemAvgTimeTryPerso;
                    double supAvgTimeTotPerso = lines.Length - 4 -infAvgTimeTotPerso - idemAvgTimeTotPerso ;              

                    infAvgTryPerso/= (lines.Length - 4) * 100;
                    idemAvgTryPerso /= (lines.Length - 4) * 100;
                    supAvgTryPerso/=(lines.Length-4)*100;
                    infAvgTimeTryPerso/=(lines.Length-4)*100;
                    idemAvgTimeTryPerso /= (lines.Length - 4) * 100;
                    supAvgTimeTryPerso/=(lines.Length-4)*100;
                    infAvgTimeTotPerso/=(lines.Length-4)*100;
                    idemAvgTimeTotPerso /= (lines.Length - 4) * 100;
                    supAvgTimeTotPerso/=(lines.Length-4)*100;
                
                    
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

                    IDictionary<int, string[]> myScreenParams;
                    myScreenParams = MyRenderer.RenderScreen("StatScreen");*/
                    #endregion

                    Console.WriteLine("\tVos Statistiques pour le niveau {0}", level);
                    Console.WriteLine("Vous avez terminé la partie en {0} tentative(s) et {1} secondes", nbTry, int.Parse(tab.Where(c => c != null).ToArray().Last().Split("|")[2]) / 1000);
                    //Console.WriteLine("Vous avez terminé la partie en {0} tentative(s) et {1} secondes", (this._game.History.Length-1),int.Parse(this._game.History.Where(c => c != null).ToArray().Last().Split("|")[2])/1000 );

                    Console.WriteLine("Nombres de tentatives");

                    Console.BackgroundColor = ConsoleColor.Cyan;
                    for (int i = 0; i < infAvgTryPerso+1; i+=2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.Write(" {0} % ont réalisé moins de tentatives que vous\n", infAvgTryPerso);

                    Console.BackgroundColor = ConsoleColor.Cyan;
                    for (int i = 0; i < idemAvgTryPerso + 1; i+=2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.Write(" {0} % ont réalisé autant de tentatives que vous\n", idemAvgTryPerso);

                    Console.BackgroundColor = ConsoleColor.Cyan;
                    for (int i = 0; i < supAvgTryPerso+1; i+=2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.WriteLine(" {0} % ont réalisé plus de tentatives que vous\n", supAvgTryPerso);

    
                    Console.WriteLine("Temps moyen par tentative ");

                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    for (int i = 0; i < infAvgTimeTryPerso + 1; i += 2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.Write(" {0} % ont un temps par tentative inférieur au votre\n", Math.Round(infAvgTimeTryPerso, 1).ToString("0.0"));

                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    for (int i = 0; i < idemAvgTimeTryPerso + 1; i += 2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.Write(" {0} % ont un temps par tentative identique au votre\n", Math.Round(idemAvgTimeTryPerso, 1).ToString("0.0"));

                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    for (int i = 0; i < supAvgTimeTryPerso + 1; i += 2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.WriteLine(" {0} % ont un temps par tentative supérieur au votre\n", Math.Round(supAvgTimeTryPerso, 1).ToString("0.0"));


                    Console.WriteLine("Temps total moyen");

                    Console.BackgroundColor = ConsoleColor.Green;
                    for (int i = 0; i < infAvgTimeTotPerso + 1; i += 2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.Write(" {0} % ont un temps de résolution inférieur au votre\n", Math.Round(infAvgTimeTotPerso, 1).ToString("0.0"));

                    Console.BackgroundColor = ConsoleColor.Green;
                    for (int i = 0; i < idemAvgTimeTotPerso + 1; i += 2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.Write(" {0} % ont un temps de résolution identique au votre\n", Math.Round(idemAvgTimeTotPerso, 1).ToString("0.0"));

                    Console.BackgroundColor = ConsoleColor.Green;
                    for (int i = 0; i < supAvgTimeTotPerso + 1; i += 2)
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                    Console.WriteLine(" {0} % ont un temps de résolution supérieur au votre\n", Math.Round(supAvgTimeTotPerso, 1).ToString("0.0"));
                }
                
            }
            catch (Exception ex)
            {
                Console.Write("Une erreur est survenue au cours de l'opération :");
                Console.WriteLine(ex.Message);
            }
            //Console.ReadLine();
        }
    }
}