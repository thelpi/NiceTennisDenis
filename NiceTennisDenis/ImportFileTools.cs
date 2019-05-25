using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using NiceTennisDenis.Properties;

namespace NiceTennisDenis
{
    /// <summary>
    /// Tools to import datas files in the SQL database.
    /// </summary>
    internal static class ImportFile
    {
        /// <summary>
        /// Proceeds to import every files of matches in the "source_datas" table of the database.
        /// </summary>
        /// <param name="latestYear">Latest year to proceed (included).</param>
        /// <param name="clearPrevious">Indicates if existing datas for a year should be erased.</param>
        /// <remarks>
        /// From the beginning of Open era, without skipping any year.
        /// Every year has its own try / catch.
        /// </remarks>
        public static void ImporteMatchesFromEveryFiles(int latestYear, bool clearPrevious)
        {
            for (int i = Settings.Default.yearBeginOpenEra; i <= latestYear; i++)
            {
                try
                {
                    ImportSingleMatchesFileInDatabase(i, clearPrevious);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error while processing the year {i}.\r\n{ex.Message}");
                }
            }
        }

        /// <summary>
        /// Proceeds to import a single file of matches in the "source_datas" table of the database.
        /// </summary>
        /// <param name="year">The year of matches.</param>
        /// <param name="clearPrevious">
        /// Indicates if existing datas for this year should be erased.
        /// Otherwise, the operation is cancelled and an exception is thrown.
        /// Processed datas can't be erased and will throw an exception anyway.
        /// </param>
        public static void ImportSingleMatchesFileInDatabase(int year, bool clearPrevious)
        {
            string fileName = string.Format(Settings.Default.matchesFileNamePattern, year);

            string fullFileName = Path.Combine(Settings.Default.datasDirectory, fileName);
            if (!File.Exists(fullFileName))
            {
                throw new ArgumentException($"No file found for the specified year (path : {fullFileName}).", nameof(year));
            }

            CheckExistingMatchesForASpecifiedYear(clearPrevious, fileName);

            ExtractMatchesColumnsHeadersAndValues(fileName, fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent);

            using (var sqlConnection = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = SqlTools.GetSqlInsertStatement("source_matches", headerColumns);
                    headerColumns.ForEach(hc => sqlCommand.Parameters.Add(string.Concat("@", hc), MySqlDbType.String, 255));
                    sqlCommand.Prepare();
                    foreach (var contentLine in linesOfContent)
                    {
                        int i = 0;
                        foreach (var contentColumn in contentLine)
                        {
                            sqlCommand.Parameters[i].Value = contentColumn;
                            i++;
                        }
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ExtractMatchesColumnsHeadersAndValues(string fileName, string fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent)
        {
            headerColumns = null;
            linesOfContent = new List<List<string>>();
            using (var reader = new StreamReader(fullFileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    List<string> columnsList = line.Split(new string[] { Settings.Default.columnSeparator }, StringSplitOptions.None).ToList();
                    if (headerColumns == null)
                    {
                        headerColumns = columnsList;
                    }
                    else if (columnsList.Count != headerColumns.Count)
                    {
                        throw new Exception("The columns count doesn't match the headers count.");
                    }
                    else
                    {
                        linesOfContent.Add(columnsList);
                    }
                }
            }

            if (headerColumns == null || linesOfContent.Count == 0)
            {
                throw new Exception("Invalid content for the file of the specified year.");
            }
            headerColumns.Add("file_name");
            linesOfContent.ForEach(line => line.Add(fileName));
        }

        private static void CheckExistingMatchesForASpecifiedYear(bool clearPrevious, string fileName)
        {
            using (var sqlConnection = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();

                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    // checks existing datas
                    sqlCommand.CommandText = "SELECT COUNT(*) FROM source_datas WHERE file_name = @file_name";
                    sqlCommand.Parameters.Add("@file_name", MySqlDbType.String, 255);
                    sqlCommand.Parameters["@file_name"].Value = fileName;
                    if (Convert.ToInt32(sqlCommand.ExecuteScalar()) > 0)
                    {
                        if (clearPrevious)
                        {
                            // deletes disposable datas
                            sqlCommand.CommandText = "DELETE FROM source_datas WHERE file_name = @file_name AND date_processed IS NULL";
                            sqlCommand.ExecuteNonQuery();

                            // checks remaining non-removable datas
                            sqlCommand.CommandText = "SELECT COUNT(*) FROM source_datas WHERE file_name = @file_name AND date_processed IS NOT NULL";
                            if (Convert.ToInt32(sqlCommand.ExecuteScalar()) > 0)
                            {
                                throw new ArgumentException("The specified year contains datas already processed.", nameof(fileName));
                            }
                        }
                        else
                        {
                            throw new ArgumentException("The specified year has already been imported.", nameof(fileName));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Proceeds to import the file of players in the "source_players" table of database.
        /// Imports only players not processed. Existing non-processed players are replaced.
        /// </summary>
        public static void ImportNewPlayers()
        {
            string fullFileName = Path.Combine(Settings.Default.datasDirectory, Settings.Default.playersFileName);
            if (!File.Exists(fullFileName))
            {
                throw new Exception($"No file {Settings.Default.playersFileName} found.");
            }

            List<string> playersUnableToRemove = ComputeUnremovablePlayersList();

            ExtractPlayersColumnsHeadersAndValues(fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent);

            int idIndexOf = headerColumns.IndexOf("player_id");

            using (var sqlConnection = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = SqlTools.GetSqlReplaceStatement("source_players", headerColumns);
                    headerColumns.ForEach(hc => sqlCommand.Parameters.Add(string.Concat("@", hc), MySqlDbType.String, 255));
                    sqlCommand.Prepare();
                    foreach (var contentLine in linesOfContent.Where(lc => !playersUnableToRemove.Contains(lc[idIndexOf])))
                    {
                        int i = 0;
                        foreach (var contentColumn in contentLine)
                        {
                            sqlCommand.Parameters[i].Value = contentColumn;
                            i++;
                        }
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private static List<string> ComputeUnremovablePlayersList()
        {
            List<string> players = new List<string>();

            using (var sqlConnection = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = "SELECT player_id FROM source_players WHERE date_processed IS NOT NULL";
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            players.Add(sqlReader.GetString("player_id"));
                        }
                    }
                }
            }

            return players;
        }

        private static void ExtractPlayersColumnsHeadersAndValues(string fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent)
        {
            headerColumns = null;
            linesOfContent = new List<List<string>>();
            using (var reader = new StreamReader(fullFileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    List<string> columnsList = line.Split(new string[] { Settings.Default.columnSeparator }, StringSplitOptions.None).ToList();
                    if (headerColumns == null)
                    {
                        headerColumns = columnsList;
                    }
                    else if (columnsList.Count != headerColumns.Count)
                    {
                        throw new Exception("The columns count doesn't match the headers count.");
                    }
                    else
                    {
                        linesOfContent.Add(columnsList);
                    }
                }
            }

            if (headerColumns == null || linesOfContent.Count == 0)
            {
                throw new Exception("Invalid content for the players file.");
            }
        }

        /// <summary>
        /// Creates in the table "player" players pending in the "source_players" table.
        /// </summary>
        public static void CreatePendingPlayersFromSource()
        {
            using (MySqlConnection sqlConnection = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionBis = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                sqlConnectionBis.Open();
                using (MySqlCommand sqlCommand = sqlConnection.CreateCommand(),
                    sqlCommandBis = sqlConnectionBis.CreateCommand())
                {
                    var columnsHeaders = new List<string>
                    {
                        "id", "first_name", "last_name", "hand", "birth_date", "country"
                    };
                    sqlCommandBis.CommandText = SqlTools.GetSqlInsertStatement("player", columnsHeaders);
                    sqlCommandBis.Parameters.Add("@id", MySqlDbType.UInt32);
                    sqlCommandBis.Parameters.Add("@first_name", MySqlDbType.String, 255);
                    sqlCommandBis.Parameters.Add("@last_name", MySqlDbType.String, 255);
                    sqlCommandBis.Parameters.Add("@hand", MySqlDbType.String, 1);
                    sqlCommandBis.Parameters.Add("@birth_date", MySqlDbType.DateTime);
                    sqlCommandBis.Parameters.Add("@country", MySqlDbType.String, 3);
                    sqlCommandBis.Prepare();

                    sqlCommand.CommandText = "select CONVERT(player_id, UNSIGNED) AS id, TRIM(name_first) AS first_name, " +
                        "TRIM(name_list) AS last_name, IF(hand = 'U', NULL, hand) AS hand, CONVERT(birthdate, DATETIME) AS birth_date, country " +
                        "FROM source_players WHERE date_processed IS NULL";
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            columnsHeaders.ForEach(ch => sqlCommandBis.Parameters[string.Concat("@", ch)].Value = sqlReader[ch]);
                            sqlCommandBis.ExecuteNonQuery();
                        }
                    }

                    sqlCommand.CommandText = "UPDATE source_players SET date_processed = NOW() WHERE date_processed IS NULL";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Creates in the table "edition" tournaments editions pending in the "source_matches" table.
        /// </summary>
        public static void CreatePendingTournamentEditionsFromSource()
        {
            // TODO : to load from database
            var surfaces = new Dictionary<string, int>
            {
                { "Grass", 1 },
                { "Clay", 2 },
                { "Carpet", 3 },
                { "Hard", 4 }
            };

            // TODO : to load from database
            var levels = new Dictionary<string, int>
            {
                { "G", 1 },
                { "D", 2 },
                { "F", 3 },
                { "A", 4 },
                { "M", 5 },
                { "O", 6 },
                { "A500", 7 },
                { "A250", 8 },
            };

            using (MySqlConnection sqlConnection = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionBis = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                sqlConnectionBis.Open();
                using (MySqlCommand sqlCommand = sqlConnection.CreateCommand(),
                    sqlCommandBis = sqlConnectionBis.CreateCommand())
                {
                    var columnsHeaders = new List<string>
                    {
                        "year", "code", "name", "surface_id", "draw_size", "level_id", "date_begin"
                    };
                    sqlCommandBis.CommandText = SqlTools.GetSqlInsertStatement("edition", columnsHeaders);
                    sqlCommandBis.Parameters.Add("@year", MySqlDbType.UInt32);
                    sqlCommandBis.Parameters.Add("@code", MySqlDbType.String, 255);
                    sqlCommandBis.Parameters.Add("@name", MySqlDbType.String, 255);
                    sqlCommandBis.Parameters.Add("@surface_id", MySqlDbType.UInt32);
                    sqlCommandBis.Parameters.Add("@draw_size", MySqlDbType.UInt32);
                    sqlCommandBis.Parameters.Add("@level_id", MySqlDbType.UInt32);
                    sqlCommandBis.Parameters.Add("@date_begin", MySqlDbType.DateTime);
                    sqlCommandBis.Prepare();

                    var getEditionsSql = new StringBuilder();
                    getEditionsSql.AppendLine("SELECT");
                    getEditionsSql.AppendLine(" CONVERT(SUBSTR(tourney_id, 1, 4), UNSIGNED) AS year,");
                    getEditionsSql.AppendLine(" SUBSTR(tourney_id, 6, 255) AS code,");
                    getEditionsSql.AppendLine(" MIN(tourney_name) AS name,");
                    getEditionsSql.AppendLine(" MIN(surface) AS surface,");
                    getEditionsSql.AppendLine(" CONVERT(MIN(draw_size), UNSIGNED) AS draw_size,");
                    getEditionsSql.AppendLine(" MIN(tourney_level) AS tourney_level,");
                    getEditionsSql.AppendLine(" CONVERT(MIN(tourney_date), DATETIME) AS tourney_date");
                    getEditionsSql.AppendLine("FROM source_matches");
                    getEditionsSql.AppendLine("WHERE NOT EXISTS (");
                    getEditionsSql.AppendLine("     SELECT 1 FROM edition");
                    getEditionsSql.AppendLine("     WHERE year = SUBSTR(tourney_id, 1, 4) AND code = SUBSTR(tourney_id, 6, 255)");
                    getEditionsSql.AppendLine(")");
                    getEditionsSql.AppendLine("GROUP BY");
                    getEditionsSql.AppendLine(" CONVERT(SUBSTR(tourney_id, 1, 4), UNSIGNED),");
                    getEditionsSql.AppendLine(" SUBSTR(tourney_id, 6, 255)");
                    getEditionsSql.AppendLine("ORDER BY");
                    getEditionsSql.AppendLine(" CONVERT(SUBSTR(tourney_id, 1, 4), UNSIGNED) ASC,");
                    getEditionsSql.AppendLine(" SUBSTR(tourney_id, 6, 255) ASC");

                    sqlCommand.CommandText = getEditionsSql.ToString();
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            sqlCommandBis.Parameters["@year"].Value = sqlReader["year"];
                            sqlCommandBis.Parameters["@code"].Value = sqlReader["code"];
                            sqlCommandBis.Parameters["@name"].Value = sqlReader["name"];
                            sqlCommandBis.Parameters["@surface_id"].Value = surfaces.ContainsKey(sqlReader.GetString("surface")) ?
                                (object)surfaces[sqlReader.GetString("surface")] : DBNull.Value;
                            sqlCommandBis.Parameters["@draw_size"].Value = sqlReader["draw_size"];
                            sqlCommandBis.Parameters["@level_id"].Value = levels[sqlReader.GetString("tourney_level")];
                            sqlCommandBis.Parameters["@date_begin"].Value = sqlReader["tourney_date"];
                            sqlCommandBis.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the "height" information on players from the source of matches.
        /// </summary>
        public static void UpdatePlayersHeightFromMatchesSource()
        {
            List<uint> playersId = new List<uint>();

            using (MySqlConnection sqlConnection = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (MySqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = "SELECT id FROM player WHERE height IS NULL";
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            playersId.Add(sqlReader.GetUInt32("id"));
                        }
                    }
                }
            }

            using (MySqlConnection sqlConnection = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionBis = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                sqlConnectionBis.Open();
                using (MySqlCommand sqlCommand = sqlConnection.CreateCommand(),
                    sqlCommandBis = sqlConnectionBis.CreateCommand())
                {
                    sqlCommandBis.CommandText = "UPDATE player SET height = @height WHERE id = @id";
                    sqlCommandBis.Parameters.Add("@id", MySqlDbType.UInt32);
                    sqlCommandBis.Parameters.Add("@height", MySqlDbType.UInt32);
                    sqlCommandBis.Prepare();

                    var getHeightSql = new StringBuilder();
                    getHeightSql.AppendLine("SELECT tmp.pid, tmp.ht");
                    getHeightSql.AppendLine("FROM (");
                    getHeightSql.AppendLine("   SELECT CONVERT(winner_id, UNSIGNED) AS pid, CONVERT(winner_ht, UNSIGNED) AS ht");
                    getHeightSql.AppendLine("   FROM source_matches");
                    getHeightSql.AppendLine("   WHERE CONVERT(winner_id, UNSIGNED) IN (");
                    getHeightSql.AppendLine(string.Join(", ", playersId));
                    getHeightSql.AppendLine(")");
                    getHeightSql.AppendLine("   AND CONVERT(winner_ht, UNSIGNED) IS NOT NULL");
                    getHeightSql.AppendLine("   AND CONVERT(winner_ht, UNSIGNED) > 0");
                    getHeightSql.AppendLine("   UNION ALL");
                    getHeightSql.AppendLine("   SELECT CONVERT(loser_id, UNSIGNED) AS pid, CONVERT(loser_ht, UNSIGNED) AS ht");
                    getHeightSql.AppendLine("   FROM source_matches");
                    getHeightSql.AppendLine("   WHERE CONVERT(loser_id, UNSIGNED) IN (");
                    getHeightSql.AppendLine(string.Join(", ", playersId));
                    getHeightSql.AppendLine(")");
                    getHeightSql.AppendLine("   AND CONVERT(loser_ht, UNSIGNED) IS NOT NULL");
                    getHeightSql.AppendLine("   AND CONVERT(loser_ht, UNSIGNED) > 0");
                    getHeightSql.AppendLine(") AS tmp");
                    getHeightSql.AppendLine("GROUP BY tmp.pid, tmp.ht");
                    getHeightSql.AppendLine("ORDER BY tmp.pid, COUNT(*) DESC");

                    sqlCommand.CommandText = getHeightSql.ToString();
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        uint? currentPlayerId = null;
                        while (sqlReader.Read())
                        {
                            if (!currentPlayerId.HasValue || currentPlayerId.Value != sqlReader.GetUInt32("pid"))
                            {
                                currentPlayerId = sqlReader.GetUInt32("pid");
                                sqlCommandBis.Parameters["@id"].Value = currentPlayerId;
                                sqlCommandBis.Parameters["@height"].Value = sqlReader["ht"];
                                sqlCommandBis.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates pending matches from the table "source_matches".
        /// </summary>
        public static void CreatePendingMatchesFromSource()
        {
            var editions = new Dictionary<string, uint>();
            using (var sqlConnection = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = "select id, concat(year, '-', code) as full_code from edition";
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            editions.Add(sqlReader.GetString("full_code"), sqlReader.GetUInt32("id"));
                        }
                    }
                }
            }

            // TODO : load from the dabatase.
            var entries = new Dictionary<string, uint>
            {
                { "Q", 1 },
                { "LL", 2 },
                { "WC", 3 },
                { "PR", 4 },
                { "SE", 5 },
                { "ALT", 6 }
            };

            // TODO : load from the database
            var rounds = new Dictionary<string, uint>
            {
                { "F", 1 },
                { "SF", 2 },
                { "QF", 3 },
                { "R16", 4 },
                { "R32", 5 },
                { "R64", 6 },
                { "R128", 7 },
                { "RR", 8 },
                { "BR", 9 }
            };

            var retirements = new List<string> { "ret", "abd", "abn", "aba" };
            var walkovers = new List<string> { "w/o", "walkover", "wo" };
            var disqualifications = new List<string> { "def", "disq" };
            var unfinished = new List<string> { "unfinished" };

            using (MySqlConnection sqlConnectionGetMatches = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionCreateGeneral = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionCreateScore = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionCreateStat = new MySqlConnection(SqlTools.ConnectionString),
                sqlConnectionUpdateDateprocessed = new MySqlConnection(SqlTools.ConnectionString))
            {
                sqlConnectionGetMatches.Open();
                sqlConnectionCreateGeneral.Open();
                sqlConnectionCreateScore.Open();
                sqlConnectionCreateStat.Open();
                sqlConnectionUpdateDateprocessed.Open();
                using (MySqlCommand sqlCommandGetMatches = sqlConnectionGetMatches.CreateCommand(),
                    sqlCommandCreateGeneral = sqlConnectionCreateGeneral.CreateCommand(),
                    sqlCommandCreateScore = sqlConnectionCreateScore.CreateCommand(),
                    sqlCommandCreateStat = sqlConnectionCreateStat.CreateCommand(),
                    sqlCommandUpdateDateProcessed = sqlConnectionUpdateDateprocessed.CreateCommand())
                {
                    #region Prepares insert match_general

                    var genHeadersColumnsUint = new List<string>
                    {
                        "edition_id", "match_num", "best_of", "round_id", "minutes",
                        "winner_id", "winner_seed", "winner_entry_id", "winner_rank", "winner_rank_points",
                        "loser_id", "loser_seed", "loser_entry_id", "loser_rank", "loser_rank_points"
                    };
                    var genHeadersColumnsBool = new List<string>
                    {
                        "walkover", "retirement", "disqualification", "unfinished"
                    };
                    sqlCommandCreateGeneral.CommandText = SqlTools.GetSqlInsertStatement("match_general", genHeadersColumnsUint.Concat(genHeadersColumnsBool));
                    foreach (var headerColumn in genHeadersColumnsUint)
                    {
                        sqlCommandCreateGeneral.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.UInt32);
                    }
                    foreach (var headerColumn in genHeadersColumnsBool)
                    {
                        sqlCommandCreateGeneral.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.Byte);
                    }
                    sqlCommandCreateGeneral.Prepare();

                    #endregion

                    #region Prepares insert match_score

                    var scoreHeadersColumnsUint = new List<string>();
                    for (var i = 1; i <= 5; i++)
                    {
                        scoreHeadersColumnsUint.Add($"w_set_{i}");
                        scoreHeadersColumnsUint.Add($"l_set_{i}");
                        scoreHeadersColumnsUint.Add($"tb_set_{i}");
                    }
                    var scoreHeadersColumnsOther = new List<string> { "match_id", "super_tb" };
                    sqlCommandCreateScore.CommandText = SqlTools.GetSqlInsertStatement("match_score", scoreHeadersColumnsUint.Concat(scoreHeadersColumnsOther));
                    sqlCommandCreateScore.Parameters.Add("@match_id", MySqlDbType.UInt64);
                    sqlCommandCreateScore.Parameters.Add("@super_tb", MySqlDbType.String, 255);
                    foreach (var headerColumn in scoreHeadersColumnsUint)
                    {
                        sqlCommandCreateScore.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.UInt32);
                    }
                    sqlCommandCreateScore.Prepare();

                    #endregion

                    #region Prepares insert match_stat

                    var statHeadersColumns = new List<string>
                    {
                        "match_id", "w_ace", "l_ace", "w_df", "l_df", "w_sv_pt", "l_sv_pt", "w_1st_in", "l_1st_in", "w_1st_won", "l_1st_won",
                        "w_2nd_won", "l_2nd_won", "w_sv_gms", "l_sv_gms", "w_bp_saved", "l_bp_saved", "w_bp_faced", "l_bp_faced"
                    };
                    sqlCommandCreateStat.CommandText = SqlTools.GetSqlInsertStatement("match_stat", statHeadersColumns);
                    sqlCommandCreateStat.Parameters.Add("@match_id", MySqlDbType.UInt64);
                    // Note : the first column "match_id" is skipped.
                    foreach (var headerColumn in statHeadersColumns.Skip(1))
                    {
                        sqlCommandCreateStat.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.UInt32);
                    }
                    sqlCommandCreateStat.Prepare();

                    #endregion

                    // Prepares update source_match
                    sqlCommandUpdateDateProcessed.CommandText = "UPDATE source_matches SET date_processed = NOW() WHERE id = @id";
                    sqlCommandUpdateDateProcessed.Parameters.Add("@id", MySqlDbType.UInt64);
                    sqlCommandUpdateDateProcessed.Prepare();

                    // Gets matches to insert
                    sqlCommandGetMatches.CommandText = "SELECT * FROM source_matches WHERE date_processed IS NULL";
                    using (var sqlReader = sqlCommandGetMatches.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            string rawScore = sqlReader.GetString("score").Trim().ToLowerInvariant();

                            #region inserts general

                            sqlCommandCreateGeneral.Parameters["@edition_id"].Value = editions[sqlReader.GetString("tourney_id")];
                            sqlCommandCreateGeneral.Parameters["@match_num"].Value = Convert.ToUInt32(sqlReader["match_num"]);
                            sqlCommandCreateGeneral.Parameters["@best_of"].Value = Convert.ToUInt32(sqlReader["best_of"]);
                            sqlCommandCreateGeneral.Parameters["@round_id"].Value = rounds[sqlReader.GetString("round")];
                            sqlCommandCreateGeneral.Parameters["@minutes"].Value = sqlReader.ToUint("minutes");
                            sqlCommandCreateGeneral.Parameters["@winner_id"].Value = Convert.ToUInt32(sqlReader["winner_id"]);
                            sqlCommandCreateGeneral.Parameters["@winner_seed"].Value = sqlReader.ToUint("winner_seed");
                            sqlCommandCreateGeneral.Parameters["@winner_entry_id"].Value =
                                !string.IsNullOrWhiteSpace(sqlReader.GetString("winner_entry")) ?
                                    (object)entries[sqlReader.GetString("winner_entry")] : DBNull.Value;
                            sqlCommandCreateGeneral.Parameters["@winner_rank"].Value = sqlReader.ToUint("winner_rank");
                            sqlCommandCreateGeneral.Parameters["@winner_rank_points"].Value = sqlReader.ToUint("winner_rank_points");
                            sqlCommandCreateGeneral.Parameters["@loser_id"].Value = Convert.ToUInt32(sqlReader["loser_id"]);
                            sqlCommandCreateGeneral.Parameters["@loser_seed"].Value = sqlReader.ToUint("loser_seed");
                            sqlCommandCreateGeneral.Parameters["@loser_entry_id"].Value =
                                !string.IsNullOrWhiteSpace(sqlReader.GetString("loser_entry")) ?
                                    (object)entries[sqlReader.GetString("loser_entry")] : DBNull.Value;
                            sqlCommandCreateGeneral.Parameters["@loser_rank"].Value = sqlReader.ToUint("loser_rank");
                            sqlCommandCreateGeneral.Parameters["@loser_rank_points"].Value = sqlReader.ToUint("loser_rank_points");
                            sqlCommandCreateGeneral.Parameters["@walkover"].Value =
                                walkovers.Any(x => rawScore.Contains(x)) ? 1 : 0;
                            sqlCommandCreateGeneral.Parameters["@retirement"].Value =
                                retirements.Any(x => rawScore.Contains(x)) ? 1 : 0;
                            sqlCommandCreateGeneral.Parameters["@disqualification"].Value =
                                disqualifications.Any(x => rawScore.Contains(x)) ? 1 : 0;
                            sqlCommandCreateGeneral.Parameters["@unfinished"].Value =
                                unfinished.Any(x => rawScore.Contains(x)) ? 1 : 0;
                            sqlCommandCreateGeneral.ExecuteNonQuery();

                            #endregion

                            long matchId = sqlCommandCreateGeneral.LastInsertedId;

                            #region Inserts score

                            ParseScore(rawScore, out List<List<uint?>>  result, out string superTb);

                            if (result != null)
                            {
                                sqlCommandCreateScore.Parameters["@match_id"].Value = matchId;
                                sqlCommandCreateScore.Parameters["@w_set_1"].Value =
                                    result[0][0].HasValue ? (object)result[0][0].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@l_set_1"].Value =
                                    result[0][1].HasValue ? (object)result[0][1].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@tb_set_1"].Value =
                                    result[0][2].HasValue ? (object)result[0][2].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@w_set_2"].Value =
                                    result[1][0].HasValue ? (object)result[1][0].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@l_set_2"].Value =
                                    result[1][1].HasValue ? (object)result[1][1].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@tb_set_2"].Value =
                                    result[1][2].HasValue ? (object)result[1][2].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@w_set_3"].Value =
                                    result[2][0].HasValue ? (object)result[2][0].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@l_set_3"].Value =
                                    result[2][1].HasValue ? (object)result[2][1].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@tb_set_3"].Value =
                                    result[2][2].HasValue ? (object)result[2][2].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@w_set_4"].Value =
                                    result[3][0].HasValue ? (object)result[3][0].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@l_set_4"].Value =
                                    result[3][1].HasValue ? (object)result[3][1].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@tb_set_4"].Value =
                                    result[3][2].HasValue ? (object)result[3][2].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@w_set_5"].Value =
                                    result[4][0].HasValue ? (object)result[4][0].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@l_set_5"].Value =
                                    result[4][1].HasValue ? (object)result[4][1].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@tb_set_5"].Value =
                                    result[4][2].HasValue ? (object)result[4][2].Value : DBNull.Value;
                                sqlCommandCreateScore.Parameters["@super_tb"].Value =
                                    string.IsNullOrWhiteSpace(superTb) ? DBNull.Value : (object)superTb;
                                sqlCommandCreateScore.ExecuteNonQuery();
                            }

                            #endregion

                            #region Inserts match_stat

                            sqlCommandCreateStat.Parameters["@match_id"].Value = matchId;
                            sqlCommandCreateStat.Parameters["@w_ace"].Value = sqlReader.ToUint("w_ace");
                            sqlCommandCreateStat.Parameters["@l_ace"].Value = sqlReader.ToUint("l_ace");
                            sqlCommandCreateStat.Parameters["@w_df"].Value = sqlReader.ToUint("w_df");
                            sqlCommandCreateStat.Parameters["@l_df"].Value = sqlReader.ToUint("l_df");
                            sqlCommandCreateStat.Parameters["@w_sv_pt"].Value = sqlReader.ToUint("w_svPt");
                            sqlCommandCreateStat.Parameters["@l_sv_pt"].Value = sqlReader.ToUint("l_svPt");
                            sqlCommandCreateStat.Parameters["@w_1st_in"].Value = sqlReader.ToUint("w_1stIn");
                            sqlCommandCreateStat.Parameters["@l_1st_in"].Value = sqlReader.ToUint("l_1stIn");
                            sqlCommandCreateStat.Parameters["@w_1st_won"].Value = sqlReader.ToUint("w_1stWon");
                            sqlCommandCreateStat.Parameters["@l_1st_won"].Value = sqlReader.ToUint("l_1stWon");
                            sqlCommandCreateStat.Parameters["@w_2nd_won"].Value = sqlReader.ToUint("w_2ndWon");
                            sqlCommandCreateStat.Parameters["@l_2nd_won"].Value = sqlReader.ToUint("l_2ndWon");
                            sqlCommandCreateStat.Parameters["@w_sv_gms"].Value = sqlReader.ToUint("w_svGms");
                            sqlCommandCreateStat.Parameters["@l_sv_gms"].Value = sqlReader.ToUint("l_svGms");
                            sqlCommandCreateStat.Parameters["@w_bp_saved"].Value = sqlReader.ToUint("w_bpSaved");
                            sqlCommandCreateStat.Parameters["@l_bp_saved"].Value = sqlReader.ToUint("l_bpSaved");
                            sqlCommandCreateStat.Parameters["@w_bp_faced"].Value = sqlReader.ToUint("w_bpFaced");
                            sqlCommandCreateStat.Parameters["@l_bp_faced"].Value = sqlReader.ToUint("l_bpFaced");
                            sqlCommandCreateStat.ExecuteNonQuery();

                            #endregion

                            // Updates source_matches
                            sqlCommandUpdateDateProcessed.Parameters["@id"].Value = sqlReader["id"];
                            sqlCommandUpdateDateProcessed.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private static void ParseScore(string score, out List<List<uint?>> result, out string superTb)
        {
            result = null;
            superTb = null;

            if (string.IsNullOrWhiteSpace(score))
            {
                return;
            }

            if (score.EndsWith("]"))
            {
                score = score.Substring(0, score.Length - 1);
                var posOfSuperTbBegin = score.LastIndexOf("[");
                if (posOfSuperTbBegin > -1)
                {
                    superTb = score.Substring(posOfSuperTbBegin + 1);
                    score = score.Substring(0, posOfSuperTbBegin);
                }
            }

            int setNo = 1;
            var sets = score.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string set in sets)
            {
                var elementsOfSet = set.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                if (elementsOfSet.Length == 2)
                {
                    uint? wSet = null;
                    uint? lSet = null;
                    uint? tb = null;

                    if (uint.TryParse(elementsOfSet[0], out uint wSetReal))
                    {
                        wSet = wSetReal;
                    }
                    else
                    {
                        // invalid set
                        break;
                    }

                    if (elementsOfSet[1].EndsWith(")"))
                    {
                        elementsOfSet[1] = elementsOfSet[1].Substring(0, elementsOfSet[1].Length - 1);
                        var posOfTbBegin = elementsOfSet[1].IndexOf("(");
                        if (posOfTbBegin > -1)
                        {
                            if (uint.TryParse(elementsOfSet[1].Substring(posOfTbBegin + 1), out uint tbReal))
                            {
                                tb = tbReal;
                            }
                            else
                            {
                                // invalid set
                                break;
                            }
                            elementsOfSet[1] = elementsOfSet[1].Substring(0, posOfTbBegin);
                        }
                        else
                        {
                            // invalid set
                            break;
                        }
                    }

                    if (uint.TryParse(elementsOfSet[1], out uint lSetReal))
                    {
                        lSet = lSetReal;
                    }
                    else
                    {
                        // invalid set
                        break;
                    }

                    if (result == null)
                    {
                        result = new List<List<uint?>>();
                    }
                    result.Add(new List<uint?> { wSet, lSet, tb });

                    setNo++;
                }
                else
                {
                    // invalid set
                    break;
                }
            }

            if (result != null && result.Count < 5)
            {
                while (result.Count < 5)
                {
                    result.Add(new List<uint?> { null, null, null });
                }
            }
        }
    }
}
