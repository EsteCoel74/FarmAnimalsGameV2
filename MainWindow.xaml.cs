using System.Windows;
using FarmAnimalsGameV2.Views;

namespace FarmAnimalsGameV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AnimalMystereButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDifficultyPage();
        }

        private void MemoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDifficultyPage();
        }

        private void ShowDifficultyPage()
        {
            var difficultyPage = new DifficultyPage();
            difficultyPage.BackRequested += DifficultyPage_BackRequested;
            PageHost.Content = difficultyPage;
            MainMenu.Visibility = Visibility.Collapsed;
        }

        private void DifficultyPage_BackRequested(object? sender, RoutedEventArgs e)
        {
            PageHost.Content = null;
            MainMenu.Visibility = Visibility.Visible;
        }
    }
}