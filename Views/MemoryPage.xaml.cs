using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FarmAnimalsGameV2.Services;

namespace FarmAnimalsGameV2.Views
{
    /// <summary>
    /// Logique d'interaction pour MemoryPage.xaml
    /// </summary>
    public partial class MemoryPage : UserControl
    {
        public event RoutedEventHandler? BackRequested;
        private readonly DispatcherTimer _timer;
        private readonly TimeSpan _duration;
        private readonly GameDifficulty _difficulty;
        private TimeSpan _remaining;
        private readonly Random _random = new();
        private readonly List<MemoryCard> _selectedCards = new();
        private bool _isChecking;
        private int _attempts;
        private bool _isCompleted;
        private readonly List<StarBurst> _activeStars = new();
        private readonly Canvas? _starCanvas;
        private readonly DispatcherTimer _fireworksTimer;
        private int _fireworksTicks;
        private readonly DispatcherTimer _loseStarsTimer;
        private int _loseStarsTicks;

        public ObservableCollection<MemoryCard> Cards { get; } = new();

        public MemoryPage()
            : this(GameDifficulty.Medium)
        {
        }

        /// <summary>
        /// Initialise la page Memory avec la difficulté choisie.
        /// </summary>
        public MemoryPage(GameDifficulty difficulty)
        {
            InitializeComponent();

            _starCanvas = FindName("StarCanvas") as Canvas;

            DataContext = this;

            _difficulty = difficulty;
            _duration = GetDuration(difficulty);
            _remaining = _duration;
            UpdateTimerText();

            InitializeCards();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            Loaded += MemoryPage_Loaded;
            Unloaded += MemoryPage_Unloaded;

            _fireworksTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _fireworksTimer.Tick += FireworksTimer_Tick;

            _loseStarsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(280) };
            _loseStarsTimer.Tick += LoseStarsTimer_Tick;
        }

        /// <summary>
        /// Démarre le timer quand la page est chargée.
        /// </summary>
        private void MemoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Arrête les timers et les animations quand la page est déchargée.
        /// </summary>
        private void MemoryPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }

            StopFireworks();
            StopLoseStars();
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        /// <summary>
        /// Met à jour le compte à rebours à chaque tick.
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
                UpdateTimerText();
                _timer.Stop();
                ShowSummary(isWinner: false);
                return;
            }

            _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
            UpdateTimerText();
        }

        /// <summary>
        /// Met à jour l'affichage du chronomètre.
        /// </summary>
        private void UpdateTimerText()
        {
            TimerText.Text = _remaining.ToString(@"mm\:ss");
        }

        /// <summary>
        /// Génère et mélange les cartes du memory.
        /// </summary>
        private void InitializeCards()
        {
            Cards.Clear();

            var pairs = new List<(string Parent, string Baby)>
            {
                ("Vache", "Veau"),
                ("Mouton", "Agneau"),
                ("Chèvre", "Chevreau"),
                ("Cochon", "Porcelet"),
                ("Cheval", "Poulain"),
                ("Âne", "Ânon"),
                ("Lapin", "Lapereau"),
                ("Canard", "Caneton"),
                ("Oie", "Oison"),
                ("Poule", "Poussin"),
                ("Dinde", "Dindonneau"),
                ("Chien", "Chiot"),
                ("Chat", "Chaton"),
                ("Buffle", "Bufflon"),
                ("Pintade", "Pintadeau"),
            };

            var selectedPairs = pairs.OrderBy(_ => _random.Next()).Take(10).ToList();

            for (var i = 0; i < selectedPairs.Count; i++)
            {
                var pair = selectedPairs[i];
                Cards.Add(new MemoryCard(pair.Parent, i, NormalizeFileName(pair.Parent)));
                Cards.Add(new MemoryCard(pair.Baby, i, NormalizeFileName(pair.Baby)));
            }

            var shuffled = Cards.OrderBy(_ => _random.Next()).ToList();
            Cards.Clear();
            foreach (var card in shuffled)
            {
                Cards.Add(card);
            }
        }

        /// <summary>
        /// Normalise un libellé pour en faire un nom de fichier d'image.
        /// </summary>
        private static string NormalizeFileName(string label)
        {
            var normalized = label.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gère la sélection d'une carte et la logique de comparaison.
        /// </summary>
        private async void CardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isChecking)
            {
                return;
            }

            if (sender is not Button button || button.DataContext is not MemoryCard card)
            {
                return;
            }

            if (card.IsMatched || card.IsRevealed)
            {
                return;
            }

            card.IsRevealed = true;
            _selectedCards.Add(card);

            if (_selectedCards.Count < 2)
            {
                return;
            }

            _isChecking = true;
            _attempts++;

            var first = _selectedCards[0];
            var second = _selectedCards[1];

            if (first.PairId == second.PairId)
            {
                TriggerStarBurst();
                first.IsMatched = true;
                second.IsMatched = true;
                await Task.Delay(2000);
                first.IsCleared = true;
                second.IsCleared = true;
                if (Cards.All(card => card.IsCleared))
                {
                    _timer.Stop();
                    ShowSummary(isWinner: true);
                }
            }
            else
            {
                ApplyMistakePenalty();
                await Task.Delay(900);
                first.IsRevealed = false;
                second.IsRevealed = false;
            }

            _selectedCards.Clear();
            _isChecking = false;
        }

        /// <summary>
        /// Affiche le panneau de résumé (victoire ou défaite).
        /// </summary>
        private void ShowSummary(bool isWinner)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            if (isWinner)
            {
                StartFireworks();
                StopLoseStars();
            }
            else
            {
                StopFireworks();
                StartLoseStars();
            }
            SummaryTitleText.Text = isWinner ? "Bravo !" : "Partie perdue";
            SummarySubtitleText.Text = isWinner ? "Toutes les paires sont trouvées" : "Nombre d'essais atteint";
            var elapsed = _duration - _remaining;
            AttemptsText.Text = $"Tentatives : {_attempts}";
            TimeText.Text = $"Temps : {elapsed.ToString(@"mm\:ss")}";
            SummaryPanel.Visibility = Visibility.Visible;
            CardsBoard.IsEnabled = false;
        }

        /// <summary>
        /// Réinitialise une partie de memory.
        /// </summary>
        private void ResetGame()
        {
            _attempts = 0;
            _isCompleted = false;
            _selectedCards.Clear();
            _isChecking = false;
            _remaining = _duration;
            SummaryTitleText.Text = "Bravo !";
            SummarySubtitleText.Text = "Toutes les paires sont trouvées";
            UpdateTimerText();
            InitializeCards();
            SummaryPanel.Visibility = Visibility.Collapsed;
            CardsBoard.IsEnabled = true;
            _starCanvas?.Children.Clear();
            _activeStars.Clear();
            StopFireworks();
            StopLoseStars();
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Notifie le retour au menu.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, e);
        }

        /// <summary>
        /// Relance une nouvelle partie.
        /// </summary>
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        /// <summary>
        /// Retourne la durée de base selon la difficulté.
        /// </summary>
        private static TimeSpan GetDuration(GameDifficulty difficulty)
        {
            return difficulty switch
            {
                GameDifficulty.Easy => TimeSpan.FromSeconds(120),
                GameDifficulty.Medium => TimeSpan.FromSeconds(90),
                GameDifficulty.Hard => TimeSpan.FromSeconds(75),
                _ => TimeSpan.FromSeconds(90)
            };
        }

        /// <summary>
        /// Applique une pénalité de temps après une erreur.
        /// </summary>
        private void ApplyMistakePenalty()
        {
            var penalty = GetMistakePenalty(_difficulty);
            _remaining = _remaining > penalty ? _remaining - penalty : TimeSpan.Zero;
            UpdateTimerText();

            if (_remaining == TimeSpan.Zero)
            {
                _timer.Stop();
                ShowSummary(isWinner: false);
            }
        }

        /// <summary>
        /// Retourne la pénalité de temps selon la difficulté.
        /// </summary>
        private static TimeSpan GetMistakePenalty(GameDifficulty difficulty)
        {
            return difficulty switch
            {
                GameDifficulty.Easy => TimeSpan.FromSeconds(7),
                GameDifficulty.Medium => TimeSpan.FromSeconds(4),
                GameDifficulty.Hard => TimeSpan.FromSeconds(2),
                _ => TimeSpan.FromSeconds(2)
            };
        }

        /// <summary>
        /// Lance l'animation d'étoiles lors d'une paire trouvée.
        /// </summary>
        private void TriggerStarBurst()
        {
            if (_starCanvas is null || _starCanvas.ActualWidth <= 0 || _starCanvas.ActualHeight <= 0)
            {
                return;
            }

            const int starCount = 18;
            for (var i = 0; i < starCount; i++)
            {
                var angle = 2 * Math.PI * i / starCount;
                var velocity = 3.0 + _random.NextDouble() * 2.5;
                var startX = _random.NextDouble() * _starCanvas.ActualWidth;
                var startY = _random.NextDouble() * _starCanvas.ActualHeight;
                var star = new StarBurst
                {
                    Position = new Vector(startX, startY),
                    Velocity = new Vector(Math.Cos(angle) * velocity, Math.Sin(angle) * velocity),
                    Life = 40 + _random.Next(10),
                    Shape = CreateStarShape()
                };
                star.InitialLife = star.Life;
                _activeStars.Add(star);
                _starCanvas.Children.Add(star.Shape);
                Canvas.SetLeft(star.Shape, startX);
                Canvas.SetTop(star.Shape, startY);
            }

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// Anime les particules d'étoiles à chaque frame.
        /// </summary>
        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (_activeStars.Count == 0)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                return;
            }

            if (_starCanvas is null)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                return;
            }

            for (var i = _activeStars.Count - 1; i >= 0; i--)
            {
                var star = _activeStars[i];
                star.Life -= 1;
                if (star.IsFalling)
                {
                    star.Velocity = new Vector(star.Velocity.X, star.Velocity.Y + 1.2);
                }
                else
                {
                    star.Velocity *= 0.95;
                }

                star.Position += star.Velocity;
                Canvas.SetLeft(star.Shape, star.Position.X);
                Canvas.SetTop(star.Shape, star.Position.Y);
                var opacityBase = star.Life / 40.0;
                star.Shape.Opacity = Math.Max(0, Math.Min(1, opacityBase));

                if (star.FadeToGray && star.FillBrush is not null && star.InitialLife > 0)
                {
                    var t = 1 - Math.Max(0, Math.Min(1, star.Life / star.InitialLife));
                    star.FillBrush.Color = LerpColor(Color.FromRgb(255, 214, 64), Color.FromRgb(160, 160, 160), t);
                }

                var isOffscreen = star.Position.Y > _starCanvas.ActualHeight + 40;
                if (star.Life <= 0 || isOffscreen)
                {
                    _starCanvas.Children.Remove(star.Shape);
                    _activeStars.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Crée une étoile jaune par défaut.
        /// </summary>
        private Shape CreateStarShape()
        {
            return CreateStarShape(Color.FromRgb(255, 214, 64));
        }

        /// <summary>
        /// Crée une étoile avec une couleur spécifique.
        /// </summary>
        private Shape CreateStarShape(Color color)
        {
            return CreateStarShape(color, out _);
        }

        /// <summary>
        /// Crée une étoile et expose son pinceau de remplissage.
        /// </summary>
        private Shape CreateStarShape(Color color, out SolidColorBrush brush)
        {
            var size = 14 + _random.Next(6);
            var points = new PointCollection
            {
                new Point(0.5, 0),
                new Point(0.62, 0.38),
                new Point(1, 0.38),
                new Point(0.7, 0.62),
                new Point(0.8, 1),
                new Point(0.5, 0.75),
                new Point(0.2, 1),
                new Point(0.3, 0.62),
                new Point(0, 0.38),
                new Point(0.38, 0.38)
            };

            brush = new SolidColorBrush(color);
            return new Polygon
            {
                Points = points,
                Fill = brush,
                Width = size,
                Height = size,
                RenderTransform = new ScaleTransform(size, size)
            };
        }

        /// <summary>
        /// Interpole deux couleurs pour l'animation de défaite.
        /// </summary>
        private static Color LerpColor(Color start, Color end, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            var r = (byte)Math.Round(start.R + (end.R - start.R) * t);
            var g = (byte)Math.Round(start.G + (end.G - start.G) * t);
            var b = (byte)Math.Round(start.B + (end.B - start.B) * t);
            return Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Démarre les feux d'artifice de victoire.
        /// </summary>
        private void StartFireworks()
        {
            if (_fireworksTimer.IsEnabled)
            {
                return;
            }

            _fireworksTicks = 0;
            _fireworksTimer.Start();
        }

        /// <summary>
        /// Stoppe l'animation de feux d'artifice.
        /// </summary>
        private void StopFireworks()
        {
            if (_fireworksTimer.IsEnabled)
            {
                _fireworksTimer.Stop();
            }
        }

        /// <summary>
        /// Démarre l'animation de défaite.
        /// </summary>
        private void StartLoseStars()
        {
            if (_loseStarsTimer.IsEnabled)
            {
                return;
            }

            _loseStarsTicks = 0;
            _loseStarsTimer.Start();
        }

        /// <summary>
        /// Stoppe l'animation de défaite.
        /// </summary>
        private void StopLoseStars()
        {
            if (_loseStarsTimer.IsEnabled)
            {
                _loseStarsTimer.Stop();
            }
        }

        /// <summary>
        /// Anime les étoiles de défaite à intervalle régulier.
        /// </summary>
        private void LoseStarsTimer_Tick(object? sender, EventArgs e)
        {
            if (_loseStarsTicks >= 12)
            {
                StopLoseStars();
                return;
            }

            TriggerLoseStarFall();
            _loseStarsTicks++;
        }

        /// <summary>
        /// Génère des étoiles qui tombent lors d'une défaite.
        /// </summary>
        private void TriggerLoseStarFall()
        {
            if (_starCanvas is null || _starCanvas.ActualWidth <= 0 || _starCanvas.ActualHeight <= 0)
            {
                return;
            }

            var starCount = 12 + _random.Next(8);
            for (var i = 0; i < starCount; i++)
            {
                var startX = _random.NextDouble() * _starCanvas.ActualWidth;
                var startY = -20 - _random.NextDouble() * 40;
                var velocity = 3.5 + _random.NextDouble() * 2.5;
                var star = new StarBurst
                {
                    Position = new Vector(startX, startY),
                    Velocity = new Vector(0.6 - _random.NextDouble() * 1.2, velocity),
                    Life = 160 + _random.Next(40),
                    Shape = CreateStarShape(Color.FromRgb(160, 160, 160))
                };
                _activeStars.Add(star);
                _starCanvas.Children.Add(star.Shape);
                Canvas.SetLeft(star.Shape, startX);
                Canvas.SetTop(star.Shape, startY);
            }

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// Déclenche une rafale de feux d'artifice.
        /// </summary>
        private void FireworksTimer_Tick(object? sender, EventArgs e)
        {
            if (_fireworksTicks >= 10)
            {
                StopFireworks();
                return;
            }

            TriggerFireworkBurst();
            _fireworksTicks++;
        }

        /// <summary>
        /// Génère des particules de feux d'artifice.
        /// </summary>
        private void TriggerFireworkBurst()
        {
            if (_starCanvas is null || _starCanvas.ActualWidth <= 0 || _starCanvas.ActualHeight <= 0)
            {
                return;
            }

            var centerX = _random.NextDouble() * _starCanvas.ActualWidth;
            var centerY = _random.NextDouble() * _starCanvas.ActualHeight * 0.6;
            var particleCount = 28 + _random.Next(10);
            var hue = _random.Next(0, 360);

            for (var i = 0; i < particleCount; i++)
            {
                var angle = 2 * Math.PI * i / particleCount;
                var velocity = 4.0 + _random.NextDouble() * 3.5;
                var particle = new StarBurst
                {
                    Position = new Vector(centerX, centerY),
                    Velocity = new Vector(Math.Cos(angle) * velocity, Math.Sin(angle) * velocity),
                    Life = 50 + _random.Next(15),
                    Shape = CreateFireworkParticle(hue),
                    IsFalling = false
                };
                particle.InitialLife = particle.Life;
                _activeStars.Add(particle);
                _starCanvas.Children.Add(particle.Shape);
                Canvas.SetLeft(particle.Shape, centerX);
                Canvas.SetTop(particle.Shape, centerY);
            }

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// Crée une particule de feu d'artifice colorée.
        /// </summary>
        private Shape CreateFireworkParticle(int baseHue)
        {
            var size = 8 + _random.Next(6);
            var hue = (baseHue + _random.Next(-40, 40) + 360) % 360;
            var color = ColorFromHsv(hue, 0.85, 1.0);
            return new Ellipse
            {
                Fill = new SolidColorBrush(color),
                Width = size,
                Height = size
            };
        }

        /// <summary>
        /// Convertit une couleur HSV en RGB.
        /// </summary>
        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            var chroma = value * saturation;
            var x = chroma * (1 - Math.Abs((hue / 60.0 % 2) - 1));
            var m = value - chroma;
            double rPrime;
            double gPrime;
            double bPrime;

            if (hue < 60)
            {
                rPrime = chroma;
                gPrime = x;
                bPrime = 0;
            }
            else if (hue < 120)
            {
                rPrime = x;
                gPrime = chroma;
                bPrime = 0;
            }
            else if (hue < 180)
            {
                rPrime = 0;
                gPrime = chroma;
                bPrime = x;
            }
            else if (hue < 240)
            {
                rPrime = 0;
                gPrime = x;
                bPrime = chroma;
            }
            else if (hue < 300)
            {
                rPrime = x;
                gPrime = 0;
                bPrime = chroma;
            }
            else
            {
                rPrime = chroma;
                gPrime = 0;
                bPrime = x;
            }

            var r = (byte)Math.Round((rPrime + m) * 255);
            var g = (byte)Math.Round((gPrime + m) * 255);
            var b = (byte)Math.Round((bPrime + m) * 255);
            return Color.FromRgb(r, g, b);
        }

        private sealed class StarBurst
        {
            public Vector Position { get; set; }
            public Vector Velocity { get; set; }
            public double Life { get; set; }
            public double InitialLife { get; set; }
            public Shape Shape { get; set; } = null!;
            public SolidColorBrush? FillBrush { get; set; }
            public bool IsFalling { get; set; }
            public bool FadeToGray { get; set; }
        }

        public sealed class MemoryCard : INotifyPropertyChanged
        {
            private bool _isRevealed;
            private bool _isMatched;
            private bool _isCleared;

            /// <summary>
            /// Initialise une carte de memory.
            /// </summary>
            public MemoryCard(string label, int pairId, string imageFileName)
            {
                Label = label;
                PairId = pairId;
                ImageFileName = imageFileName;
            }

            public string Label { get; }
            public int PairId { get; }
            public string ImageFileName { get; }
            public string ImagePath => $"pack://application:,,,/Assets/Memory/{ImageFileName}.png";

            public bool IsRevealed
            {
                get => _isRevealed;
                set
                {
                    if (_isRevealed == value)
                    {
                        return;
                    }

                    _isRevealed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }

            public bool IsMatched
            {
                get => _isMatched;
                set
                {
                    if (_isMatched == value)
                    {
                        return;
                    }

                    _isMatched = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }

            public bool IsCleared
            {
                get => _isCleared;
                set
                {
                    if (_isCleared == value)
                    {
                        return;
                    }

                    _isCleared = value;
                    OnPropertyChanged();
                }
            }

            public string DisplayText => IsRevealed || IsMatched ? Label : "?";

            public event PropertyChangedEventHandler? PropertyChanged;

            /// <summary>
            /// Notifie la mise à jour d'une propriété liée.
            /// </summary>
            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
