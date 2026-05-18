using FarmAnimalsGameV2.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace FarmAnimalsGameV2
{
    public partial class MainWindow : Window
    {
        private bool _isMemoryMode;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AnimalMystereButton_Click(object sender, RoutedEventArgs e)
        {
            _isMemoryMode = false;
            ShowDifficultyPage();
        }

        private void MemoryButton_Click(object sender, RoutedEventArgs e)
        {
            _isMemoryMode = true;
            ShowDifficultyPage();
        }

        private void ShowDifficultyPage()
        {
            var difficultyPage = new DifficultyPage();
            difficultyPage.BackRequested += DifficultyPage_BackRequested;
            difficultyPage.DifficultySelected += DifficultyPage_DifficultySelected;
            PageHost.Content = difficultyPage;
            MainMenu.Visibility = Visibility.Collapsed;
        }

        private void DifficultyPage_BackRequested(object? sender, RoutedEventArgs e)
        {
            PageHost.Content = null;
            MainMenu.Visibility = Visibility.Visible;
        }

        private void DifficultyPage_DifficultySelected(MysteryAnimalPage.Difficulty difficulty)
        {
            if (_isMemoryMode)
            {
                // Conversion MysteryAnimalPage.Difficulty → GameDifficulty
                var gameDifficulty = difficulty switch
                {
                    MysteryAnimalPage.Difficulty.Facile => GameDifficulty.Easy,
                    MysteryAnimalPage.Difficulty.Moyen => GameDifficulty.Medium,
                    MysteryAnimalPage.Difficulty.Difficile => GameDifficulty.Hard,
                    _ => GameDifficulty.Medium
                };
                PageHost.Content = new MemoryPage(gameDifficulty);
                return;
            }

            var page = new MysteryAnimalPage();
            page.CurrentDifficulty = difficulty;
            page.TimerSeconds = 30;
            page.RoundCompleted += ShowDifficultyPage;
            page.StartGame();
            PageHost.Content = page;
        }
    }
}
