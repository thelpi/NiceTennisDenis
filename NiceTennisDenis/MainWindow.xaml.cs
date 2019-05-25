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

namespace NiceTennisDenis
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            // 1- ImportFile.ImportSingleMatchesFileInDatabase([year_to_import], [true_if_retry])
            // 2- Checklist (players section)
            // 3- ImportFile.CreatePendingPlayersFromSource()
            // 4- ImportFile.UpdatePlayersHeightFromMatchesSource()
            // 5- Checklist (editions section)
            // 6- ImportFile.CreatePendingTournamentEditionsFromSource()
            // 7- Checklist (matches)
            // 8- ImportFile.CreatePendingMatchesFromSource();
        }
    }
}
