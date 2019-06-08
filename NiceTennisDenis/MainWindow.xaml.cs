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
        private const uint YEAR_BEGIN = 1990;
        private const uint YEAR_END = 2019;
        private static readonly List<string> displayedLevels =
            new List<string> { "G", "F", "M", "O", "A5" };

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            NiceTennisDenisDll.DataMapper.InitializeDefault(
                string.Format(Settings.Default.sqlConnStringPattern,
                    Settings.Default.sqlServer,
                    Settings.Default.sqlDatabase,
                    Settings.Default.sqlUser,
                    Settings.Default.sqlPassword
                ), Settings.Default.datasDirectory).LoadModel();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var bgw = new BackgroundWorker();
            bgw.DoWork += delegate (object bgwSender, DoWorkEventArgs evt)
            {
                try
                {
                    NiceTennisDenisDll.DataMapper.Default.GenerateAtpRanking(2);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            };
            bgw.RunWorkerAsync();
            // 01- ImportFile.ImportSingleMatchesFileInDatabase([year]);
            // 02- Checklist (players section)
            // 03- ImportFile.CreatePendingPlayersFromSource()
            // 04- ImportFile.UpdatePlayersHeightFromMatchesSource()
            // 05- Checklist (editions section)
            // 06- ImportFile.CreatePendingTournamentEditionsFromSource();
            // 07- Checklist (matches)
            // 08- ImportFile.CreatePendingMatchesFromSource();
            // 09- Creates or updates slot and tournament for each edition (if tournament exists, add the new code in known_codes)
            // 10- "Next Gen Finals" => level 10
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            PgbGenerate.Visibility = Visibility.Visible;
            BtnGenerate.IsEnabled = false;
            BtnImport.IsEnabled = false;
            BtnSaveToJpg.IsEnabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate (object w, DoWorkEventArgs evt)
            {
                for (uint year = YEAR_BEGIN; year <= YEAR_END; year++)
                {
                    NiceTennisDenisDll.DataMapper.Default.LoadMatches(year);
                }
            };
            worker.RunWorkerCompleted += delegate (object w, RunWorkerCompletedEventArgs evt)
            {
                GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
                GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                var editionsInRange = NiceTennisDenisDll.Models.EditionPivot.GetList().Where(me =>
                    me.Year <= YEAR_END && me.Year >= YEAR_BEGIN && displayedLevels.Contains(me.Level.Code));

                var slots = NiceTennisDenisDll.Models.SlotPivot.GetList()
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
                        GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                        AddBlock(0, column, string.Empty, Brushes.DarkGray);
                        column++;
                        currentLevelName = slot.Level.Name;
                    }

                    GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                    // Header slot
                    AddBlock(0, column, string.Concat(slot.Name, "(", slot.Level.Name, ")"));
                    column++;
                }

                int row = 1;
                for (uint year = YEAR_BEGIN; year <= YEAR_END; year++)
                {
                    GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });

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
                            var final = currentEdition.Matches.FirstOrDefault(me => me.Round.IsFinal);

                            if (final != null)
                            {
                                string profilePicPath = Path.Combine(Settings.Default.datasDirectory, "profiles",
                                    string.Concat(CleanName(final.Winner.FirstName), "_", CleanName(final.Winner.LastName), ".jpg"));

                                AddBlock(row, column, final.Winner.Name, ColorBySurfaceId(currentEdition.Surface, currentEdition.Indoor),
                                    File.Exists(profilePicPath) ? profilePicPath : null);
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

        private Brush ColorBySurfaceId(NiceTennisDenisDll.Models.SurfacePivot? surface, bool indoor)
        {
            switch (surface)
            {
                case NiceTennisDenisDll.Models.SurfacePivot.Grass:
                    return Brushes.LightGreen;
                case NiceTennisDenisDll.Models.SurfacePivot.Clay:
                    return Brushes.Orange;
                case NiceTennisDenisDll.Models.SurfacePivot.Carpet:
                    return Brushes.LightGray;
                case NiceTennisDenisDll.Models.SurfacePivot.Hard:
                    return indoor ? Brushes.LightGray : Brushes.LightBlue;
                default:
                    return Brushes.DarkGray;
            }
        }

        private void AddBlock(int row, int column, string text, Brush background = null, string imagePath = null)
        {
            var surBloc = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black,
                Background = background ?? Brushes.White,
            };
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri(imagePath);
                logo.EndInit();

                surBloc.Child = new Image
                {
                    Source = logo,
                    Margin = new Thickness(5),
                    Stretch = Stretch.UniformToFill
                };
            }
            else
            {
                surBloc.Child = new TextBlock
                {
                    Margin = new Thickness(5),
                    Text = text,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
            }
            surBloc.SetValue(Grid.RowProperty, row);
            surBloc.SetValue(Grid.ColumnProperty, column);
            GrdChan.Children.Add(surBloc);
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
                using (Stream fileStream = File.Create(Path.Combine(Settings.Default.datasDirectory, "screenshot", "screenshot.jpg")))
                {
                    pngImage.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while screenshoting : " + ex.Message);
            }
        }

        // Cleans player's name for filename construction.
        private string CleanName(string name)
        {
            return name.Trim().ToLowerInvariant().Replace(" ", "_");
        }
    }
}
