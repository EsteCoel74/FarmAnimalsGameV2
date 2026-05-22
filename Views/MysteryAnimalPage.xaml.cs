using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO.Ports;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
namespace FarmAnimalsGameV2.Views
{
    public class AnimalCard
    {
        public string RfidTag { get; set; }
        public string Name { get; set; }
        public string Emoji { get; set; }
        public string Hint { get; set; }
        public string ImageFile { get; set; }
    }

    public partial class MysteryAnimalPage : UserControl
    {
        // ── Paramètres ─────────────────────────────────────────
        public int TimerSeconds { get; set; } = 30;

        public enum Difficulty { Facile, Moyen, Difficile }
        public Difficulty CurrentDifficulty { get; set; } = Difficulty.Facile;

        // ── Event retour vers DifficultyPage ───────────────────
        public event Action? RoundCompleted;

        private int LivesForDifficulty()
        {
            if (CurrentDifficulty == Difficulty.Difficile) return 1;
            if (CurrentDifficulty == Difficulty.Moyen) return 2;
            return 3;
        }

        // ── Constantes ─────────────────────────────────────────
        private const int TotalRounds = 3;
        private const string HeartFull = "❤️"; // cœur plein rouge
        private const string HeartEmpty = "🖤"; // cœur perdu

        // ── État ───────────────────────────────────────────────
        private int _lives;
        private int _score;
        private int _round;       // round actuel (1 à TotalRounds)
        private int _timeLeft;
        private AnimalCard _currentAnimal;
        private int _correctIndex;
        private bool _waitingForRfid;
        private MediaPlayer _mediaPlayer = new MediaPlayer();

        private readonly DispatcherTimer _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private readonly List<AnimalCard> _catalog = new List<AnimalCard>
        {
        new AnimalCard { RfidTag="6FDB2B3E", Name="Vache",  Emoji="🐄", Hint="Donne du lait chaque matin",        ImageFile="vache.jpg"  },
        new AnimalCard { RfidTag="32FE281D", Name="Cochon",  Emoji="🐷", Hint="Se roule dans la boue",             ImageFile="cochon.jpg" },
        new AnimalCard { RfidTag="42EA691D", Name="Poulet",  Emoji="🐔", Hint="Pond des œufs tous les jours",      ImageFile="poule.jpg"  },
        new AnimalCard { RfidTag="6EAD4A74", Name="Mouton",  Emoji="🐑", Hint="Sa laine tient chaud l'hiver",      ImageFile="mouton.jpg" },
        new AnimalCard { RfidTag="EE455374", Name="Cheval",  Emoji="🐴", Hint="Court très vite dans les prés",     ImageFile="cheval.jpg" },
        new AnimalCard { RfidTag="3E5D4A74", Name="Âne",     Emoji="🫏", Hint="Porte les lourdes charges",         ImageFile="ane.jpg"    },
        new AnimalCard { RfidTag="04444A7AFB1990", Name="Canard", Emoji="🦆", Hint="Cancane près de la mare",      ImageFile="coq.jpg"    },
        new AnimalCard { RfidTag="0432486AD11990", Name="Chèvre", Emoji="🐐", Hint="Grimpe partout sans effort",   ImageFile="chèvre.jpg" },
        new AnimalCard { RfidTag="041213A28E1190", Name="Lapin",  Emoji="🐰", Hint="Saute et mange des carottes",  ImageFile="lapin.jpg"  },
        new AnimalCard { RfidTag="042D33D2FC1090", Name="Oie",    Emoji="🪿", Hint="Garde la ferme mieux qu'un chien", ImageFile="oie.jpg"},
        };

        // ══════════════════════════════════════════════════════
        //  Constructeur
        // ══════════════════════════════════════════════════════

        public MysteryAnimalPage()
        {
            InitializeComponent();
            _countdownTimer.Tick += CountdownTimer_Tick;
            _mediaPlayer.MediaEnded += (s, e) =>
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            };
            // ResetGame() est appelé via StartGame() après avoir défini CurrentDifficulty
        }

        /// <summary>À appeler depuis MainWindow APRÈS avoir défini CurrentDifficulty.</summary>
        /// 
        private SerialPort _rfidPort;
        public void StartGame()
        {
            ResetGame();
            StartRfidListener();

            System.Diagnostics.Debug.WriteLine($"[DIFFICULTÉ] {CurrentDifficulty}"); // ← ici
            string musique = CurrentDifficulty == Difficulty.Facile ? "SpongeBobSquarePants.mp3" :
                             CurrentDifficulty == Difficulty.Moyen ? "CoC.mp3" :
                             CurrentDifficulty == Difficulty.Difficile ? "SpongeBobSquarePants.mp3" : "";
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", musique);
            _mediaPlayer.Open(new Uri(path));
            _mediaPlayer.Volume = 0.5;
            _mediaPlayer.Play();

        }
        // ── Imports DLL ──────────────────────────────────────────
        [DllImport("MasterRD.dll")] static extern int rf_init_com(int port, int baud);
        [DllImport("MasterRD.dll")] static extern int rf_ClosePort();
        [DllImport("MasterRD.dll")] static extern int rf_antenna_sta(short icdev, byte mode);
        [DllImport("MasterRD.dll")] static extern int rf_init_type(short icdev, byte type);
        [DllImport("MasterRD.dll")] static extern int rf_request(short icdev, byte mode, ref ushort pTagType);
        [DllImport("MasterRD.dll")] static extern int rf_anticoll(short icdev, byte bcnt, IntPtr pSnr, ref byte pRLength);

        private Thread _rfidThread;
        private bool _rfidRunning;

        private void StartRfidListener()
        {
            _rfidRunning = true;
            _rfidThread = new Thread(() =>
            {
                while (_rfidRunning)
                {
                    if (_waitingForRfid)
                    {
                        string tag = LireCarteRfid(6, 19200); // COM6, 19200 baud
                        if (tag != null)
                            Dispatcher.Invoke(() => OnRfidTagRead(tag));
                    }
                    Thread.Sleep(300);
                }

            });
            _rfidThread.IsBackground = true;
            _rfidThread.Start();
        }

        private string LireCarteRfid(int port, int baud)
        {
            short icdev = 0x0000;
            byte type = (byte)'A';
            byte mode = 0x52;
            ushort tagType = 0;
            byte bcnt = 0x04;
            byte len = 255;
            int essais = 0;

            if (rf_init_com(port, baud) != 0) return null;

            IntPtr pSnr = Marshal.AllocHGlobal(1024);
            string cardId = null;

            try
            {
                do
                {
                    rf_antenna_sta(icdev, 0); Thread.Sleep(20);
                    rf_init_type(icdev, type); Thread.Sleep(20);
                    rf_antenna_sta(icdev, 1); Thread.Sleep(50);

                    if (rf_request(icdev, mode, ref tagType) == 0)
                    {
                        if (rf_anticoll(icdev, bcnt, pSnr, ref len) == 0)
                        {
                            cardId = "";
                            for (int i = 0; i < len; i++)
                                cardId += Marshal.ReadByte(pSnr, i).ToString("X2");
                            essais = 2;
                        }
                        else essais++;
                    }
                    else essais++;

                } while (essais < 2);
            }
            finally
            {
                Marshal.FreeHGlobal(pSnr);
                rf_ClosePort();
            }

            return cardId;
        }



        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (VisualParent == null)
            {
                _countdownTimer.Stop();
                _rfidRunning = false;
                _mediaPlayer.Stop();
            }
        }

        // ══════════════════════════════════════════════════════
        //  API publique RFID
        // ══════════════════════════════════════════════════════

        public void OnRfidTagRead(string tag)
        {
            if (!_waitingForRfid) return;

            var animal = _catalog.FirstOrDefault(a =>
                string.Equals(a.RfidTag, tag, StringComparison.OrdinalIgnoreCase));

            if (animal == null)
            {
                SetStatus($"⚠ Carte inconnue : {tag} — essaie une autre.");
                return;
            }

            LoadQuestion(animal);
        }

        // ══════════════════════════════════════════════════════
        //  Init / Reset
        // ══════════════════════════════════════════════════════

        private void ResetGame()
        {
            _lives = LivesForDifficulty();
            _score = 0;
            _round = 1;
            UpdateLivesDisplay();
            UpdateScoreDisplay();
            UpdateRoundDisplay();
            GoToWaiting();
        }

        /// <summary>
        /// Place l'écran en attente de carte RFID.
        /// </summary>
        private void GoToWaiting()
        {
            _countdownTimer.Stop();
            _waitingForRfid = true;
            ShowPanel("waiting");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus($"Round {_round}/{TotalRounds} — Approche ta carte animal !");
        }

        // ══════════════════════════════════════════════════════
        //  Question
        // ══════════════════════════════════════════════════════

        private void LoadQuestion(AnimalCard animal)
        {
            _waitingForRfid = false;
            _currentAnimal = animal;
            //AnimalImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/AnimalMystere/{animal.ImageFile}"));
            AnimalHint.Text = $"Indice : {animal.Hint}";
            RfidBadge.Text = $"🏷 RFID : {animal.RfidTag}";
            BuildAnswerButtons(animal);
            ShowPanel("question");
            TimerRow.Visibility = Visibility.Visible;
            StartCountdown();
            SetStatus($"Round {_round}/{TotalRounds} — Quel est cet animal ?");
        }

        // ══════════════════════════════════════════════════════
        //  Boutons réponse
        // ══════════════════════════════════════════════════════

        private void BuildAnswerButtons(AnimalCard correct)
        {
            var images = new Image[] { ImgA, ImgB, ImgC, ImgD };
            var rng = new Random();
            var choices = _catalog
                .Where(a => a.Name != correct.Name)
                .OrderBy(_ => rng.Next())
                .Take(3)
                .ToList();

            _correctIndex = rng.Next(4);
            choices.Insert(_correctIndex, correct);

            var btns = AnswerButtons();
            for (int i = 0; i < 4; i++)
            {
                var sp = (StackPanel)btns[i].Content;
                var tb = sp.Children.OfType<TextBlock>().First();
                var img = (Image)sp.Children[0];
                tb.Text = choices[i].Name;
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/AnimalMystere/{choices[i].ImageFile}"));
                SetButtonColor(btns[i], "#16213E", "#2A3F6F");
                btns[i].IsEnabled = true;
            }
        }

        /// <summary>
        /// Gère la sélection d'une réponse.
        /// </summary>
        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer.Stop();
            DisableAnswers();

            int chosen = int.Parse(((Button)sender).Tag.ToString());
            var btns = AnswerButtons();

            if (chosen == _correctIndex)
            {
                SetButtonColor(btns[chosen], "#1E8449", "#27AE60");
                _score++;
                UpdateScoreDisplay();
                SetStatus($"✅ Bravo ! C'est bien {_currentAnimal.Emoji} {_currentAnimal.Name} !");
            }
            else
            {
                SetButtonColor(btns[chosen], "#922B21", "#C0392B");
                SetButtonColor(btns[_correctIndex], "#1E8449", "#27AE60");
                _lives--;
                UpdateLivesDisplay();
                SetStatus($"❌ C'était {_currentAnimal.Emoji} {_currentAnimal.Name}. Plus que {_lives} vie(s).");
            }

            if (_lives <= 0)
                Delay(2.0, ShowGameOver);
            else
                Delay(1.8, NextRoundOrEnd);
        }

        // ══════════════════════════════════════════════════════
        //  Gestion des rounds
        // ══════════════════════════════════════════════════════

        private void NextRoundOrEnd()
        {
            if (_round >= TotalRounds)
            {
                // 3 rounds terminés → affiche résumé puis retour difficulté
                Delay(0.1, ShowRoundEnd);
            }
            else
            {
                _round++;
                UpdateRoundDisplay();
                GoToWaiting();
            }
        }

        /// <summary>
        /// Affiche l'écran de fin de partie en victoire.
        /// </summary>
        private void ShowRoundEnd()
        {
            // Affiche l'écran de fin avec le score final
            WinScore.Text = $"Score : {_score} / {TotalRounds} en {CurrentDifficulty}";
            ShowPanel("win");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus("Partie terminée !");
        }

        /// <summary>
        /// Met à jour l'affichage du round courant.
        /// </summary>
        private void UpdateRoundDisplay()
        {
            if (RoundLabel != null)
                RoundLabel.Text = $"Round {_round} / {TotalRounds}";
        }

        // ══════════════════════════════════════════════════════
        //  Timer
        // ══════════════════════════════════════════════════════

        private void StartCountdown()
        {
            _timeLeft = TimerSeconds;
            RefreshTimerBar();
            _countdownTimer.Start();
        }

        /// <summary>
        /// Décrémente le temps restant et gère le timeout.
        /// </summary>
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _timeLeft--;
            RefreshTimerBar();

            if (_timeLeft > 0) return;

            _countdownTimer.Stop();
            DisableAnswers();
            SetButtonColor(AnswerButtons()[_correctIndex], "#1E8449", "#27AE60");
            _lives--;
            UpdateLivesDisplay();
            SetStatus($"⏰ Temps écoulé ! C'était {_currentAnimal.Emoji} {_currentAnimal.Name}.");

            if (_lives <= 0)
                Delay(2.0, ShowGameOver);
            else
                Delay(1.8, NextRoundOrEnd);
        }

        /// <summary>
        /// Met à jour la barre et le texte du chrono.
        /// </summary>
        private void RefreshTimerBar()
        {
            TimerLabel.Text = $"{_timeLeft}s";
            double ratio = (double)_timeLeft / TimerSeconds;
            double maxWidth = TimerRow.ActualWidth > 0 ? TimerRow.ActualWidth - 120 : 500;
            TimerBar.Width = Math.Max(0, ratio * maxWidth);

            var color = ratio > 0.5
                ? Color.FromRgb(39, 174, 96)
                : ratio > 0.25
                    ? Color.FromRgb(241, 196, 15)
                    : Color.FromRgb(233, 69, 96);

            TimerBar.Background = new SolidColorBrush(color);
        }

        // ══════════════════════════════════════════════════════
        //  Fins de partie
        // ══════════════════════════════════════════════════════

        private void ShowGameOver()
        {
            _countdownTimer.Stop();
            GameOverScore.Text = $"Score : {_score} / {TotalRounds}";
            ShowPanel("gameover");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus("Game Over !");
        }

        // ── Bouton Rejouer → retourne à DifficultyPage ─────────
        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            RoundCompleted?.Invoke(); // MainWindow reviendra à DifficultyPage
        }

        // ── Bouton démo RFID ───────────────────────────────────
        private void SimulateRfid_Click(object sender, RoutedEventArgs e)
        {
            var tag = _catalog[new Random().Next(_catalog.Count)].RfidTag;
            OnRfidTagRead(tag);
        }

        // ══════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════

        private Button[] AnswerButtons() => new[] { BtnA, BtnB, BtnC, BtnD };

        /// <summary>
        /// Désactive les boutons de réponse.
        /// </summary>
        private void DisableAnswers()
        {
            foreach (var b in AnswerButtons()) b.IsEnabled = false;
        }

        /// <summary>
        /// Applique une couleur d'arrière-plan et de bordure à un bouton.
        /// </summary>
        private static void SetButtonColor(Button btn, string bg, string border)
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border));
        }

        /// <summary>
        /// Affiche le panneau demandé.
        /// </summary>
        private void ShowPanel(string panel)
        {
            WaitingScreen.Visibility = panel == "waiting" ? Visibility.Visible : Visibility.Collapsed;
            QuestionScreen.Visibility = panel == "question" ? Visibility.Visible : Visibility.Collapsed;
            GameOverScreen.Visibility = panel == "gameover" ? Visibility.Visible : Visibility.Collapsed;
            WinScreen.Visibility = panel == "win" ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Met à jour l'affichage des vies.
        /// </summary>
        private void UpdateLivesDisplay()
        {
            int max = LivesForDifficulty();
            // Le cœur disparaît quand la vie est perdue
            Heart1.Visibility = _lives >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Heart2.Visibility = (max >= 2 && _lives >= 2) ? Visibility.Visible : Visibility.Collapsed;
            Heart3.Visibility = (max >= 3 && _lives >= 3) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Met à jour l'affichage du score.
        /// </summary>
        private void UpdateScoreDisplay() =>
            ScoreLabel.Text = _score.ToString();

        /// <summary>
        /// Met à jour le texte de statut.
        /// </summary>
        private void SetStatus(string msg) =>
            StatusText.Text = msg;

        /// <summary>
        /// Exécute une action après un délai.
        /// </summary>
        private void Delay(double seconds, Action action)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            t.Tick += (s, e) => { t.Stop(); action(); };
            t.Start();
        }

    }
}