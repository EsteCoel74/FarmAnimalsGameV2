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

namespace FarmAnimalsGameV2.Views
{
    public class AnimalCard
    {
        public string RfidTag { get; set; }
        public string Name { get; set; }
        public string Emoji { get; set; }
        public string Hint { get; set; }
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

        private readonly DispatcherTimer _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private readonly List<AnimalCard> _catalog = new List<AnimalCard>
        {
            new AnimalCard { RfidTag="04:AA:01:01", Name="Vache",    Emoji="🐄", Hint="Donne du lait chaque matin"        },
            new AnimalCard { RfidTag="04:AA:01:02", Name="Cochon",   Emoji="🐷", Hint="Se roule dans la boue"             },
            new AnimalCard { RfidTag="04:AA:01:03", Name="Poulet",   Emoji="🐔", Hint="Pond des œufs tous les jours"      },
            new AnimalCard { RfidTag="04:AA:01:04", Name="Mouton",   Emoji="🐑", Hint="Sa laine tient chaud l'hiver"      },
            new AnimalCard { RfidTag="04:AA:01:05", Name="Cheval",   Emoji="🐴", Hint="Court très vite dans les prés"     },
            new AnimalCard { RfidTag="04:AA:01:06", Name="Âne",      Emoji="🫏", Hint="Porte les lourdes charges"         },
            new AnimalCard { RfidTag="04:AA:01:07", Name="Canard",   Emoji="🦆", Hint="Cancane près de la mare"           },
            new AnimalCard { RfidTag="04:AA:01:08", Name="Chèvre",   Emoji="🐐", Hint="Grimpe partout sans effort"        },
            new AnimalCard { RfidTag="04:AA:01:09", Name="Lapin",    Emoji="🐰", Hint="Saute et mange des carottes"       },
            new AnimalCard { RfidTag="04:AA:01:10", Name="Oie",      Emoji="🪿", Hint="Garde la ferme mieux qu'un chien" },
        };

        // ══════════════════════════════════════════════════════
        //  Constructeur
        // ══════════════════════════════════════════════════════

        public MysteryAnimalPage()
        {
            InitializeComponent();
            _countdownTimer.Tick += CountdownTimer_Tick;
            // ResetGame() est appelé via StartGame() après avoir défini CurrentDifficulty
        }

        /// <summary>À appeler depuis MainWindow APRÈS avoir défini CurrentDifficulty.</summary>
        public void StartGame()
        {
            ResetGame();
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
            AnimalEmoji.Text = animal.Emoji;
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
            var rng = new Random();
            var choices = _catalog
                .Where(a => a.Name != correct.Name)
                .OrderBy(_ => rng.Next())
                .Take(3)
                .Select(a => $"{a.Emoji}  {a.Name}")
                .ToList();

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

        private void ShowRoundEnd()
        {
            // Affiche l'écran de fin avec le score final
            WinScore.Text = $"Score : {_score} / {TotalRounds} en {CurrentDifficulty}";
            ShowPanel("win");
            TimerRow.Visibility = Visibility.Collapsed;
            SetStatus("Partie terminée !");
        }

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
            int max = LivesForDifficulty();
            // Le cœur disparaît quand la vie est perdue
            Heart1.Visibility = _lives >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Heart2.Visibility = (max >= 2 && _lives >= 2) ? Visibility.Visible : Visibility.Collapsed;
            Heart3.Visibility = (max >= 3 && _lives >= 3) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateScoreDisplay() =>
            ScoreLabel.Text = _score.ToString();

        private void SetStatus(string msg) =>
            StatusText.Text = msg;

        private void Delay(double seconds, Action action)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            t.Tick += (s, e) => { t.Stop(); action(); };
            t.Start();
        }
    }
}