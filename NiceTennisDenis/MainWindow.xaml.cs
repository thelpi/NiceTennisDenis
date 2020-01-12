using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiceTennisDenis.Properties;

namespace NiceTennisDenis
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double DEFAULT_BLOCK_SIZE = 160;

        private static readonly uint YEAR_BEGIN = Settings.Default.isWta ? (uint)1988 : 1990;
        private static readonly uint YEAR_END = (uint)DateTime.Now.Year - 1;
        private static readonly List<string> displayedLevels = Settings.Default.isWta ?
            new List<string> { "G", "F", "PM", "O", "WET", "P5" } : new List<string> { "G", "F", "M", "O", "A5" };

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var bgw = new BackgroundWorker();
            bgw.DoWork += delegate (object bgwSender, DoWorkEventArgs evt)
            {
                try
                {

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            };
            bgw.RunWorkerAsync();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            PgbGenerate.Visibility = Visibility.Visible;
            BtnGenerate.IsEnabled = false;
            BtnImport.IsEnabled = false;
            BtnSaveToJpg.IsEnabled = false;

            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += delegate (object w, DoWorkEventArgs evt)
            {
                uint total = YEAR_END - YEAR_BEGIN;
                uint count = 0;
                for (uint year = YEAR_BEGIN; year <= YEAR_END; year++)
                {
                    ApiRequester.Get<IEnumerable<Models.MatchPivot>>($"/Match/{Gtype()}/{year}/true");
                    count++;
                    (w as BackgroundWorker).ReportProgress((int)Math.Floor((count / (double)total) * 100));
                }
            };
            worker.ProgressChanged += delegate (object w, ProgressChangedEventArgs evt)
            {
                PgbGenerate.Value = evt.ProgressPercentage;
            };
            worker.RunWorkerCompleted += delegate (object w, RunWorkerCompletedEventArgs evt)
            {
                var withRunnerUp = ChkWithRunnerUp.IsChecked == true;

                GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DEFAULT_BLOCK_SIZE) });
                GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DEFAULT_BLOCK_SIZE) });

                var editionsInRangeBase = ApiRequester.Get<IEnumerable<Models.EditionPivot>>($"/Edition/{Gtype()}/{YEAR_BEGIN}/{YEAR_END}");
                var editionsInRange = editionsInRangeBase.Where(me => displayedLevels.Contains(me.Level.Code));

                var slotsBase = ApiRequester.Get<IEnumerable<Models.SlotPivot>>($"/Slot/{Gtype()}");
                var slots = slotsBase
                    .Where(me => editionsInRange.Any(you => you.Slot?.Id == me.Id))
                    .OrderBy(me => me.Level.DisplayOrder)
                    .ThenBy(me => me.DisplayOrder);

                int column = 1;
                string currentLevelName = null;
                foreach (var slot in slots)
                {
                    if (currentLevelName == null)
                    {
                        currentLevelName = slot.Level.Name;
                    }
                    else if (currentLevelName != slot.Level.Name)
                    {
                        GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DEFAULT_BLOCK_SIZE / 5) });
                        AddBlock(0, column, string.Empty, Brushes.DarkGray);
                        column++;
                        currentLevelName = slot.Level.Name;
                    }

                    GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DEFAULT_BLOCK_SIZE) });

                    // Header slot
                    AddBlock(0, column, string.Concat(slot.Name, "(", slot.Level.Name, ")"));
                    column++;
                }

                int row = 1;
                for (uint year = YEAR_BEGIN; year <= YEAR_END; year++)
                {
                    GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DEFAULT_BLOCK_SIZE) });

                    // Year column.
                    AddBlock(row, 0, year.ToString());

                    // Slots columns
                    column = 1;
                    currentLevelName = null;
                    foreach (var slot in slots)
                    {
                        if (currentLevelName == null)
                        {
                            currentLevelName = slot.Level.Name;
                        }
                        else if (currentLevelName != slot.Level.Name)
                        {
                            AddBlock(row, column, string.Empty, Brushes.DarkGray);
                            column++;
                            currentLevelName = slot.Level.Name;
                        }

                        var currentEdition = editionsInRange.Where(me => me.Year == year && me.Slot?.Id == slot.Id).FirstOrDefault();
                        if (currentEdition != null)
                        {
                            var final = currentEdition.Final;

                            if (final != null)
                            {
                                AddBlock(row, column, final.Winner.Name, ColorBySurfaceId(currentEdition.Surface, currentEdition.Indoor),
                                    File.Exists(final.Winner.ProfilePicturePath) && !final.Winner.IsJohnDoeProfilePicture ?
                                        final.Winner.ProfilePicturePath : null,
                                    File.Exists(final.Loser.ProfilePicturePath) && !final.Loser.IsJohnDoeProfilePicture ?
                                        final.Loser.ProfilePicturePath : null,
                                    withRunnerUp ? final.Loser.Name : null);
                            }
                            else
                            {
                                AddBlock(row, column, string.Empty, Brushes.Black);
                            }
                        }
                        else
                        {
                            AddBlock(row, column, string.Empty, Brushes.Black);
                        }

                        column++;
                    }
                    row++;
                }

                BtnGenerate.IsEnabled = true;
                BtnImport.IsEnabled = true;
                BtnSaveToJpg.IsEnabled = true;
                PgbGenerate.Visibility = Visibility.Collapsed;
            };
            worker.RunWorkerAsync();
        }

        private static string Gtype()
        {
            return (Settings.Default.isWta ? "wta" : "atp");
        }

        private Brush ColorBySurfaceId(Models.SurfacePivot? surface, bool indoor)
        {
            switch (surface)
            {
                case Models.SurfacePivot.Grass:
                    return Brushes.LightGreen;
                case Models.SurfacePivot.Clay:
                    return Brushes.Orange;
                case Models.SurfacePivot.Carpet:
                    return Brushes.LightGray;
                case Models.SurfacePivot.Hard:
                    return indoor ? Brushes.LightGray : Brushes.LightBlue;
                default:
                    return Brushes.DarkGray;
            }
        }

        private void AddBlock(int row, int column, string defaultOrWinnerText, Brush background = null,
            string winnerImagePath = null, string runnerUpImagePath = null, string runnerUpText = null)
        {
            bool withRunnerUp = !string.IsNullOrWhiteSpace(runnerUpText);

            UIElement winnerElement = null;
            if (!string.IsNullOrWhiteSpace(winnerImagePath))
            {
                winnerElement = CreateImageBlock(winnerImagePath, 1);
            }
            else
            {
                winnerElement = CreateTextBlock(defaultOrWinnerText, 1);
            }
            winnerElement.SetValue(Grid.ColumnProperty, 0);
            winnerElement.SetValue(Grid.RowProperty, 0);
            winnerElement.SetValue(Grid.ColumnSpanProperty, withRunnerUp ? 5 : 6);
            winnerElement.SetValue(Grid.RowSpanProperty, withRunnerUp ? 5 : 6);

            UIElement loserElement = null;
            if (withRunnerUp)
            {
                if (!string.IsNullOrWhiteSpace(runnerUpImagePath))
                {
                    loserElement = CreateImageBlock(runnerUpImagePath, 2);
                }
                else
                {
                    loserElement = CreateTextBlock(runnerUpText, 2, Brushes.White);
                }
                loserElement.SetValue(Grid.ColumnProperty, 4);
                loserElement.SetValue(Grid.RowProperty, 4);
                loserElement.SetValue(Grid.ColumnSpanProperty, 2);
                loserElement.SetValue(Grid.RowSpanProperty, 2);
            }

            Grid finalGrid = new Grid
            {
                Margin = new Thickness(5)
            };
            finalGrid.RowDefinitions.Add(new RowDefinition());
            finalGrid.RowDefinitions.Add(new RowDefinition());
            finalGrid.RowDefinitions.Add(new RowDefinition());
            finalGrid.RowDefinitions.Add(new RowDefinition());
            finalGrid.RowDefinitions.Add(new RowDefinition());
            finalGrid.RowDefinitions.Add(new RowDefinition());
            finalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            finalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            finalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            finalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            finalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            finalGrid.ColumnDefinitions.Add(new ColumnDefinition());
            finalGrid.Children.Add(winnerElement);
            if (loserElement != null)
            {
                finalGrid.Children.Add(loserElement);
            }

            var surBloc = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black,
                Background = background ?? Brushes.White,
                Child = finalGrid
            };
            surBloc.SetValue(Grid.RowProperty, row);
            surBloc.SetValue(Grid.ColumnProperty, column);

            GrdChan.Children.Add(surBloc);
        }

        private static UIElement CreateImageBlock(string imagePath, int zIndex)
        {
            UIElement loserElement;
            BitmapImage loserLogo = new BitmapImage();
            loserLogo.BeginInit();
            loserLogo.UriSource = new Uri(imagePath);
            loserLogo.EndInit();

            Image loserImg = new Image
            {
                Source = loserLogo,
                Stretch = Stretch.UniformToFill
            };
            loserImg.SetValue(Panel.ZIndexProperty, zIndex);

            loserElement = loserImg;
            return loserElement;
        }

        private static UIElement CreateTextBlock(string text, int zIndex, Brush fill = null)
        {
            TextBlock block = new TextBlock
            {
                Text = text,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap,
                Background = fill ?? Brushes.Transparent
            };
            block.SetValue(Panel.ZIndexProperty, zIndex);

            return block;
        }

        private void BtnSaveToJpg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RenderTargetBitmap renderTargetBitmap =
                    new RenderTargetBitmap((int)GrdChan.ActualWidth, (int)GrdChan.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(GrdChan);
                PngBitmapEncoder pngImage = new PngBitmapEncoder();
                pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                using (Stream fileStream = File.Create(@"D:\Ma programmation\csharp\Projects\NiceTennisDenis\datas\screenshot\screenshot.jpg"))
                {
                    pngImage.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while screenshoting : " + ex.Message);
            }
        }

        private void BtnGoToRanking_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            new RankingWindow().ShowDialog();
            Show();
        }
    }
}
