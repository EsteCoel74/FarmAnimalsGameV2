using System.Windows;
using FarmAnimalsGameV2.Services;
using FarmAnimalsGameV2.Views;

namespace FarmAnimalsGameV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
            ShowMainMenu();
        }

        private void DifficultyPage_DifficultySelected(object? sender, GameDifficultySelectedEventArgs e)
        {
            if (_isMemoryMode)
            {
                var memoryPage = new MemoryPage(e.Difficulty);
                memoryPage.BackRequested += MemoryPage_BackRequested;
                PageHost.Content = memoryPage;
                RootGrid.Background = (System.Windows.Media.Brush)Resources["FarmBackground"];
                return;
            }

            PageHost.Content = new MysteryAnimalPage();
            RootGrid.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void MemoryPage_BackRequested(object? sender, RoutedEventArgs e)
        {
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            PageHost.Content = null;
            MainMenu.Visibility = Visibility.Visible;
            RootGrid.Background = (System.Windows.Media.Brush)Resources["FarmBackground"];
        }
    }
}