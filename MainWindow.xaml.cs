using System.Windows;
using System.Windows.Controls;
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
        private readonly MediaElement? _backgroundMusic;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private ResizeMode _previousResizeMode;
        private bool _isFullScreen;

        public MainWindow()
        {
            InitializeComponent();
            _backgroundMusic = FindName("BackgroundMusic") as MediaElement;
            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
        }

        /// <summary>
        /// Démarre la musique globale quand la fenêtre est chargée.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_backgroundMusic is null)
            {
                return;
            }

            _backgroundMusic.MediaEnded += BackgroundMusic_MediaEnded;
            _backgroundMusic.Position = TimeSpan.Zero;
            _backgroundMusic.Play();
        }

        /// <summary>
        /// Arrête la musique et libère les événements lors du déchargement.
        /// </summary>
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_backgroundMusic is null)
            {
                return;
            }

            _backgroundMusic.MediaEnded -= BackgroundMusic_MediaEnded;
            _backgroundMusic.Stop();
        }

        /// <summary>
        /// Relance la musique lorsqu'elle se termine.
        /// </summary>
        private void BackgroundMusic_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_backgroundMusic is null)
            {
                return;
            }

            _backgroundMusic.Position = TimeSpan.Zero;
            _backgroundMusic.Play();
        }

        /// <summary>
        /// Bascule vers la sélection de difficulté pour Animal Mystère.
        /// </summary>
        private void AnimalMystereButton_Click(object sender, RoutedEventArgs e)
        {
            _isMemoryMode = false;
            ShowDifficultyPage();
        }

        /// <summary>
        /// Bascule vers la sélection de difficulté pour Memory.
        /// </summary>
        private void MemoryButton_Click(object sender, RoutedEventArgs e)
        {
            _isMemoryMode = true;
            ShowDifficultyPage();
        }

        /// <summary>
        /// Affiche la page de choix de difficulté.
        /// </summary>
        private void ShowDifficultyPage()
        {
            var difficultyPage = new DifficultyPage();
            difficultyPage.BackRequested += DifficultyPage_BackRequested;
            difficultyPage.DifficultySelected += DifficultyPage_DifficultySelected;
            PageHost.Content = difficultyPage;
            MainMenu.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Retourne au menu principal depuis la page de difficulté.
        /// </summary>
        private void DifficultyPage_BackRequested(object? sender, RoutedEventArgs e)
        {
            ShowMainMenu();
        }

        /// <summary>
        /// Charge la page choisie après sélection de la difficulté.
        /// </summary>
        private void DifficultyPage_DifficultySelected(object? sender, GameDifficultySelectedEventArgs e)
        {
            if (_isMemoryMode)
            {
                EnterFullScreen();
                var memoryPage = new MemoryPage(e.Difficulty);
                memoryPage.BackRequested += MemoryPage_BackRequested;
                PageHost.Content = memoryPage;
                RootGrid.Background = (System.Windows.Media.Brush)Resources["FarmBackground"];
                return;
            }

            PageHost.Content = new MysteryAnimalPage();
            RootGrid.Background = System.Windows.Media.Brushes.Transparent;
        }

        /// <summary>
        /// Gère le retour depuis la page Memory.
        /// </summary>
        private void MemoryPage_BackRequested(object? sender, RoutedEventArgs e)
        {
            ExitFullScreen();
            ShowMainMenu();
        }

        /// <summary>
        /// Affiche le menu principal.
        /// </summary>
        private void ShowMainMenu()
        {
            PageHost.Content = null;
            MainMenu.Visibility = Visibility.Visible;
            RootGrid.Background = (System.Windows.Media.Brush)Resources["FarmBackground"];
        }

        /// <summary>
        /// Active le plein écran pour la partie Memory.
        /// </summary>
        private void EnterFullScreen()
        {
            if (_isFullScreen)
            {
                return;
            }

            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }

        /// <summary>
        /// Restaure l'état de fenêtre avant le plein écran.
        /// </summary>
        private void ExitFullScreen()
        {
            if (!_isFullScreen)
            {
                return;
            }

            WindowStyle = _previousWindowStyle;
            ResizeMode = _previousResizeMode;
            WindowState = _previousWindowState;
            _isFullScreen = false;
        }
    }
}