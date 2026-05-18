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
using FarmAnimalsGameV2;

namespace FarmAnimalsGameV2.Views
{
    /// <summary>
    /// Logique d'interaction pour MemoryPage.xaml
    /// </summary>
    public partial class MemoryPage : UserControl
    {
        private readonly DispatcherTimer _timer;
        private TimeSpan _remaining;
        private readonly Random _random = new();
        private readonly List<MemoryCard> _selectedCards = new();
        private bool _isChecking;

        public ObservableCollection<MemoryCard> Cards { get; } = new();

        public MemoryPage()
            : this(GameDifficulty.Medium)
        {
        }

        public MemoryPage(GameDifficulty difficulty)
        {
            InitializeComponent();

            DataContext = this;

            _remaining = GetDuration(difficulty);
            UpdateTimerText();

            InitializeCards();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            Loaded += MemoryPage_Loaded;
            Unloaded += MemoryPage_Unloaded;
        }

        private void MemoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        private void MemoryPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
                UpdateTimerText();
                _timer.Stop();
                return;
            }

            _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            TimerText.Text = _remaining.ToString(@"mm\:ss");
        }

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
                ("Pigeon", "Pigeonneau"),
                ("Autruche", "Autruchon")
            };

            var selectedPairs = pairs.OrderBy(_ => _random.Next()).Take(5).ToList();

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

            var first = _selectedCards[0];
            var second = _selectedCards[1];

            if (first.PairId == second.PairId)
            {
                first.IsMatched = true;
                second.IsMatched = true;
                if (Cards.All(card => card.IsMatched))
                {
                    _timer.Stop();
                }
            }
            else
            {
                await Task.Delay(900);
                first.IsRevealed = false;
                second.IsRevealed = false;
            }

            _selectedCards.Clear();
            _isChecking = false;
        }

        private static TimeSpan GetDuration(GameDifficulty difficulty)
        {
            return difficulty switch
            {
                GameDifficulty.Easy => TimeSpan.FromSeconds(90),
                GameDifficulty.Medium => TimeSpan.FromSeconds(60),
                GameDifficulty.Hard => TimeSpan.FromSeconds(45),
                _ => TimeSpan.FromSeconds(60)
            };
        }

        public sealed class MemoryCard : INotifyPropertyChanged
        {
            private bool _isRevealed;
            private bool _isMatched;

            public MemoryCard(string label, int pairId, string imageFileName)
            {
                Label = label;
                PairId = pairId;
                ImageFileName = imageFileName;
            }

            public string Label { get; }
            public int PairId { get; }
            public string ImageFileName { get; }
            public string ImagePath => $"pack://application:,,,/Assets/{ImageFileName}.png";

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

            public string DisplayText => IsRevealed || IsMatched ? Label : "?";

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}