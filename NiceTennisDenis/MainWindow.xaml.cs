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
            // 9- Creates or updates slot and tournament for each edition (if tournament exists, add the new code in known_codes)
            // 10- "Next Gen Finals" => level 10
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            List<int?> slots = new List<int?>
            {
                null,
                4, 1, 2, 3,
                5, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17,
                null,
                30, 35, 36, 33, 32, 26, 34, 31, 38,
                37, 28, 27, 29, 43, 41, 39
            };

            GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });

            int column = 0;
            foreach (var slot in slots)
            {
                if (!slot.HasValue)
                {
                    AddBlock(0, column, string.Empty);
                }
                else
                {
                    AddBlock(0, column, GetSlotName(slot.Value));
                }
                column++;
            }

            int row = 1;
            for (int year = 1990; year <= 2018; year++)
            {
                GrdChan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
                for (int j = 0; j < slots.Count; j++)
                {
                    if (j == 0)
                    {
                        AddBlock(row, j, year.ToString());
                    }
                    else if (slots[j] != null)
                    {
                        GetWinnerAndSurface(year, slots[j], out Tuple<string, string> winner, out int surface, out bool indoor);
                        if (winner != null)
                        {
                            var imagePath = @"D:\Ma programmation\csharp\Projects\NiceTennisDenis\datas\images\" + (winner.Item1 ?? "") + ".jpg";
                            if (System.IO.File.Exists(imagePath))
                            {
                                AddBlock(row, j, imagePath, ColorBySurfaceId(surface, indoor), true);
                            }
                            else
                            {
                                AddBlock(row, j, winner.Item2, ColorBySurfaceId(surface, indoor));
                            }
                        }
                        else
                        {
                            AddBlock(row, j, "", Brushes.Black);
                        }
                    }
                }
                row++;
            }
        }

        private string GetSlotName(int slot)
        {
            string name = "";
            using (var sqlConnection = new MySql.Data.MySqlClient.MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = "select name from slot where id = " + slot;
                    name = sqlCommand.ExecuteScalar().ToString();
                }
            }
            return name;
        }

        private Brush ColorBySurfaceId(int surface, bool indoor)
        {
            switch (surface)
            {
                case 1:
                    return Brushes.LightGreen;
                case 2:
                    return Brushes.Orange;
                case 3:
                    return Brushes.LightGray;
                case 4:
                    return indoor ? Brushes.LightGray : Brushes.LightBlue;
                default:
                    return Brushes.DarkGray;
            }
        }

        private void GetWinnerAndSurface(int year, int? slotId, out Tuple<string, string> winner, out int surface, out bool indoor)
        {
            winner = null;
            surface = 0;
            indoor = false;
            using (var sqlConnection = new MySql.Data.MySqlClient.MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    StringBuilder sql = new StringBuilder();
                    sql.AppendLine("select concat(first_name, ' ', last_name), surface_id, indoor, concat(replace(trim(first_name), ' ', '_'), '_', replace(trim(last_name), ' ', '_')) "); // p.image_path
                    sql.AppendLine("from player as p join match_general as m on p.id = m.winner_id ");
                    sql.AppendLine("join edition as e on m.edition_id = e.id ");
                    sql.AppendLine("where round_id = 1");
                    sql.AppendLine("and e.year = " + year + " and e.slot_id = " + slotId);

                    sqlCommand.CommandText = sql.ToString();
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        if (sqlReader.Read())
                        {
                            winner = new Tuple<string, string>(sqlReader[3] == DBNull.Value || string.IsNullOrWhiteSpace(sqlReader[3].ToString()) ? null : sqlReader[3].ToString(), sqlReader[0].ToString());
                            surface = Convert.ToInt32(sqlReader[1]);
                            indoor = Convert.ToBoolean(sqlReader[2]);
                        }
                    }
                }
            }
        }

        private void AddBlock(int row, int column, string text, Brush background = null, bool isPath = false)
        {
            var surBloc = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black,
                Background = background ?? Brushes.White,
            };
            if (isPath)
            {
                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri(text);
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
                using (System.IO.Stream fileStream = System.IO.File.Create(@"D:\Ma programmation\csharp\Projects\NiceTennisDenis\datas\screnshot\screenshot.jpg"))
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
