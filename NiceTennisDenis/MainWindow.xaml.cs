using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NiceTennisDenis
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const uint YEAR_BEGIN = 1990;
        private const uint YEAR_END = 2018;

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
            // 9- Creates or updates slot and tournament for each edition (if tournament exists, add the new code in known_codes)
            // 10- "Next Gen Finals" => level 10
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // Top angle
            GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
            GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            List<Slot> slots = SqlTools.GetSlots(YEAR_BEGIN, YEAR_END);
            int column = 1;
            string currentLevelName = null;
            foreach (var slot in slots)
            {
                if (currentLevelName == null)
                {
                    currentLevelName = slot.LevelName;
                }
                else if (currentLevelName != slot.LevelName)
                {
                    GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    AddBlock(0, column, string.Empty, Brushes.DarkGray);
                    column++;
                    currentLevelName = slot.LevelName;
                }

                GrdChan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                // Header slot
                AddBlock(0, column, slot.Description);
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
                        currentLevelName = slot.LevelName;
                    }
                    else if (currentLevelName != slot.LevelName)
                    {
                        AddBlock(row, column, string.Empty, Brushes.DarkGray);
                        column++;
                        currentLevelName = slot.LevelName;
                    }

                    var winner = SqlTools.GetWinnerAndSurface(year, slot.Id);
                    if (winner != null)
                    {
                        string profilePicPath = Path.Combine(Properties.Settings.Default.profilePicPath, winner.Value.ProfileFileName);

                        AddBlock(row, column, winner.Value.Name, ColorBySurfaceId(winner.Value),
                            File.Exists(profilePicPath) ? profilePicPath : null);
                    }
                    else
                    {
                        AddBlock(row, column, string.Empty, Brushes.Black);
                    }

                    column++;
                }
                row++;
            }
        }

        private Brush ColorBySurfaceId(WinnerAndSurface was)
        {
            switch (was.SurfaceId)
            {
                case 1:
                    return Brushes.LightGreen;
                case 2:
                    return Brushes.Orange;
                case 3:
                    return Brushes.LightGray;
                case 4:
                    return was.Indoor ? Brushes.LightGray : Brushes.LightBlue;
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
                using (System.IO.Stream fileStream = System.IO.File.Create(System.IO.Path.Combine(Properties.Settings.Default.saveScreenshotPath, "screenshot.jpg")))
                {
                    pngImage.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro while screenshoting : " + ex.Message);
            }
        }
    }
}
