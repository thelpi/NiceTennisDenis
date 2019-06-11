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
            NiceTennisDenisDll.DataMapper.InitializeDefault(
                string.Format(Settings.Default.sqlConnStringPattern,
                    Settings.Default.sqlServer,
                    Settings.Default.sqlDatabase,
                    Settings.Default.sqlUser,
                    Settings.Default.sqlPassword
                ), Settings.Default.datasDirectory, Settings.Default.isWta, Settings.Default.configurationId).LoadModel();
            //new RankingWindow().ShowDialog();
            new MainWindow().ShowDialog();
        }
    }
}
