using System.Windows;
using NiceTennisDenis.Properties;

namespace NiceTennisDenis
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            NiceTennisDenisDll.DataController.InitializeDefault(
                string.Format(Settings.Default.sqlConnStringPattern,
                    Settings.Default.sqlServer,
                    Settings.Default.isWta ? Settings.Default.sqlDatabaseWta : Settings.Default.sqlDatabaseAtp,
                    Settings.Default.sqlUser,
                    Settings.Default.sqlPassword
                ), Settings.Default.datasDirectory, Settings.Default.isWta, Settings.Default.configurationId).LoadModel();
            //new RankingWindow().ShowDialog();
            new MainWindow().ShowDialog();
        }
    }
}
