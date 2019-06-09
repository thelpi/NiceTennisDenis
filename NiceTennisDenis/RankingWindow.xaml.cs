using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using NiceTennisDenisDll;
using NiceTennisDenisDll.Models;

namespace NiceTennisDenis
{
    /// <summary>
    /// Logique d'interaction pour RankingWindow.xaml
    /// </summary>
    public partial class RankingWindow : Window
    {
        private const uint TOP_RANKING = 20;

        public RankingWindow()
        {
            InitializeComponent();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            BtnGenerate.IsEnabled = false;
            var bgw = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            bgw.DoWork += Bgw_DoWork;
            bgw.ProgressChanged += Bgw_ProgressChanged;
            bgw.RunWorkerCompleted += Bgw_RunWorkerCompleted;
            bgw.RunWorkerAsync(new object[] { 2, new DateTime(1968, 1, 1), new DateTime(2018, 12, 31) });
        }

        private void Bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BtnGenerate.IsEnabled = true;
        }

        private void Bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LsbRanking.ItemsSource = e.UserState as IEnumerable<AtpRankingPivot>;
        }

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            var arguments = e.Argument as object[];

            var currentDate = (DateTime)arguments[1];
            var endDate = (DateTime)arguments[2];
            var versionId = Convert.ToUInt32(arguments[0]);

            while (currentDate < endDate)
            {
                var flagDate = DateTime.Now;
                var ranking = DataMapper.Default.GetRankingAtDate(versionId, currentDate, TOP_RANKING);
                (sender as BackgroundWorker).ReportProgress(0, ranking);
                currentDate = currentDate.AddDays(7);
            }
        }
    }
}
