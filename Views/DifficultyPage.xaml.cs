using System;
using System.Collections.Generic;
using System.Linq;
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
using FarmAnimalsGameV2.Services;

namespace FarmAnimalsGameV2.Views
{
    /// <summary>
    /// Logique d'interaction pour DifficultyPage.xaml
    /// </summary>
    public partial class DifficultyPage : UserControl
    {
        public event RoutedEventHandler? BackRequested;
        public event EventHandler<GameDifficultySelectedEventArgs>? DifficultySelected;

        public DifficultyPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sélectionne la difficulté facile.
        /// </summary>
        private void EasyButton_Click(object sender, RoutedEventArgs e)
        {
            OnDifficultySelected(GameDifficulty.Easy);
        }

        /// <summary>
        /// Sélectionne la difficulté moyenne.
        /// </summary>
        private void MediumButton_Click(object sender, RoutedEventArgs e)
        {
            OnDifficultySelected(GameDifficulty.Medium);
        }

        /// <summary>
        /// Sélectionne la difficulté difficile.
        /// </summary>
        private void HardButton_Click(object sender, RoutedEventArgs e)
        {
            OnDifficultySelected(GameDifficulty.Hard);
        }

        /// <summary>
        /// Demande le retour au menu précédent.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, e);
        }

        /// <summary>
        /// Notifie la sélection de difficulté.
        /// </summary>
        private void OnDifficultySelected(GameDifficulty difficulty)
        {
            DifficultySelected?.Invoke(this, new GameDifficultySelectedEventArgs(difficulty));
        }
    }
}
