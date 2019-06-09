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
        private enum Speed
        {
            Slow = 0,
            Medium,
            Fast
        }
        
        private const uint TOP_RANKING = 10;
        private volatile Speed _speed;

        public RankingWindow()
        {
            InitializeComponent();
            CbbVersion.ItemsSource = AtpRankingVersionPivot.GetList();
            DtpStartDate.DisplayDateStart = AtpRankingVersionPivot.OPEN_ERA_BEGIN;
            DtpStartDate.DisplayDateEnd = DateTime.Today;
            DtpStartDate.SelectedDate = AtpRankingVersionPivot.OPEN_ERA_BEGIN;
            DtpEndDate.DisplayDateStart = AtpRankingVersionPivot.OPEN_ERA_BEGIN;
            DtpEndDate.DisplayDateEnd = DateTime.Today;
            DtpEndDate.SelectedDate = DateTime.Today;
            CbbSpeed.SelectedIndex = 1;
            LblCurrentDate.Content = "Current date : animation is not running.";
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (CbbVersion.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a ruleset.", "NiceTennis Denis - Information");
                return;
            }

            if (CbbSpeed.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a speed.", "NiceTennis Denis - Information");
                return;
            }

            BtnGenerate.IsEnabled = false;

            object[] parameters = new object[]
            {
                (CbbVersion.SelectedItem as AtpRankingVersionPivot).Id,
                DtpStartDate.SelectedDate.GetValueOrDefault(AtpRankingVersionPivot.OPEN_ERA_BEGIN),
                DtpEndDate.SelectedDate.GetValueOrDefault(DateTime.Today)
            };

            var bgw = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            bgw.DoWork += Bgw_DoWork;
            bgw.ProgressChanged += Bgw_ProgressChanged;
            bgw.RunWorkerCompleted += Bgw_RunWorkerCompleted;
            bgw.RunWorkerAsync(parameters);
        }

        private void Bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BtnGenerate.IsEnabled = true;
        }

        private void Bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var arguments = e.UserState as object[];

            LsbRanking.ItemsSource = arguments[0] as IEnumerable<AtpRankingPivot>;
            LblCurrentDate.Content = string.Format($"Current date : {((DateTime)arguments[1]).ToString("yyyy-MM-dd")}");
        }

        private void Bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            var arguments = e.Argument as object[];

            var versionId = Convert.ToUInt32(arguments[0]);
            var currentDate = (DateTime)arguments[1];
            var endDate = (DateTime)arguments[2];

            while (currentDate < endDate)
            {
                var flagDate = DateTime.Now;
                var ranking = DataMapper.Default.GetRankingAtDate(versionId, currentDate, TOP_RANKING);
                (sender as BackgroundWorker).ReportProgress(0, new object[] { ranking, currentDate });
                var timeSpan = Convert.ToInt32(Math.Floor((DateTime.Now - flagDate).TotalMilliseconds));
                int referenceTimeElapse = GetSpeedDelay(_speed);
                if (timeSpan < referenceTimeElapse)
                {
                    System.Threading.Thread.Sleep(referenceTimeElapse - timeSpan);
                }
                currentDate = currentDate.AddDays(7);
            }
        }

        private static int GetSpeedDelay(Speed speed)
        {
            int referenceTimeElapse = 0;
            switch (speed)
            {
                case Speed.Slow:
                    referenceTimeElapse = 1000;
                    break;
                case Speed.Medium:
                    referenceTimeElapse = 500;
                    break;
                case Speed.Fast:
                    referenceTimeElapse = 250;
                    break;
            }

            return referenceTimeElapse;
        }

        private void CbbSpeed_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _speed = CbbSpeed.SelectedIndex >= 0 ? (Speed)CbbSpeed.SelectedIndex : Speed.Medium;
        }
    }
}
