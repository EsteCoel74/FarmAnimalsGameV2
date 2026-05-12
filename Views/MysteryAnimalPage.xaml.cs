using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FarmAnimalsGameV2.Views
{
    // ══════════════════════════════════════════════════════════
    //  Modèle Animal
    // ══════════════════════════════════════════════════════════

    /// <summary>Associe un tag RFID à un animal de la ferme.</summary>
    public class AnimalCard
    {
        /// <summary>Tag brut lu par le lecteur (ex. "04:AB:12:34").</summary>
        public string RfidTag { get; set; }

        /// <summary>Nom affiché dans les boutons de réponse.</summary>
        public string Name { get; set; }

        /// <summary>Emoji représentant l'animal (affiché en grand).</summary>
        public string Emoji { get; set; }

        /// <summary>Courte phrase d'indice affichée sous l'emoji.</summary>
        public string Hint { get; set; }
    }

    // ══════════════════════════════════════════════════════════
    //  Code-behind MysteryAnimalPage
    // ══════════════════════════════════════════════════════════

    public partial class MysteryAnimalPage : UserControl
    {
        // ── Paramètre configurable ─────────────────────────────
        /// <summary>Durée accordée pour répondre (secondes).</summary>
        public int TimerSeconds { get; set; } = 30;

        // ── Constantes ─────────────────────────────────────────
        private const int MaxLives = 3;

        // ── État de la partie ──────────────────────────────────
        private int _lives;
        private int _score;
        private int _timeLeft;
        private AnimalCard _currentAnimal;
        private int _correctIndex;
        private bool _waitingForRfid;

        // ── Timer ──────────────────────────────────────────────
        private readonly DispatcherTimer _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        // ── Catalogue des animaux ──────────────────────────────
        // ⚠ Remplace les RfidTag par les vraies valeurs de tes cartes.
        private readonly List<AnimalCard> _catalog = new List<AnimalCard>
        {
            new AnimalCard { RfidTag="04:AA:01:01", Name="Vache",     Emoji="🐄", Hint="Donne du lait chaque matin"          },
            new AnimalCard { RfidTag="04:AA:01:02", Name="Cochon",    Emoji="🐷", Hint="Se roule dans la boue"               },
            new AnimalCard { RfidTag="04:AA:01:03", Name="Poulet",    Emoji="🐔", Hint="Pond des œufs tous les jours"        },
            new AnimalCard { RfidTag="04:AA:01:04", Name="Mouton",    Emoji="🐑", Hint="Sa laine tient chaud l'hiver"        },
            new AnimalCard { RfidTag="04:AA:01:05", Name="Cheval",    Emoji="🐴", Hint="Court très vite dans les prés"       },
            new AnimalCard { RfidTag="04:AA:01:06", Name="Âne",       Emoji="🫏", Hint="Porte les lourdes charges"           },
            new AnimalCard { RfidTag="04:AA:01:07", Name="Canard",    Emoji="🦆", Hint="Cancane près de la mare"             },
            new AnimalCard { RfidTag="04:AA:01:08", Name="Chèvre",    Emoji="🐐", Hint="Grimpe partout sans effort"          },
            new AnimalCard { RfidTag="04:AA:01:09", Name="Lapin",     Emoji="🐰", Hint="Saute et mange des carottes"         },
            new AnimalCard { RfidTag="04:AA:01:10", Name="Oie",       Emoji="🪿", Hint="Garde la ferme mieux qu'un chien"   },
        };

        // ══════════════════════════════════════════════════════════
        //  Constructeur
        // ══════════════════════════════════════════════════════════

        public MysteryAnimalPage()
        {
            InitializeComponent();
            _countdownTimer.Tick += CountdownTimer_Tick;
            ResetGame();
        }

        // ══════════════════════════════════════════════════════════
        //  API PUBLIQUE — appelée depuis l'extérieur (lecteur RFID)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Appelle cette méthode dès qu'un tag RFID est lu par ton lecteur.
        /// Elle doit être appelée depuis le thread UI ou via Dispatcher.Invoke.
        /// </summary>
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

        // ══════════════════════════════════════════════════════════
        //  Initialisation / réinitialisation
        // ══════════════════════════════════════════════════════════

        private void ResetGame()
        {
            _lives = MaxLives;
            _score = 0;
            UpdateLivesDisplay();
            UpdateScoreDisplay();
            GoToWaiting();
        }

        private void GoToWaiting()
        {
            _countdownTimer.Stop();
            _waitingForRfid = true;

            ShowPanel("waiting");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus("En attente d'une carte RFID…");
        }

        // ══════════════════════════════════════════════════════════
        //  Chargement d'une question
        // ══════════════════════════════════════════════════════════

        private void LoadQuestion(AnimalCard animal)
        {
            _waitingForRfid = false;
            _currentAnimal = animal;

            // Affichage carte
            AnimalEmoji.Text = animal.Emoji;
            AnimalHint.Text = $"Indice : {animal.Hint}";
            RfidBadge.Text = $"🏷 RFID : {animal.RfidTag}";

            // Construction des 4 propositions
            BuildAnswerButtons(animal);

            ShowPanel("question");
            TimerRow.Visibility = Visibility.Visible;
            StartCountdown();
            SetStatus($"Animal détecté ! À toi de deviner…");
        }

        // ══════════════════════════════════════════════════════════
        //  Boutons de réponse
        // ══════════════════════════════════════════════════════════

        private void BuildAnswerButtons(AnimalCard correct)
        {
            var rng = new Random();

            // 3 mauvaises réponses
            var choices = _catalog
                .Where(a => a.Name != correct.Name)
                .OrderBy(_ => rng.Next())
                .Take(3)
                .Select(a => $"{a.Emoji}  {a.Name}")
                .ToList();

            // Position aléatoire de la bonne réponse
            _correctIndex = rng.Next(4);
            choices.Insert(_correctIndex, $"{correct.Emoji}  {correct.Name}");

            var btns = AnswerButtons();
            for (int i = 0; i < 4; i++)
            {
                var tb = (TextBlock)btns[i].Content;
                tb.Text = choices[i];
                SetButtonColor(btns[i], "#16213E", "#2A3F6F");
                btns[i].IsEnabled = true;
            }
        }

        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer.Stop();
            DisableAnswers();

            int chosen = int.Parse(((Button)sender).Tag.ToString());
            var btns = AnswerButtons();

            if (chosen == _correctIndex)
            {
                SetButtonColor(btns[chosen], "#1E8449", "#27AE60");  // vert
                _score++;
                UpdateScoreDisplay();
                SetStatus($"✅ Bravo ! C'est bien {_currentAnimal.Emoji} {_currentAnimal.Name} !");
            }
            else
            {
                SetButtonColor(btns[chosen], "#922B21", "#C0392B");     // rouge
                SetButtonColor(btns[_correctIndex], "#1E8449", "#27AE60"); // révèle
                _lives--;
                UpdateLivesDisplay();
                SetStatus($"❌ C'était {_currentAnimal.Emoji} {_currentAnimal.Name}. Plus que {_lives} vie(s).");
            }

            if (_lives <= 0)
                Delay(2.0, ShowGameOver);
            else if (_score >= _catalog.Count)
                Delay(2.0, ShowWin);
            else
                Delay(1.8, GoToWaiting);
        }

        // ══════════════════════════════════════════════════════════
        //  Timer countdown
        // ══════════════════════════════════════════════════════════

        private void StartCountdown()
        {
            _timeLeft = TimerSeconds;
            RefreshTimerBar();
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _timeLeft--;
            RefreshTimerBar();

            if (_timeLeft > 0) return;

            _countdownTimer.Stop();
            DisableAnswers();

            // Révèle la bonne réponse
            SetButtonColor(AnswerButtons()[_correctIndex], "#1E8449", "#27AE60");

            _lives--;
            UpdateLivesDisplay();
            SetStatus($"⏰ Temps écoulé ! C'était {_currentAnimal.Emoji} {_currentAnimal.Name}.");

            if (_lives <= 0)
                Delay(2.0, ShowGameOver);
            else
                Delay(1.8, GoToWaiting);
        }

        private void RefreshTimerBar()
        {
            TimerLabel.Text = $"{_timeLeft}s";

            double ratio = (double)_timeLeft / TimerSeconds;
            double maxWidth = TimerRow.ActualWidth > 0
                ? TimerRow.ActualWidth - 120   // soustrait icône + label
                : 500;
            TimerBar.Width = Math.Max(0, ratio * maxWidth);

            // Couleur selon urgence
            var color = ratio > 0.5
                ? Color.FromRgb(39, 174, 96)    // vert
                : ratio > 0.25
                    ? Color.FromRgb(241, 196, 15)  // jaune
                    : Color.FromRgb(233, 69, 96);  // rouge

            TimerBar.Background = new SolidColorBrush(color);
        }

        // ══════════════════════════════════════════════════════════
        //  Fins de partie
        // ══════════════════════════════════════════════════════════

        private void ShowGameOver()
        {
            _countdownTimer.Stop();
            GameOverScore.Text = $"Score final : {_score} / {_catalog.Count}";
            ShowPanel("gameover");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus("Partie terminée.");
        }

        private void ShowWin()
        {
            _countdownTimer.Stop();
            WinScore.Text = $"Score parfait : {_score} / {_catalog.Count} 🎉";
            ShowPanel("win");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus("Félicitations !");
        }

        // ══════════════════════════════════════════════════════════
        //  Événements boutons XAML
        // ══════════════════════════════════════════════════════════

        /// <summary>Bouton de démo — simule une lecture RFID aléatoire.</summary>
        private void SimulateRfid_Click(object sender, RoutedEventArgs e)
        {
            var tag = _catalog[new Random().Next(_catalog.Count)].RfidTag;
            OnRfidTagRead(tag);
        }

        private void Replay_Click(object sender, RoutedEventArgs e) => ResetGame();

        // ══════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════

        private Button[] AnswerButtons() => new[] { BtnA, BtnB, BtnC, BtnD };

        private void DisableAnswers()
        {
            foreach (var b in AnswerButtons()) b.IsEnabled = false;
        }

        private static void SetButtonColor(Button btn, string bg, string border)
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border));
        }

        private void ShowPanel(string panel)
        {
            WaitingScreen.Visibility = panel == "waiting" ? Visibility.Visible : Visibility.Collapsed;
            QuestionScreen.Visibility = panel == "question" ? Visibility.Visible : Visibility.Collapsed;
            GameOverScreen.Visibility = panel == "gameover" ? Visibility.Visible : Visibility.Collapsed;
            WinScreen.Visibility = panel == "win" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateLivesDisplay()
        {
            Heart1.Text = _lives >= 1 ? "❤️" : "🖤";
            Heart2.Text = _lives >= 2 ? "❤️" : "🖤";
            Heart3.Text = _lives >= 3 ? "❤️" : "🖤";
        }

        private void UpdateScoreDisplay() =>
            ScoreLabel.Text = _score.ToString();

        private void SetStatus(string msg) =>
            StatusText.Text = msg;

        /// <summary>Exécute une action après un délai (sans bloquer l'UI).</summary>
        private void Delay(double seconds, Action action)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            t.Tick += (s, e) => { t.Stop(); action(); };
            t.Start();
        }
    }
}
