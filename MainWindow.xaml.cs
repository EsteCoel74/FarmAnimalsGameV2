using System.Windows;
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
            PageHost.Content = null;
            MainMenu.Visibility = Visibility.Visible;
        }

        private void DifficultyPage_DifficultySelected(object? sender, GameDifficultySelectedEventArgs e)
        {
            if (_isMemoryMode)
            {
                PageHost.Content = new MemoryPage(e.Difficulty);
                return;
            }

            PageHost.Content = new MysteryAnimalPage();
        }
    }
}