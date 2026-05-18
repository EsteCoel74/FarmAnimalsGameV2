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

namespace FarmAnimalsGameV2.Views
{
    public partial class DifficultyPage : UserControl
    {
        public event RoutedEventHandler? BackRequested;
        public event Action<MysteryAnimalPage.Difficulty>? DifficultySelected;

        public DifficultyPage()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, e);
        }

        private void FacileButton_Click(object sender, RoutedEventArgs e)
        {
            DifficultySelected?.Invoke(MysteryAnimalPage.Difficulty.Facile);
        }

        private void MoyenButton_Click(object sender, RoutedEventArgs e)
        {
            DifficultySelected?.Invoke(MysteryAnimalPage.Difficulty.Moyen);
        }

        private void DifficileButton_Click(object sender, RoutedEventArgs e)
        {
            DifficultySelected?.Invoke(MysteryAnimalPage.Difficulty.Difficile);
        }
    }
}
