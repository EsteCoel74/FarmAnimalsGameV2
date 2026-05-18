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
    /// <summary>
    /// Logique d'interaction pour MysteryAnimalPage.xaml
    /// </summary>
    public partial class MysteryAnimalPage : UserControl
    {
        public MysteryAnimalPage()
        {
            InitializeComponent();
        }
        Dictionary<string, string> cartes = new Dictionary<string, string>
{
    { "6FDB2B3E", "Vache" },
    { "32FE281D", "Mouton" },
    { "42EA691D", "Chèvre" },
    // à compléter quand tu auras scanné les 7 autres cartes
    // { "????????", "Cochon" },
    // { "????????", "Cheval" },
    // { "????????", "Âne" },
    // { "????????", "Lapin" },
    // { "????????", "Oie" },
    // { "????????", "Poule" },
    // { "????????", "Coq" },
};
    }
}
