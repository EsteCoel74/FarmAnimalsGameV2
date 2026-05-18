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
using FarmAnimalsGameV2;

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
        private void EasyButton_Click(object sender, RoutedEventArgs e)
        {
            OnDifficultySelected(GameDifficulty.Easy);
        }
        private void MediumButton_Click(object sender, RoutedEventArgs e)
        {
            OnDifficultySelected(GameDifficulty.Medium);
        }
        private void HardButton_Click(object sender, RoutedEventArgs e)
        {
            OnDifficultySelected(GameDifficulty.Hard);
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, e);
        }
        private void OnDifficultySelected(GameDifficulty difficulty)
        {
            DifficultySelected?.Invoke(this, new GameDifficultySelectedEventArgs(difficulty));
        }
    }
}
