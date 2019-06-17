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
            new MainWindow().ShowDialog();
        }
    }
}
